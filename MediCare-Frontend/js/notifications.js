// Notifications Controller
let notificationsList = [];
let userRole = null;

document.addEventListener('DOMContentLoaded', async () => {
    const user = Auth.requireAuth(['PATIENT', 'CAREGIVER', 'ADMIN']);
    if (!user) return;

    userRole = Auth.getRole();
    setupSidebar(userRole);
    setupUI();

    await loadNotifications();

    const form = document.getElementById('broadcastForm');
    if (form) form.addEventListener('submit', handleBroadcastSubmit);
});

function setupSidebar(role) {
    const menu = document.getElementById('sidebarMenu');
    const brandLink = document.getElementById('brandLink');
    const roleLabel = document.getElementById('sidebarRoleLabel');

    if (role === 'PATIENT') {
        if (brandLink) brandLink.href = 'patient-dashboard.html';
        if (roleLabel) roleLabel.textContent = 'Patient';
        menu.innerHTML = `
            <div class="menu-category">Health Dashboard</div>
            <a href="patient-dashboard.html" class="menu-item"><i data-lucide="layout-grid"></i><span>Bento Hub</span></a>
            <a href="medicines.html" class="menu-item"><i data-lucide="pill"></i><span>My Medicines</span></a>
            <a href="schedule.html" class="menu-item"><i data-lucide="calendar"></i><span>Dose Schedule</span></a>
            <a href="medicine-logs.html" class="menu-item"><i data-lucide="clipboard-list"></i><span>Intake History</span></a>
            <a href="adherence.html" class="menu-item"><i data-lucide="trending-up"></i><span>Adherence Reports</span></a>
            <div class="menu-category">Communication & Care</div>
            <a href="notifications.html" class="menu-item active"><i data-lucide="bell"></i><span>Notifications</span></a>
            <a href="emergency.html" class="menu-item text-danger" style="color: #f87171;"><i data-lucide="alert-octagon"></i><span>Emergency Alerts</span></a>
            <a href="profile.html" class="menu-item"><i data-lucide="user"></i><span>My Profile</span></a>
        `;
    } else if (role === 'CAREGIVER') {
        if (brandLink) brandLink.href = 'caregiver-dashboard.html';
        if (roleLabel) roleLabel.textContent = 'Caregiver';
        menu.innerHTML = `
            <div class="menu-category">Patient Monitoring</div>
            <a href="caregiver-dashboard.html" class="menu-item"><i data-lucide="layout-dashboard"></i><span>Overview</span></a>
            <a href="patients.html" class="menu-item"><i data-lucide="users"></i><span>Linked Patients</span></a>
            <a href="medicines.html" class="menu-item"><i data-lucide="pill"></i><span>Prescriptions</span></a>
            <a href="schedule.html" class="menu-item"><i data-lucide="calendar"></i><span>Schedules</span></a>
            <a href="medicine-logs.html" class="menu-item"><i data-lucide="clipboard-list"></i><span>Intake Logs</span></a>
            <div class="menu-category">Safety & Alerts</div>
            <a href="emergency.html" class="menu-item text-danger" style="color: #f87171;"><i data-lucide="alert-octagon"></i><span>Emergency Alerts</span></a>
            <a href="notifications.html" class="menu-item active"><i data-lucide="bell"></i><span>Notifications</span></a>
            <a href="profile.html" class="menu-item"><i data-lucide="user"></i><span>My Profile</span></a>
        `;
    } else if (role === 'ADMIN') {
        if (brandLink) brandLink.href = 'admin-dashboard.html';
        if (roleLabel) roleLabel.textContent = 'Admin';
        menu.innerHTML = `
            <div class="menu-category">System Administration</div>
            <a href="admin-dashboard.html" class="menu-item"><i data-lucide="layout-dashboard"></i><span>Overview</span></a>
            <a href="medicines.html" class="menu-item"><i data-lucide="pill"></i><span>All Medicines</span></a>
            <a href="schedule.html" class="menu-item"><i data-lucide="calendar"></i><span>Dose Schedules</span></a>
            <a href="medicine-logs.html" class="menu-item"><i data-lucide="clipboard-list"></i><span>Intake Logs</span></a>
            <div class="menu-category">System Health & Alerts</div>
            <a href="emergency.html" class="menu-item text-danger" style="color: #f87171;"><i data-lucide="alert-octagon"></i><span>Emergency Alerts</span></a>
            <a href="notifications.html" class="menu-item active"><i data-lucide="bell"></i><span>Broadcast Notifications</span></a>
            <a href="profile.html" class="menu-item"><i data-lucide="user"></i><span>My Profile</span></a>
        `;
    }
    if (window.lucide) lucide.createIcons();
}

