// auth.js

function requireAuth(allowedRoles = []) {
    const token = localStorage.getItem('token');
    if (!token) {
        window.location.href = 'login.html';
        return null;
    }

    const payload = getJwtPayload();
    if (!payload) {
        localStorage.removeItem('token');
        window.location.href = 'login.html';
        return null;
    }

    // Role check (JWT claim keys might vary, check common ones)
    const role = payload.role || payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
    
    // Normalize role string if needed, e.g., to lowercase
    const normalizedRole = role ? role.toLowerCase() : '';

    // If allowedRoles is provided, ensure user has one of them
    if (allowedRoles.length > 0) {
        const hasRole = allowedRoles.map(r => r.toLowerCase()).includes(normalizedRole);
        if (!hasRole) {
            // Redirect based on actual role
            redirectByRole(normalizedRole);
            return null;
        }
    }

    return payload;
}

function redirectByRole(role) {
    role = role ? role.toLowerCase() : '';
    if (role === 'patient') {
        window.location.href = 'patient-dashboard.html';
    } else if (role === 'caregiver') {
        window.location.href = 'caregiver-dashboard.html';
    } else if (role === 'admin') {
        window.location.href = 'admin-dashboard.html';
    } else {
        window.location.href = 'login.html';
    }
}

function logout() {
    localStorage.removeItem('token');
    window.location.href = 'login.html';
}

function handleLogin(email, password) {
    return api.post('/auth/login', { email, password }).then(data => {
        if (data && data.token) {
            localStorage.setItem('token', data.token);
            const payload = getJwtPayload();
            const role = payload.role || payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
            redirectByRole(role);
        }
    });
}

function handleRegister(data) {
    return api.post('/auth/register', data);
}
