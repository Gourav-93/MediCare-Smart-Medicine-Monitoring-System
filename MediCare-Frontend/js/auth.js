// MediCare Authentication & Session Guard Module
const Auth = {
    // Retrieve stored JWT token
    getToken() {
        return sessionStorage.getItem(CONFIG.STORAGE_KEYS.TOKEN) || localStorage.getItem(CONFIG.STORAGE_KEYS.TOKEN);
    },

    // Check if active authenticated session exists
    isAuthenticated() {
        return !!this.getToken() && !!this.getRole();
    },

    // Retrieve cached user profile
    getUser() {
        try {
            const raw = sessionStorage.getItem(CONFIG.STORAGE_KEYS.USER) || localStorage.getItem(CONFIG.STORAGE_KEYS.USER);
            return raw ? JSON.parse(raw) : null;
        } catch {
            return null;
        }
    },

    // Save session credentials
    saveSession(token, userProfile) {
        // Preferred session-based storage per architectural security guidelines
        sessionStorage.setItem(CONFIG.STORAGE_KEYS.TOKEN, token);
        sessionStorage.setItem(CONFIG.STORAGE_KEYS.USER, JSON.stringify(userProfile));

        // Also persist in localStorage for multi-tab continuity
        localStorage.setItem(CONFIG.STORAGE_KEYS.TOKEN, token);
        localStorage.setItem(CONFIG.STORAGE_KEYS.USER, JSON.stringify(userProfile));
    },

    // Clear session on logout or expiration
    clearSession() {
        sessionStorage.removeItem(CONFIG.STORAGE_KEYS.TOKEN);
        sessionStorage.removeItem(CONFIG.STORAGE_KEYS.USER);
        localStorage.removeItem(CONFIG.STORAGE_KEYS.TOKEN);
        localStorage.removeItem(CONFIG.STORAGE_KEYS.USER);
    },

    // Decode JWT payload claims
    getJwtPayload() {
        const token = this.getToken();
        if (!token) return null;
        try {
            const parts = token.split('.');
            if (parts.length !== 3) return null;
            const base64Url = parts[1];
            const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
            const jsonPayload = decodeURIComponent(
                atob(base64)
                    .split('')
                    .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
                    .join('')
            );
            return JSON.parse(jsonPayload);
        } catch (e) {
            console.warn('Failed to parse JWT payload', e);
            return null;
        }
    },

    // Determine current user's role
    getRole() {
        const user = this.getUser();
        if (user && user.role) return user.role.toUpperCase();

        const payload = this.getJwtPayload();
        if (!payload) return null;

        const roleClaim = payload.role || 
                          payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ||
                          payload['Role'];
        return roleClaim ? String(roleClaim).toUpperCase() : null;
    },

    // Authentication Guard for protected dashboard pages
    requireAuth(allowedRoles = []) {
        const token = this.getToken();
        if (!token) {
            window.location.href = 'login.html';
            return null;
        }

        const role = this.getRole();
        if (!role) {
            this.clearSession();
            window.location.href = 'login.html';
            return null;
        }

        // Verify if current role is permitted for this page
        if (allowedRoles && allowedRoles.length > 0) {
            const normalizedAllowed = allowedRoles.map(r => String(r).toUpperCase());
            if (!normalizedAllowed.includes(role)) {
                Utils.showToast(`Access denied. Page restricted to ${allowedRoles.join(' / ')}.`, 'error');
                this.redirectByRole(role);
                return null;
            }
        }

        return this.getUser() || { role };
    },

    // Role-based dashboard redirector
    redirectByRole(role) {
        if (!role) {
            window.location.href = 'login.html';
        } else {
            window.location.href = 'dashboard.html';
        }
    },

    // Render consistent sidebar navigation based on active role
    renderSidebar(activePage = '') {
        const sidebar = document.getElementById('sidebar');
        const sidebarMenu = document.getElementById('sidebarMenu');
        const brandLink = document.getElementById('brandLink');
        const role = this.getRole() || '';

        if (brandLink) {
            brandLink.href = 'dashboard.html';
            brandLink.className = 'sidebar-brand';
            brandLink.innerHTML = `
                <div class="sidebar-brand-icon">
                    <i data-lucide="box"></i>
                </div>
                <div class="sidebar-brand-text">
                    <span class="sidebar-brand-title">MediCare</span>
                    <span class="sidebar-brand-subtitle">Health Management</span>
                </div>
            `;
        }

        if (!sidebarMenu) return;

        const navItems = [
            { id: 'dashboard', label: 'Dashboard', href: 'dashboard.html', icon: 'layout-grid', roles: ['PATIENT', 'CAREGIVER', 'ADMIN'] },
            { id: 'medicines', label: role === 'ADMIN' ? 'Medicines (Products)' : 'My Medicines', href: 'medicines.html', icon: 'package', roles: ['PATIENT', 'CAREGIVER', 'ADMIN'] },
            { id: 'schedules', label: role === 'ADMIN' ? 'Schedules (Categories)' : 'Dose Schedules', href: 'schedules.html', icon: 'tag', roles: ['PATIENT', 'CAREGIVER', 'ADMIN'] },
            { id: 'caregiver-patient', label: role === 'PATIENT' ? 'My Caregivers (Suppliers)' : (role === 'CAREGIVER' ? 'My Patients (Customers)' : 'Caregivers & Patients'), href: 'caregiver-patient.html', icon: 'truck', roles: ['PATIENT', 'CAREGIVER', 'ADMIN'] },
            { id: 'logs', label: 'Intake Logs (Orders)', href: 'medicine-logs.html', icon: 'clipboard-list', roles: ['PATIENT', 'CAREGIVER', 'ADMIN'] },
            { id: 'adherence', label: 'Adherence & Reports', href: 'adherence.html', icon: 'shopping-cart', roles: ['PATIENT', 'CAREGIVER', 'ADMIN'] },
            { id: 'stock', label: 'Inventory Stock', href: 'medicines.html#stock', icon: 'archive', roles: ['PATIENT', 'CAREGIVER', 'ADMIN'] },
            { id: 'emergency', label: 'Emergency Safety', href: 'emergency.html', icon: 'alert-triangle', danger: true, roles: ['PATIENT', 'CAREGIVER', 'ADMIN'] },
            { id: 'notifications', label: 'Notifications', href: 'notifications.html', icon: 'bell', roles: ['PATIENT', 'CAREGIVER', 'ADMIN'] },
            { id: 'profile', label: 'Settings', href: 'profile.html', icon: 'settings', roles: ['PATIENT', 'CAREGIVER', 'ADMIN'] }
        ];

        let html = '<div class="menu-category">WORKSPACE</div>';
        navItems.forEach(item => {
            if (item.roles.includes(role)) {
                const isActive = activePage === item.id ? 'active' : '';
                const dangerClass = item.danger ? 'style="color: #ef4444;"' : '';
                html += `
                    <a href="${item.href}" class="menu-item ${isActive}" ${dangerClass}>
                        <i data-lucide="${item.icon}"></i>
                        <span>${item.label}</span>
                    </a>
                `;
            }
        });

        sidebarMenu.innerHTML = html;

        // Ensure footer exists in sidebar
        if (sidebar && !document.getElementById('sidebarFooter')) {
            const footerEl = document.createElement('div');
            footerEl.id = 'sidebarFooter';
            footerEl.className = 'sidebar-footer';
            footerEl.innerHTML = `
                <div class="sidebar-footer-title">MediCare Frontend</div>
                <div class="sidebar-footer-sub">Demo environment</div>
            `;
            sidebar.appendChild(footerEl);
        }

        if (window.lucide) {
            lucide.createIcons();
        }
    },

    // Handle Login request to verified backend route
    async handleLogin(email, password) {
        if (!email || !password) {
            throw new Error('Email and password are required.');
        }

        const response = await api.post('/AuthController/login', {
            email: email.trim(),
            password: password
        });

        if (response && response.token) {
            const userProfile = {
                userId: response.userId,
                name: response.name,
                email: response.email,
                role: response.role
            };

            this.saveSession(response.token, userProfile);
            return userProfile;
        } else {
            throw new Error('Invalid login response from server.');
        }
    },

    // Handle Register request to verified backend route
    async handleRegister(data) {
        // Enforce registration restrictions (Public registration only PATIENT or CAREGIVER)
        const role = String(data.role || '').toUpperCase();
        if (role !== 'PATIENT' && role !== 'CAREGIVER') {
            throw new Error('Public registration is permitted for Patient and Caregiver accounts only.');
        }

        const payload = {
            name: data.name.trim(),
            email: data.email.trim().toLowerCase(),
            password: data.password,
            role: role
        };

        return await api.post('/AuthController/register', payload);
    },

    // Sign out action
    logout() {
        this.clearSession();
        window.location.href = 'login.html';
    },

    // Setup navbar with profile information
    setupNavbar() {
        const user = this.getUser();
        if (!user) return;

        const nameEl = document.getElementById('nav-username');
        const roleEl = document.getElementById('nav-role');
        const initialEl = document.getElementById('nav-initial');

        if (nameEl) nameEl.textContent = user.name || user.email;
        if (roleEl) roleEl.textContent = user.role ? (user.role.charAt(0) + user.role.slice(1).toLowerCase()) : '';
        if (initialEl) initialEl.textContent = Utils.getInitials(user.name || user.email);
    }
};

// Global shorthand methods for legacy or inline DOM references
function requireAuth(roles) { return Auth.requireAuth(roles); }
function redirectByRole(role) { Auth.redirectByRole(role); }
function logout() { Auth.logout(); }
function getJwtPayload() { return Auth.getJwtPayload(); }

// Automatically wire navbar info on DOM load
document.addEventListener('DOMContentLoaded', () => {
    Auth.setupNavbar();
    if (window.lucide) {
        lucide.createIcons();
    }
});
