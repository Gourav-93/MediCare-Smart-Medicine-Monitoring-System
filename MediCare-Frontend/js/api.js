/**
 * MEDICARE PLATFORM - CENTRALIZED API & AUTHENTICATION HANDLER
 * File: js/api.js
 * Description: Unified client wrapper for ASP.NET Core backend communication,
 *              JWT bearer token persistence, 401 interception, and role access guards.
 */

(function (window) {
  'use strict';

  // 1. CONFIGURATION CONSTANTS
  // The .NET Core backend runs on port 5000 (and 5095).
  // If the page is hosted by Kestrel itself (port 5000 or 5095), use relative origin.
  // When running on VS Code Live Server (e.g. 5500, 5501) or other dev servers, point to http://localhost:5000/api.
  function resolveApiBaseUrl() {
    if (typeof window !== 'undefined' && window.MEDICARE_API_BASE_URL) {
      return window.MEDICARE_API_BASE_URL;
    }
    if (typeof window !== 'undefined' && window.location) {
      const port = String(window.location.port || '');
      if (port === '5000' || port === '5095') {
        return `${window.location.origin}/api`;
      }
    }
    return 'http://localhost:5000/api';
  }
  const API_BASE_URL = resolveApiBaseUrl();
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
    // Keep backwards compatibility with older scripts
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
    if (!token) return null; // No role when not logged in
    const user = getCurrentUser();
    if (user && user.role) return user.role.toUpperCase();
    const storedRole = localStorage.getItem(ROLE_KEY) || localStorage.getItem('userRole');
    if (storedRole && storedRole !== 'null' && storedRole !== 'undefined') return storedRole.toUpperCase();
    return 'PATIENT';
  }

  // 3. GENERIC API FETCH WRAPPER
  /**
   * Robust fetch wrapper with automatic JWT injection, error normalization,
   * and 401 Unauthorized redirection.
   *
   * @param {string} endpoint - API path (e.g. '/medicine', 'dashboard/patient') or full URL
   * @param {string} method - HTTP Verb: GET, POST, PUT, DELETE, PATCH
   * @param {any} body - Request payload (Object, Array, FormData, or string)
   * @param {object} options - Optional fetch configuration overrides
   * @returns {Promise<any>} Cleanly parsed JSON response
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

    // 4. Execute Network Request
    try {
      const response = await fetch(url, fetchConfig);

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
        const errorMessage =
          (data && (data.message || data.title || data.error)) ||
          `API Request failed with status ${response.status} (${response.statusText})`;
        const error = new Error(errorMessage);
        error.status = response.status;
        error.data = data;
        throw error;
      }

      return data;
    } catch (err) {
      console.error(`[MediCare API] Error during ${method} ${url}:`, err);
      throw err;
    }
  }

  // 4. CONVENIENCE HTTP VERB HELPERS
  const apiGet = (endpoint, options) => apiFetch(endpoint, 'GET', null, options);
  const apiPost = (endpoint, body, options) => apiFetch(endpoint, 'POST', body, options);
  const apiPut = (endpoint, body, options) => apiFetch(endpoint, 'PUT', body, options);
  const apiDelete = (endpoint, options) => apiFetch(endpoint, 'DELETE', null, options);

  // 5. ROLE-BASED ACCESS CONTROL GUARD
  /**
   * Guard a page based on authorized roles.
   * Redirects unauthorized users to login.html or to their corresponding home dashboard.
   *
   * @param {string[]} allowedRoles - Array of allowed roles, e.g., ['PATIENT', 'CAREGIVER', 'ADMIN']
   * @returns {boolean} True if authorized, false if redirected
   */
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

  // 6. EXPORT UTILITIES TO GLOBAL SCOPE
  const MediCareAPI = {
    API_BASE_URL,
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
