// notifications.js

let userPayload;

document.addEventListener('DOMContentLoaded', async () => {
    userPayload = getJwtPayload();
    if (!userPayload) {
        window.location.href = 'login.html';
        return;
    }

    const role = (userPayload.role || userPayload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']).toLowerCase();
    setupSidebar(role);

    if (role === 'patient') {
        document.getElementById('nav-initial').textContent = 'P';
    } else if (role === 'caregiver') {
        document.getElementById('nav-initial').textContent = 'C';
    } else if (role === 'admin') {
        document.getElementById('nav-initial').textContent = 'A';
    }

    await loadNotifications();
});

function setupSidebar(role) {
    const menu = document.getElementById('sidebarMenu');
    let html = '';
    
    if (role === 'patient') {
        html = `
            <a href="patient-dashboard.html" class="menu-item"><i data-lucide="layout-dashboard"></i> Dashboard</a>
            <a href="medicines.html" class="menu-item"><i data-lucide="pill"></i> My Medicines</a>
            <a href="schedule.html" class="menu-item"><i data-lucide="calendar"></i> Schedule</a>
            <a href="medicine-logs.html" class="menu-item"><i data-lucide="clipboard-list"></i> History</a>
            <a href="notifications.html" class="menu-item active"><i data-lucide="bell"></i> Notifications</a>
            <a href="emergency.html" class="menu-item text-danger" style="color: var(--danger);"><i data-lucide="alert-circle"></i> Emergency</a>
        `;
    } else if (role === 'caregiver') {
        html = `
            <a href="caregiver-dashboard.html" class="menu-item"><i data-lucide="layout-dashboard"></i> Dashboard</a>
            <a href="patients.html" class="menu-item"><i data-lucide="users"></i> Patients</a>
            <a href="notifications.html" class="menu-item active"><i data-lucide="bell"></i> Notifications</a>
            <a href="emergency.html" class="menu-item text-danger" style="color: var(--danger);"><i data-lucide="alert-circle"></i> Emergency Alerts</a>
        `;
    } else if (role === 'admin') {
        html = `
            <a href="admin-dashboard.html" class="menu-item"><i data-lucide="layout-dashboard"></i> Dashboard</a>
            <a href="emergency.html" class="menu-item text-danger" style="color: var(--danger);"><i data-lucide="alert-circle"></i> Emergency Alerts</a>
        `;
    }
    
    menu.innerHTML = html;
    lucide.createIcons();
}

async function loadNotifications() {
    try {
        const data = await api.get('/notification');
        renderNotifications(data);
    } catch (e) {
        // Handled
    }
}

function renderNotifications(notifications) {
    const container = document.getElementById('notificationsContainer');
    if (!notifications || notifications.length === 0) {
        container.innerHTML = '<div style="padding: 2rem; text-align: center; color: var(--text-muted);">You\'re all caught up.</div>';
        return;
    }

    container.innerHTML = notifications.map(n => {
        let icon = 'bell';
        let color = 'var(--text-muted)';
        
        if (n.title.toLowerCase().includes('emergency')) {
            icon = 'alert-triangle';
            color = 'var(--danger)';
        } else if (n.title.toLowerCase().includes('missed')) {
            icon = 'x-circle';
            color = 'var(--danger)';
        } else if (n.title.toLowerCase().includes('stock')) {
            icon = 'package-minus';
            color = 'var(--warning)';
        }

        const unreadClass = n.isRead ? '' : 'unread';

        return `
            <div class="notification-card ${unreadClass}" onclick="markAsRead(${n.id}, ${n.isRead})">
                <div class="notification-icon" style="color: ${color}; background: ${color}22;">
                    <i data-lucide="${icon}"></i>
                </div>
                <div class="notification-content">
                    <div class="notification-title">${n.title}</div>
                    <div class="notification-message">${n.message}</div>
                    <div class="notification-time">${new Date(n.createdAt).toLocaleString()}</div>
                </div>
            </div>
        `;
    }).join('');

    lucide.createIcons();
}

async function markAsRead(id, isRead) {
    if (isRead) return;
    try {
        await api.put(`/notification/${id}/read`, {});
        await loadNotifications();
    } catch (e) {
        // Handled
    }
}

async function markAllAsRead() {
    try {
        await api.put('/notification/read-all', {});
        showToast("All notifications marked as read.", "success");
        await loadNotifications();
    } catch (e) {
        // Handled
    }
}