function setupUI() {
    if (userRole === 'ADMIN') {
        const btnBroadcast = document.getElementById('btnAdminBroadcast');
        if (btnBroadcast) btnBroadcast.style.display = 'inline-flex';
    }

    const toggleBtn = document.getElementById('sidebarToggle');
    const sidebar = document.getElementById('sidebar');
    if (toggleBtn && sidebar) {
        toggleBtn.addEventListener('click', () => {
            sidebar.classList.toggle('open');
        });
    }
}

async function loadNotifications() {
    try {
        const data = await api.get('/notification');
        notificationsList = Array.isArray(data) ? data : [];
        renderNotificationsList(notificationsList);
    } catch (err) {
        console.error('Failed to load notifications:', err);
    }
}

function renderNotificationsList(notifications = []) {
    const container = document.getElementById('notificationsContainer');
    if (!notifications || notifications.length === 0) {
        container.innerHTML = `
            <div class="empty-state">
                <i data-lucide="bell-off" class="empty-state-icon"></i>
                <div class="empty-state-title">You're all caught up!</div>
                <div class="empty-state-desc">No notifications or unread alerts at this time.</div>
            </div>
        `;
        if (window.lucide) lucide.createIcons({ root: container });
        return;
    }

    container.innerHTML = notifications.map(n => {
        let iconName = 'bell';
        let bgStyle = 'background: rgba(37, 99, 235, 0.1); color: var(--primary);';

        const typeLower = String(n.type || '').toLowerCase();
        const titleLower = String(n.title || '').toLowerCase();

        if (typeLower.includes('emergency') || titleLower.includes('emergency')) {
            iconName = 'alert-octagon';
            bgStyle = 'background: rgba(239, 68, 68, 0.12); color: var(--danger);';
        } else if (titleLower.includes('missed') || typeLower.includes('missed')) {
            iconName = 'x-circle';
            bgStyle = 'background: rgba(239, 68, 68, 0.1); color: var(--danger);';
        } else if (titleLower.includes('stock') || typeLower.includes('stock')) {
            iconName = 'package-minus';
            bgStyle = 'background: rgba(245, 158, 11, 0.1); color: var(--warning-dark);';
        }

        const unreadClass = n.isRead ? '' : 'unread';

        return `
            <div class="notification-card ${unreadClass}" onclick="markNotificationRead(${n.id}, ${n.isRead})">
                <div class="notification-icon-wrap" style="${bgStyle}">
                    <i data-lucide="${iconName}"></i>
                </div>
                <div class="notification-content">
                    <div class="notification-header-row">
                        <span class="notification-title">${Utils.escapeHtml(n.title)}</span>
                        <span class="notification-time">${Utils.formatDateTime(n.createdAt)}</span>
                    </div>
                    <div class="notification-message">${Utils.escapeHtml(n.message)}</div>
                </div>
            </div>
        `;
    }).join('');

    if (window.lucide) lucide.createIcons({ root: container });
}

window.markNotificationRead = async function(id, isRead) {
    if (isRead) return;
    try {
        await api.put(`/notification/${id}/read`);
        await loadNotifications();
    } catch (err) {
        console.error('Failed to mark notification read:', err);
    }
};

window.markAllNotificationsAsRead = async function() {
    try {
        await api.put('/notification/read-all');
        Utils.showToast('All notifications marked as read.', 'success');
        await loadNotifications();
    } catch (err) {
        console.error('Failed to mark all as read:', err);
    }
};

window.openBroadcastModal = function() {
    document.getElementById('broadcastForm').reset();
    Utils.openModal('broadcastModal');
};

async function handleBroadcastSubmit(e) {
    e.preventDefault();
    const submitBtn = document.getElementById('btnSubmitBroadcast');

    const payload = {
        userId: parseInt(document.getElementById('notifUserId').value, 10),
        title: document.getElementById('notifTitle').value.trim(),
        message: document.getElementById('notifMessage').value.trim(),
        type: document.getElementById('notifType').value
    };

    try {
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<span class="spinner" style="width: 16px; height: 16px; border-width: 2px;"></span> Sending...';

        await api.post('/notification', payload);
        Utils.showToast('Notification sent successfully.', 'success');
        Utils.closeModal('broadcastModal');
        await loadNotifications();

    } catch (err) {
        console.error('Failed to broadcast notification:', err);
    } finally {
        submitBtn.disabled = false;
        submitBtn.innerHTML = '<i data-lucide="send"></i> Send Notification';
        if (window.lucide) lucide.createIcons({ root: submitBtn });
    }
}
