// common.js — Shared Utilities & UI Helpers

function showToast(message, type = 'info') {
    if (window.Utils && typeof Utils.showToast === 'function') {
        Utils.showToast(message, type);
        return;
    }

    let container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        document.body.appendChild(container);
    }

    const toast = document.createElement('div');
    toast.className = `toast ${type}`;
    
    let icon = 'info';
    if (type === 'success') icon = 'check-circle';
    if (type === 'error') icon = 'alert-circle';
    if (type === 'warning') icon = 'alert-triangle';

    toast.innerHTML = `
        <i data-lucide="${icon}"></i>
        <span>${message}</span>
    `;

    container.appendChild(toast);
    
    if (window.lucide) {
        lucide.createIcons({ root: toast });
    }

    setTimeout(() => {
        toast.style.animation = 'slideInUp 0.3s ease reverse forwards';
        setTimeout(() => toast.remove(), 300);
    }, 3000);
}

function showLoader() {
    let loader = document.getElementById('global-loader');
    if (!loader) {
        loader = document.createElement('div');
        loader.id = 'global-loader';
        loader.className = 'loader-overlay';
        loader.innerHTML = '<div class="spinner"></div>';
        document.body.appendChild(loader);
    }
    loader.style.display = 'flex';
}

function hideLoader() {
    const loader = document.getElementById('global-loader');
    if (loader) {
        loader.style.display = 'none';
    }
}

// Dark Mode Toggle
function toggleTheme() {
    if (window.Utils && typeof Utils.toggleTheme === 'function') {
        Utils.toggleTheme();
        return;
    }
    const isDark = document.body.getAttribute('data-theme') === 'dark';
    const newTheme = isDark ? 'light' : 'dark';
    document.body.setAttribute('data-theme', newTheme);
    localStorage.setItem('theme', newTheme);
}

// Init Theme
const savedTheme = localStorage.getItem('theme');
if (savedTheme) {
    document.body.setAttribute('data-theme', savedTheme);
} else if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
    document.body.setAttribute('data-theme', 'dark');
}

// Sidebar Toggle
document.addEventListener('DOMContentLoaded', () => {
    if (window.lucide) {
        lucide.createIcons();
    }

    const sidebarToggle = document.getElementById('sidebarToggle');
    const sidebar = document.querySelector('.sidebar');
    
    if (sidebarToggle && sidebar) {
        sidebarToggle.addEventListener('click', () => {
            sidebar.classList.toggle('active');
            sidebar.classList.toggle('open');
        });
    }

    // Populate user profile info in navbar if available
    const userPayload = getJwtPayload();
    if (userPayload) {
        const usernameEl = document.getElementById('nav-username');
        const roleEl = document.getElementById('nav-role');
        const email = userPayload.email || userPayload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] || '';
        const name = userPayload.name || userPayload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] || (email ? email.split('@')[0] : 'User');
        const role = userPayload.role || userPayload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || '';

        if(usernameEl) usernameEl.textContent = name;
        if(roleEl) roleEl.textContent = role;
    }
});

function getJwtPayload() {
    const token = sessionStorage.getItem('medicare_token') || localStorage.getItem('token') || localStorage.getItem('medicare_token');
    if (!token) return null;
    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));
        return JSON.parse(jsonPayload);
    } catch (e) {
        return null;
    }
}
