// profile.js

document.addEventListener('DOMContentLoaded', () => {
    const userPayload = getJwtPayload();
    if (!userPayload) {
        window.location.href = 'login.html';
        return;
    }

    const role = (userPayload.role || userPayload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']).toLowerCase();
    setupSidebar(role);

    const email = userPayload.email || userPayload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] || 'Unknown Email';
    const name = userPayload.name || userPayload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] || email.split('@')[0];

    document.getElementById('profileAvatar').textContent = name.charAt(0).toUpperCase();
    document.getElementById('profileName').textContent = name;
    document.getElementById('profileEmail').textContent = email;
    document.getElementById('profileRole').textContent = role.toUpperCase();

    document.getElementById('inputEmail').value = email;
    document.getElementById('inputRole').value = role.toUpperCase();
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
            <a href="notifications.html" class="menu-item"><i data-lucide="bell"></i> Notifications</a>
            <a href="emergency.html" class="menu-item text-danger" style="color: var(--danger);"><i data-lucide="alert-circle"></i> Emergency</a>
        `;
    } else if (role === 'caregiver') {
        html = `
            <a href="caregiver-dashboard.html" class="menu-item"><i data-lucide="layout-dashboard"></i> Dashboard</a>
            <a href="patients.html" class="menu-item"><i data-lucide="users"></i> Patients</a>
            <a href="notifications.html" class="menu-item"><i data-lucide="bell"></i> Notifications</a>
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
