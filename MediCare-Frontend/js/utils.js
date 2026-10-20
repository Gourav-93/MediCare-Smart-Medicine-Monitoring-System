// MediCare UI & Data Utilities
const Utils = {
    // Safe HTML Escape to prevent XSS injection
    escapeHtml(str) {
        if (str === null || str === undefined) return '';
        const div = document.createElement('div');
        div.textContent = String(str);
        return div.innerHTML;
    },

    // Format Date string (e.g., 2026-10-19 -> Oct 19, 2026)
    formatDate(dateStr) {
        if (!dateStr) return 'N/A';
        const date = new Date(dateStr);
        if (isNaN(date.getTime())) return String(dateStr);
        return date.toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'short',
            day: 'numeric'
        });
    },

    // Format Time string ("08:00:00" or ISO Date -> "8:00 AM")
    formatTime(timeStr) {
        if (!timeStr) return '';
        // If it's a TimeSpan string like "08:30:00" or "08:30"
        if (typeof timeStr === 'string' && timeStr.includes(':') && !timeStr.includes('T')) {
            const parts = timeStr.split(':');
            const d = new Date();
            d.setHours(parseInt(parts[0], 10), parseInt(parts[1], 10), 0);
            return d.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit', hour12: true });
        }
        // If it's an ISO string
        const date = new Date(timeStr);
        if (!isNaN(date.getTime())) {
            return date.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit', hour12: true });
        }
        return String(timeStr);
    },

    // Format full Date and Time (e.g. Oct 19, 2026, 05:30 PM)
    formatDateTime(dateStr) {
        if (!dateStr) return 'N/A';
        const date = new Date(dateStr);
        if (isNaN(date.getTime())) return String(dateStr);
        return date.toLocaleDateString('en-US', {
            month: 'short',
            day: 'numeric',
            year: 'numeric',
            hour: 'numeric',
            minute: '2-digit',
            hour12: true
        });
    },

    // Dynamic greeting based on current local time
    getGreeting(name) {
        const hour = new Date().getHours();
        let greeting = 'Good morning';
        if (hour >= 12 && hour < 17) {
            greeting = 'Good afternoon';
        } else if (hour >= 17 || hour < 4) {
            greeting = 'Good evening';
        }
        return name ? `${greeting}, ${name}` : greeting;
    },

    // Generate user initials for avatar
    getInitials(name) {
        if (!name) return 'U';
        const parts = name.trim().split(/\s+/);
        if (parts.length === 1) return parts[0].charAt(0).toUpperCase();
        return (parts[0].charAt(0) + parts[parts.length - 1].charAt(0)).toUpperCase();
    },

    // Badge markup for Adherence Percentage
    getAdherenceBadge(percent) {
        const p = Math.round(Number(percent) || 0);
        let badgeClass = 'badge-success';
        if (p < 80) badgeClass = 'badge-warning';
        if (p < 50) badgeClass = 'badge-danger';
        return `<span class="badge ${badgeClass}">${p}% Adherence</span>`;
    },

    // Badge markup for Medicine Stock status
    getStockBadge(stock) {
        const s = Number(stock) || 0;
        if (s === 0) {
            return `<span class="badge badge-danger">Out of Stock (0)</span>`;
        }
        if (s <= 5) {
            return `<span class="badge badge-warning">Low Stock (${s})</span>`;
        }
        return `<span class="badge badge-success">In Stock (${s})</span>`;
    },

    // Badge markup for intake status
    getStatusBadge(status) {
        const s = String(status || '').toLowerCase();
        if (s === 'taken') return '<span class="badge badge-success">Taken</span>';
        if (s === 'missed') return '<span class="badge badge-danger">Missed</span>';
        if (s === 'pending') return '<span class="badge badge-warning">Pending</span>';
        return `<span class="badge badge-info">${Utils.escapeHtml(status)}</span>`;
    },

    // Toast Notifications
    showToast(message, type = 'info', duration = 3500) {
        let container = document.getElementById('toast-container');
        if (!container) {
            container = document.createElement('div');
            container.id = 'toast-container';
            document.body.appendChild(container);
        }

        const toast = document.createElement('div');
        toast.className = `toast ${type}`;
        
        let iconName = 'info';
        if (type === 'success') iconName = 'check-circle';
        if (type === 'error') iconName = 'alert-triangle';
        if (type === 'warning') iconName = 'alert-circle';

        toast.innerHTML = `
            <i data-lucide="${iconName}" class="toast-icon"></i>
            <span class="toast-msg">${Utils.escapeHtml(message)}</span>
        `;

        container.appendChild(toast);
        
        if (window.lucide) {
            lucide.createIcons({ root: toast });
        }

        setTimeout(() => {
            toast.style.animation = 'slideOutDown 0.3s ease forwards';
            setTimeout(() => toast.remove(), 300);
        }, duration);
    },

    // Global loading overlay
    showLoader() {
        let loader = document.getElementById('global-loader');
        if (!loader) {
            loader = document.createElement('div');
            loader.id = 'global-loader';
            loader.className = 'loader-overlay';
            loader.innerHTML = `
                <div class="loader-content">
                    <div class="spinner"></div>
                    <div class="loader-text">Processing...</div>
                </div>
            `;
            document.body.appendChild(loader);
        }
        loader.style.display = 'flex';
    },

    hideLoader() {
        const loader = document.getElementById('global-loader');
        if (loader) {
            loader.style.display = 'none';
        }
    },

    // Modal management
    openModal(modalId) {
        const modal = document.getElementById(modalId);
        if (modal) {
            modal.classList.add('active');
            document.body.style.overflow = 'hidden';
            if (window.lucide) lucide.createIcons({ root: modal });
        }
    },

    closeModal(modalId) {
        const modal = document.getElementById(modalId);
        if (modal) {
            modal.classList.remove('active');
            document.body.style.overflow = '';
        }
    },

    // Simple custom confirmation dialog
    confirmAction(message, title = 'Confirmation') {
        return new Promise((resolve) => {
            const confirmed = window.confirm(`${title}\n\n${message}`);
            resolve(confirmed);
        });
    },

    // Theme Management
    initTheme() {
        const savedTheme = localStorage.getItem(CONFIG.STORAGE_KEYS.THEME);
        if (savedTheme) {
            document.documentElement.setAttribute('data-theme', savedTheme);
        } else if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
            document.documentElement.setAttribute('data-theme', 'dark');
        }
    },

    toggleTheme() {
        const current = document.documentElement.getAttribute('data-theme') === 'dark' ? 'dark' : 'light';
        const newTheme = current === 'dark' ? 'light' : 'dark';
        document.documentElement.setAttribute('data-theme', newTheme);
        localStorage.setItem(CONFIG.STORAGE_KEYS.THEME, newTheme);
        if (window.lucide) lucide.createIcons();
    }
};

// Global shorthand functions for legacy HTML inline events
function showToast(msg, type) { Utils.showToast(msg, type); }
function showLoader() { Utils.showLoader(); }
function hideLoader() { Utils.hideLoader(); }
function toggleTheme() { Utils.toggleTheme(); }
function openModal(id) { Utils.openModal(id); }
function closeModal(id) { 
    if (id) {
        Utils.closeModal(id);
    } else {
        document.querySelectorAll('.modal-overlay.active').forEach(m => m.classList.remove('active'));
        document.body.style.overflow = '';
    }
}

// Initialize theme on script evaluation
Utils.initTheme();
