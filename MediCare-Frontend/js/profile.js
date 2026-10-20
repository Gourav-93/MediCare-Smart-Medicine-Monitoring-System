// Profile & Account Controller
document.addEventListener('DOMContentLoaded', async () => {
    const user = Auth.requireAuth(['PATIENT', 'CAREGIVER', 'ADMIN']);
    if (!user) return;

    const role = Auth.getRole();
    setupSidebar(role);

    // Populate profile fields from verified user object
    const name = user.name || (user.email ? user.email.split('@')[0] : 'User');
    const email = user.email || 'No email provided';
    const userId = user.userId || 'N/A';
    const roleFormatted = role.charAt(0).toUpperCase() + role.slice(1).toLowerCase();

    // Elements
    const avatarEl = document.getElementById('profileAvatarLarge');
    const nameEl = document.getElementById('profileName');
    const emailEl = document.getElementById('profileEmail');
    const roleBadgeEl = document.getElementById('profileRoleBadge');
    
    const profUserId = document.getElementById('profUserId');
    const profRoleText = document.getElementById('profRoleText');
    const profEmailText = document.getElementById('profEmailText');

    if (avatarEl) avatarEl.textContent = name.charAt(0).toUpperCase();
    if (nameEl) nameEl.textContent = name;
    if (emailEl) emailEl.textContent = email;

    if (roleBadgeEl) {
        let badgeClass = 'badge-info';
        if (role === 'ADMIN') badgeClass = 'badge-purple';
        else if (role === 'CAREGIVER') badgeClass = 'badge-primary';
        else if (role === 'PATIENT') badgeClass = 'badge-success';
        roleBadgeEl.innerHTML = `<span class="badge ${badgeClass}" style="font-size: 0.85rem; padding: 0.35rem 0.85rem;">${role}</span>`;
    }

    if (profUserId) profUserId.value = userId;
    if (profRoleText) profRoleText.value = roleFormatted;
    if (profEmailText) profEmailText.value = email;

    // Mobile sidebar toggle support
    const sidebarToggle = document.getElementById('sidebarToggle');
    if (sidebarToggle) {
        sidebarToggle.addEventListener('click', () => {
            const sidebar = document.getElementById('sidebar');
            if (sidebar) sidebar.classList.toggle('active');
        });
    }

    lucide.createIcons();
});

function setupSidebar(role) {
    const menu = document.getElementById('sidebarMenu');
    const brandLink = document.getElementById('brandLink');
    const roleLabel = document.getElementById('sidebarRoleLabel');

    if (!menu) return;

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
            <a href="notifications.html" class="menu-item"><i data-lucide="bell"></i><span>Notifications</span></a>
            <a href="emergency.html" class="menu-item text-danger" style="color: #f87171;"><i data-lucide="alert-octagon"></i><span>Emergency Alerts</span></a>
            <a href="profile.html" class="menu-item active"><i data-lucide="user"></i><span>My Profile</span></a>
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
            <a href="notifications.html" class="menu-item"><i data-lucide="bell"></i><span>Notifications</span></a>
            <a href="profile.html" class="menu-item active"><i data-lucide="user"></i><span>My Profile</span></a>
        `;
    } else if (role === 'ADMIN') {
        if (brandLink) brandLink.href = 'admin-dashboard.html';
        if (roleLabel) roleLabel.textContent = 'Admin';
        menu.innerHTML = `
            <div class="menu-category">System Administration</div>
            <a href="admin-dashboard.html" class="menu-item"><i data-lucide="layout-dashboard"></i><span>Overview</span></a>
            <a href="medicines.html" class="menu-item"><i data-lucide="pill"></i><span>All Medicines</span></a>
            <a href="schedule.html" class="menu-item"><i data-lucide="calendar"></i><span>All Schedules</span></a>
            <a href="medicine-logs.html" class="menu-item"><i data-lucide="clipboard-list"></i><span>Intake Records</span></a>
            <div class="menu-category">Safety & Alerts</div>
            <a href="emergency.html" class="menu-item text-danger" style="color: #f87171;"><i data-lucide="alert-octagon"></i><span>Emergency Alerts</span></a>
            <a href="notifications.html" class="menu-item"><i data-lucide="bell"></i><span>Notifications</span></a>
            <a href="profile.html" class="menu-item active"><i data-lucide="user"></i><span>My Profile</span></a>
        `;
    }

    lucide.createIcons();
}
