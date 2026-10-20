// MediCare Centralized API Layer
const api = {
    async request(endpoint, options = {}) {
        // Ensure endpoint starts with a slash
        const normalizedEndpoint = endpoint.startsWith('/') ? endpoint : `/${endpoint}`;
        const url = `${CONFIG.API_BASE}${normalizedEndpoint}`;

        // Retrieve current JWT token from Auth module or storage
        const token = Auth ? Auth.getToken() : localStorage.getItem(CONFIG.STORAGE_KEYS.TOKEN);

        const headers = {
            'Accept': 'application/json',
            ...(options.body && !(options.body instanceof FormData) && { 'Content-Type': 'application/json' }),
            ...(token && { 'Authorization': `Bearer ${token}` }),
            ...options.headers
        };

        const fetchConfig = {
            ...options,
            headers
        };

        try {
            Utils.showLoader();
            const response = await fetch(url, fetchConfig);

            // Handle 401 Unauthorized (Expired / Missing Token)
            if (response.status === 401) {
                if (Auth) {
                    Auth.clearSession();
                } else {
                    localStorage.removeItem(CONFIG.STORAGE_KEYS.TOKEN);
                    localStorage.removeItem(CONFIG.STORAGE_KEYS.USER);
                }
                
                // Do not redirect in an endless loop if already on login page
                if (!window.location.pathname.endsWith('login.html') && !window.location.pathname.endsWith('register.html')) {
                    Utils.showToast('Your session has expired. Please sign in again.', 'warning');
                    setTimeout(() => {
                        window.location.href = 'login.html';
                    }, 1200);
                }
                return null;
            }

            // Handle 403 Forbidden (Insufficient Role Permissions)
            if (response.status === 403) {
                const msg = 'You are not authorized to perform this operation with your current role.';
                Utils.showToast(msg, 'error');
                throw new Error(msg);
            }

            // Handle 204 No Content
            if (response.status === 204) {
                return true;
            }

            // Safely parse JSON or text response
            const contentType = response.headers.get('content-type') || '';
            let responseData = null;

            if (contentType.includes('application/json') || contentType.includes('application/problem+json')) {
                responseData = await response.json().catch(() => null);
            } else {
                responseData = await response.text().catch(() => '');
            }

            // If response is not OK (e.g. 400, 404, 500)
            if (!response.ok) {
                let errorMessage = 'An unexpected server error occurred.';

                if (responseData) {
                    if (typeof responseData === 'string' && responseData.trim().length > 0) {
                        errorMessage = responseData;
                    } else if (responseData.message) {
                        errorMessage = responseData.message;
                    } else if (responseData.title) {
                        errorMessage = responseData.title;
                    }

                    // Extract detailed ASP.NET Core ModelState validation errors if present
                    if (responseData.errors && typeof responseData.errors === 'object') {
                        const errorDetails = Object.values(responseData.errors)
                            .flat()
                            .filter(Boolean)
                            .join(' ');
                        if (errorDetails) {
                            errorMessage = `${errorMessage} ${errorDetails}`;
                        }
                    }
                }

                Utils.showToast(errorMessage, 'error');
                const err = new Error(errorMessage);
                err.status = response.status;
                err.data = responseData;
                throw err;
            }

            return responseData;

        } catch (error) {
            // Check for network connection / CORS error
            if (error.name === 'TypeError' && error.message === 'Failed to fetch') {
                const netErrMsg = `Unable to connect to MediCare API at ${CONFIG.API_BASE}. Please ensure the backend is running and CORS is enabled.`;
                console.error(netErrMsg, error);
                Utils.showToast(netErrMsg, 'error', 5000);
            }
            throw error;
        } finally {
            Utils.hideLoader();
        }
    },

    get(endpoint, params = null) {
        let url = endpoint;
        if (params && typeof params === 'object') {
            const queryParams = new URLSearchParams();
            Object.entries(params).forEach(([key, val]) => {
                if (val !== null && val !== undefined && val !== '') {
                    queryParams.append(key, val);
                }
            });
            const qs = queryParams.toString();
            if (qs) {
                url += (url.includes('?') ? '&' : '?') + qs;
            }
        }
        return this.request(url, { method: 'GET' });
    },

    post(endpoint, body = {}) {
        return this.request(endpoint, {
            method: 'POST',
            body: JSON.stringify(body)
        });
    },

    put(endpoint, body = {}) {
        return this.request(endpoint, {
            method: 'PUT',
            body: JSON.stringify(body)
        });
    },

    delete(endpoint, body = null) {
        const options = { method: 'DELETE' };
        if (body) {
            options.body = JSON.stringify(body);
        }
        return this.request(endpoint, options);
    }
};
