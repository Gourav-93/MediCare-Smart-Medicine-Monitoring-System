/**
 * MEDICARE PLATFORM - CENTRALIZED API & AUTHENTICATION HANDLER
 * File: js/api.js
 * Description: Unified client wrapper for ASP.NET Core backend communication,
 *              JWT bearer token persistence, 401 interception, and role access guards.
 */

(function (window) {
  'use strict';

  // 1. DYNAMIC API BASE RESOLVER
  // ASP.NET Core Kestrel typically listens on port 5000 and/or 5095 (or 7164 for HTTPS).
  // If the web page is served directly from Kestrel, use window.location.origin.
  // Otherwise, default to port 5000 with automatic fallback to port 5095 if 5000 is unavailable.
  function resolveApiBaseUrl() {
    if (typeof window !== 'undefined' && window.MEDICARE_API_BASE_URL) {
      return window.MEDICARE_API_BASE_URL;
    }
    if (typeof window !== 'undefined' && window.location && window.location.origin) {
      const origin = window.location.origin;
      const port = String(window.location.port || '');
      // If page is hosted on Kestrel (5000, 5095, 7164)
      if (port === '5000' || port === '5095' || port === '7164') {
        return `${origin}/api`;
      }
      // If hosted on localhost root/proxy
      if ((origin.includes('localhost') || origin.includes('127.0.0.1')) && (!port || port === '80' || port === '443')) {
        return `${origin}/api`;
      }
    }
    return 'http://localhost:5000/api';
  }

  let API_BASE_URL = resolveApiBaseUrl();
  const TOKEN_KEY = 'medicare_jwt_token';
  const USER_KEY = 'medicare_user';
  const ROLE_KEY = 'medicare_role';

  /**
   * Safe JWT Payload Parser (Base64 URL decoder)
   * Decodes without requiring third-party libraries.
   */
  function parseJwt(token) {
    if (!token || typeof token !== 'string') return null;
    try {
      const parts = token.split('.');
      if (parts.length !== 3) return null;
      const base64Url = parts[1];
      const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
      const jsonPayload = decodeURIComponent(
        atob(base64)
          .split('')
          .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
          .join('')
      );
      return JSON.parse(jsonPayload);
    } catch (e) {
      console.warn('[MediCare API] Failed to parse JWT token:', e);
      return null;
    }
  }

  // 2. JWT TOKEN STORAGE HELPERS
  function getAuthToken() {
    const token =
      localStorage.getItem(TOKEN_KEY) ||
      localStorage.getItem('jwtToken') ||
      localStorage.getItem('token') ||
      null;
    if (!token || token === 'null' || token === 'undefined' || token.trim() === '') {
      return null;
    }
    return token;
  }

  function setAuthToken(token, user = null) {
    if (!token) return;
    localStorage.setItem(TOKEN_KEY, token);
    localStorage.setItem('jwtToken', token);
    localStorage.setItem('token', token);

    // If user wasn't supplied directly, extract claims from the JWT
    if (!user) {
      const claims = parseJwt(token);
      if (claims) {
        const role =
          claims['role'] ||
          claims['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ||
          claims['Role'] ||
          'PATIENT';
        const email =
          claims['email'] ||
          claims['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] ||
          claims['unique_name'] ||
          '';
        const name =
          claims['name'] ||
          claims['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ||
          email.split('@')[0] ||
          'User';

        user = { email, name, role };
      }
    }

    if (user) {
      localStorage.setItem(USER_KEY, JSON.stringify(user));
      if (user.role) {
        localStorage.setItem(ROLE_KEY, user.role.toUpperCase());
        localStorage.setItem('userRole', user.role.toUpperCase());
      }
    }

    if (typeof window !== 'undefined' && window.dispatchEvent) {
      window.dispatchEvent(new CustomEvent('medicare:auth-change', { detail: { isLoggedIn: true, user } }));
    }
  }

  function clearAuthToken() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem('jwtToken');
    localStorage.removeItem('token');
    localStorage.removeItem(USER_KEY);
    localStorage.removeItem(ROLE_KEY);
    localStorage.removeItem('userRole');

    if (typeof window !== 'undefined' && window.dispatchEvent) {
      window.dispatchEvent(new CustomEvent('medicare:auth-change', { detail: { isLoggedIn: false } }));
    }
  }

  function getCurrentUser() {
    try {
      const raw = localStorage.getItem(USER_KEY);
      if (raw) return JSON.parse(raw);
    } catch (e) {
      console.warn('[MediCare API] Error reading current user:', e);
    }

    // Fallback: extract from token
    const token = getAuthToken();
    if (token) {
      const claims = parseJwt(token);
      if (claims) {
        const role =
          claims['role'] ||
          claims['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ||
          'PATIENT';
        const email =
          claims['email'] ||
          claims['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] ||
          '';
        const name =
          claims['name'] ||
          claims['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ||
          'User';
        return { email, name, role };
      }
    }
    return null;
  }

  function getUserRole() {
    const token = getAuthToken();
    if (!token) return null;
    const user = getCurrentUser();
    if (user && user.role) return user.role.toUpperCase();
    const storedRole = localStorage.getItem(ROLE_KEY) || localStorage.getItem('userRole');
    if (storedRole && storedRole !== 'null' && storedRole !== 'undefined') return storedRole.toUpperCase();
    return 'PATIENT';
  }

  // 3. GENERIC API FETCH WRAPPER WITH AUTO-RETRY ON BACKEND PORTS
  /**
   * Robust fetch wrapper with automatic JWT injection, error normalization,
   * multi-port fallback (5000 <-> 5095), and 401 Unauthorized redirection.
   */
  async function apiFetch(endpoint, method = 'GET', body = null, options = {}) {
    // 1. Build Absolute URL
    let url = endpoint;
    if (!endpoint.startsWith('http://') && !endpoint.startsWith('https://')) {
      const base = API_BASE_URL.replace(/\/+$/, '');
      const path = endpoint.replace(/^\/+/, '');
      url = `${base}/${path}`;
    }

    // 2. Prepare Headers
    const headers = { ...(options.headers || {}) };
    const token = getAuthToken();
    if (token && !headers['Authorization']) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    const isFormData = typeof FormData !== 'undefined' && body instanceof FormData;
    if (body && !isFormData && !headers['Content-Type']) {
      headers['Content-Type'] = 'application/json';
    }

    // 3. Prepare Fetch Config
    const fetchConfig = {
      method: method.toUpperCase(),
      headers,
      ...options,
    };

    if (body !== null && body !== undefined && method.toUpperCase() !== 'GET' && method.toUpperCase() !== 'HEAD') {
      fetchConfig.body = isFormData || typeof body === 'string' ? body : JSON.stringify(body);
    }

    // 4. Execute Network Request with Fallback
    let response;
    try {
      response = await fetch(url, fetchConfig);
    } catch (netErr) {
      // If connection refused to port 5000, attempt fallback to port 5095 (or vice-versa)
      if (url.includes('localhost:5000') || url.includes('127.0.0.1:5000')) {
        const altUrl = url.replace(':5000', ':5095');
        console.warn(`[MediCare API] Primary port 5000 failed. Retrying alternate port 5095: ${altUrl}`);
        try {
          response = await fetch(altUrl, fetchConfig);
          API_BASE_URL = API_BASE_URL.replace(':5000', ':5095');
        } catch {
          throw netErr;
        }
      } else if (url.includes('localhost:5095') || url.includes('127.0.0.1:5095')) {
        const altUrl = url.replace(':5095', ':5000');
        console.warn(`[MediCare API] Primary port 5095 failed. Retrying alternate port 5000: ${altUrl}`);
        try {
          response = await fetch(altUrl, fetchConfig);
          API_BASE_URL = API_BASE_URL.replace(':5095', ':5000');
        } catch {
          throw netErr;
        }
      } else {
        throw netErr;
      }
    }

    // Handle 401 Unauthorized Session Expiry
    if (response.status === 401) {
      console.warn(`[MediCare API] 401 Unauthorized for endpoint: ${url}`);
      clearAuthToken();
      const currentPath = window.location.pathname.split('/').pop().toLowerCase();
      if (currentPath !== 'login.html' && currentPath !== 'index.html' && currentPath !== '') {
        window.location.href = `login.html?expired=true&redirect=${encodeURIComponent(currentPath)}`;
      }
      const error = new Error('Session expired or unauthorized. Please sign in again.');
      error.status = 401;
      throw error;
    }

    // Handle 204 No Content
    if (response.status === 204) {
      return { success: true };
    }

    // Parse Response (JSON or Text)
    const contentType = response.headers.get('content-type') || '';
    let data = null;
    if (contentType.includes('application/json')) {
      data = await response.json();
    } else {
      const rawText = await response.text();
      try {
        data = JSON.parse(rawText);
      } catch {
        data = rawText;
      }
    }

    // Check HTTP Error Codes
    if (!response.ok) {
      let errorMessage =
        (typeof data === 'string' && data.trim().length > 0 ? data.trim() : null) ||
        (data && (data.message || data.title || data.error)) ||
        `API Request failed with status ${response.status} (${response.statusText})`;

      // Extract ASP.NET Core ModelState validation errors if present
      if (data && data.errors && typeof data.errors === 'object') {
        const errorList = Object.values(data.errors).flat().filter(Boolean);
        if (errorList.length > 0) {
          errorMessage = errorList.join(' ');
        }
      }

      const error = new Error(errorMessage);
      error.status = response.status;
      error.data = data;
      throw error;
    }

    return data;
  }

  // 4. AUTHENTICATION SERVICE SHORTCUTS
  /**
   * Log in user with email & password, storing token on success.
   */
  async function login(email, password) {
    const res = await apiFetch('/auth/login', 'POST', {
      email: (email || '').trim(),
      password: password || '',
    });
    if (res && res.token) {
      setAuthToken(res.token, res);
    }
    return res;
  }

  /**
   * Register new Patient or Caregiver user.
   */
  async function register(dto) {
    const res = await apiFetch('/auth/register', 'POST', {
      name: (dto.name || '').trim(),
      email: (dto.email || '').trim(),
      password: dto.password || '',
      role: (dto.role || 'PATIENT').trim().toUpperCase(),
      phoneNumber: (dto.phoneNumber || dto.phone || '').trim() || null,
    });
    if (res && res.token) {
      setAuthToken(res.token, res);
    }
    return res;
  }

  // 5. CONVENIENCE HTTP VERB HELPERS
  const apiGet = (endpoint, options) => apiFetch(endpoint, 'GET', null, options);
  const apiPost = (endpoint, body, options) => apiFetch(endpoint, 'POST', body, options);
  const apiPut = (endpoint, body, options) => apiFetch(endpoint, 'PUT', body, options);
  const apiDelete = (endpoint, options) => apiFetch(endpoint, 'DELETE', null, options);

  // 6. ROLE-BASED ACCESS CONTROL GUARD
  function checkRole(allowedRoles = ['PATIENT', 'CAREGIVER', 'ADMIN']) {
    const currentPath = window.location.pathname.split('/').pop().toLowerCase();
    
    // Public pages are always accessible
    if (currentPath === 'index.html' || currentPath === 'login.html' || currentPath === '') {
      return true;
    }

    const token = getAuthToken();
    const currentRole = getUserRole();

    // If no token exists, require login
    if (!token) {
      console.warn(`[MediCare Auth] No active token found. Redirecting to login.`);
      window.location.href = `login.html?redirect=${encodeURIComponent(currentPath)}`;
      return false;
    }

    // Normalize roles to uppercase
    const normalizedAllowed = allowedRoles.map((r) => r.toUpperCase());
    
    // If user's role is not authorized for this specific view
    if (currentRole && !normalizedAllowed.includes(currentRole)) {
      console.warn(`[MediCare Auth] Role "${currentRole}" not permitted for ${currentPath}.`);
      let fallbackPage = 'index.html';
      if (currentRole === 'PATIENT') fallbackPage = 'patient-dashboard.html';
      else if (currentRole === 'CAREGIVER') fallbackPage = 'caregiver-dashboard.html';
      else if (currentRole === 'ADMIN') fallbackPage = 'admin-dashboard.html';

      alert(`Access Restricted: Your current account (${currentRole}) does not have permission for this portal.`);
      window.location.href = fallbackPage;
      return false;
    }

    return true;
  }

  // 7. EXPORT UTILITIES TO GLOBAL SCOPE
  const MediCareAPI = {
    API_BASE_URL,
    resolveApiBaseUrl,
    getAuthToken,
    setAuthToken,
    clearAuthToken,
    getCurrentUser,
    getUserRole,
    parseJwt,
    apiFetch,
    apiGet,
    apiPost,
    apiPut,
    apiDelete,
    login,
    register,
    checkRole,
  };

  window.MediCareAPI = MediCareAPI;
  window.apiFetch = apiFetch;
  window.getAuthToken = getAuthToken;
  window.setAuthToken = setAuthToken;
  window.clearAuthToken = clearAuthToken;
  window.getCurrentUser = getCurrentUser;
  window.getUserRole = getUserRole;
  window.checkRole = checkRole;
  window.API_BASE_URL = API_BASE_URL;

})(window);
