// Medicine Logs & History Controller
let allLogs = [];
let medicinesList = [];
let userRole = null;

document.addEventListener('DOMContentLoaded', async () => {
    const user = Auth.requireAuth(['PATIENT', 'CAREGIVER', 'ADMIN']);
    if (!user) return;

    userRole = Auth.getRole();
    Auth.setupNavbar();
    Auth.renderSidebar('logs');
    setupUI();

    await loadLogsData();
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
            <a href="medicine-logs.html" class="menu-item active"><i data-lucide="clipboard-list"></i><span>Intake History</span></a>
            <a href="adherence.html" class="menu-item"><i data-lucide="trending-up"></i><span>Adherence Reports</span></a>
            <div class="menu-category">Communication & Care</div>
            <a href="notifications.html" class="menu-item"><i data-lucide="bell"></i><span>Notifications</span></a>
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
            <a href="medicine-logs.html" class="menu-item active"><i data-lucide="clipboard-list"></i><span>Intake Logs</span></a>
            <div class="menu-category">Safety & Alerts</div>
            <a href="emergency.html" class="menu-item text-danger" style="color: #f87171;"><i data-lucide="alert-octagon"></i><span>Emergency Alerts</span></a>
            <a href="notifications.html" class="menu-item"><i data-lucide="bell"></i><span>Notifications</span></a>
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
            <a href="medicine-logs.html" class="menu-item active"><i data-lucide="clipboard-list"></i><span>Intake Logs</span></a>
            <div class="menu-category">System Health & Alerts</div>
            <a href="emergency.html" class="menu-item text-danger" style="color: #f87171;"><i data-lucide="alert-octagon"></i><span>Emergency Alerts</span></a>
            <a href="notifications.html" class="menu-item"><i data-lucide="bell"></i><span>Broadcast Notifications</span></a>
            <a href="profile.html" class="menu-item"><i data-lucide="user"></i><span>My Profile</span></a>
        `;
    }
    if (window.lucide) lucide.createIcons();
}

function setupUI() {
    const toggleBtn = document.getElementById('sidebarToggle');
    const sidebar = document.getElementById('sidebar');
    if (toggleBtn && sidebar) {
        toggleBtn.addEventListener('click', () => {
            sidebar.classList.toggle('open');
        });
    }
}

async function loadLogsData() {
    try {
        const [logs, meds] = await Promise.all([
            api.get('/medicinelog'),
            api.get('/medicine')
        ]);

        allLogs = Array.isArray(logs) ? logs : [];
        medicinesList = Array.isArray(meds) ? meds : [];

        // Sort chronologically descending
        allLogs.sort((a, b) => new Date(b.scheduledTime) - new Date(a.scheduledTime));

        renderLogsTable(allLogs);

    } catch (err) {
        console.error('Failed to load medication logs:', err);
    }
}

function renderLogsTable(logs = []) {
    const tbody = document.getElementById('logsTbody');
    if (!logs || logs.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="6" style="text-align: center; padding: 2.5rem 1rem; color: var(--text-muted);">
                    <i data-lucide="clipboard-list" style="width: 36px; height: 36px; margin: 0 auto 0.5rem; display: block; opacity: 0.5;"></i>
                    No intake records match your filters.
                </td>
            </tr>
        `;
        if (window.lucide) lucide.createIcons({ root: tbody });
        return;
    }

    tbody.innerHTML = logs.map(l => {
        const med = l.medicine || medicinesList.find(m => m.id === l.medicineId);
        const medName = med?.name || 'Prescription';
        const dosage = med?.dosage || '';
        const patientName = l.patient?.user?.name || (userRole === 'PATIENT' ? 'Self' : 'Patient');

        const scheduled = Utils.formatDateTime(l.scheduledTime);
        const taken = l.takenTime ? Utils.formatDateTime(l.takenTime) : '-';
        const statusBadge = Utils.getStatusBadge(l.status);

        let actionBtn = '<span style="color: var(--text-muted); font-size: 0.8rem;">—</span>';
        if (userRole === 'PATIENT' && String(l.status).toLowerCase() === 'pending') {
            actionBtn = `
                <button class="btn btn-sm btn-primary" onclick="markLogAsTaken(${l.id})">
                    <i data-lucide="check" style="width: 12px; height: 12px;"></i>
                    Mark Taken
                </button>
            `;
        }

        return `
            <tr>
                <td style="font-weight: 600;">
                    <div style="color: var(--text-main);">${Utils.escapeHtml(medName)}</div>
                    <div style="font-size: 0.75rem; color: var(--text-muted);">${Utils.escapeHtml(dosage)}</div>
                </td>
                <td><span style="font-size: 0.85rem; color: var(--text-muted);">${Utils.escapeHtml(patientName)}</span></td>
                <td>${scheduled}</td>
                <td>${taken}</td>
                <td>${statusBadge}</td>
                <td>${actionBtn}</td>
            </tr>
        `;
    }).join('');

    if (window.lucide) lucide.createIcons({ root: tbody });
}

window.filterLogs = function() {
    const status = document.getElementById('filterStatus')?.value || 'All';
    const query = (document.getElementById('logSearchInput')?.value || '').toLowerCase().trim();

    let filtered = allLogs;

    if (status !== 'All') {
        filtered = filtered.filter(l => String(l.status).toLowerCase() === status.toLowerCase());
    }

    if (query) {
        filtered = filtered.filter(l => {
            const med = l.medicine || medicinesList.find(m => m.id === l.medicineId);
            const medName = (med?.name || '').toLowerCase();
            const patName = (l.patient?.user?.name || '').toLowerCase();
            return medName.includes(query) || patName.includes(query);
        });
    }

    renderLogsTable(filtered);
};

window.markLogAsTaken = async function(id) {
    if (!id) return;
    const btn = event?.target?.closest('button');
    if (btn) {
        btn.disabled = true;
        btn.innerHTML = 'Updating...';
    }
    try {
        await api.put(`/medicinelog/${id}/taken`);
        Utils.showToast('Medication recorded as taken successfully!', 'success');
        await loadLogsData();
    } catch (err) {
        console.error('Failed to mark log as taken:', err);
        if (btn) {
            btn.disabled = false;
            btn.innerHTML = 'Mark Taken';
        }
    }
};
