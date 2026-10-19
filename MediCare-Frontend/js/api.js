const API_BASE = 'http://localhost:5095/api'; // Updated to match backend port

const api = {
    async request(endpoint, options = {}) {
        const token = localStorage.getItem('token');
        
        const defaultHeaders = {
            'Content-Type': 'application/json',
            ...(token && { 'Authorization': `Bearer ${token}` })
        };

        const config = {
            ...options,
            headers: {
                ...defaultHeaders,
                ...options.headers
            }
        };

        try {
            showLoader();
            const response = await fetch(`${API_BASE}${endpoint}`, config);
            
            if (response.status === 401) {
                // Unauthorized - token expired or invalid
                localStorage.removeItem('token');
                window.location.href = 'login.html';
                return null;
            }

            if (response.status === 403) {
                showToast('You do not have permission to perform this action', 'error');
                return null;
            }

            // Some endpoints might return 204 No Content
            if (response.status === 204) {
                return true;
            }

            const data = await response.json().catch(() => ({}));

            if (!response.ok) {
                throw new Error(data.message || data.title || 'An error occurred');
            }

            return data;
        } catch (error) {
            console.error('API Error:', error);
            showToast(error.message, 'error');
            throw error;
        } finally {
            hideLoader();
        }
    },

    get(endpoint) {
        return this.request(endpoint, { method: 'GET' });
    },

    post(endpoint, body) {
        return this.request(endpoint, {
            method: 'POST',
            body: JSON.stringify(body)
        });
    },

    put(endpoint, body) {
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
