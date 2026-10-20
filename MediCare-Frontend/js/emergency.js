// Emergency Alerts Controller
let allEmergencies = [];
let userRole = null;

document.addEventListener('DOMContentLoaded', async () => {
    const user = Auth.requireAuth(['PATIENT', 'CAREGIVER', 'ADMIN']);
    if (!user) return;

    userRole = Auth.getRole();
    setupSidebar(userRole);
    setupUI();

    await loadEmergencies();

    const form = document.getElementById('sosForm');
    if (form) form.addEventListener('submit', handleSosAlertSubmit);
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
            <a href="notifications.html" class="menu-item"><i data-lucide="bell"></i><span>Notifications</span></a>
            <a href="emergency.html" class="menu-item active text-danger" style="color: #f87171;"><i data-lucide="alert-octagon"></i><span>Emergency Alerts</span></a>
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
            <a href="emergency.html" class="menu-item active text-danger" style="color: #f87171;"><i data-lucide="alert-octagon"></i><span>Emergency Alerts</span></a>
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
            <a href="medicine-logs.html" class="menu-item"><i data-lucide="clipboard-list"></i><span>Intake Logs</span></a>
            <div class="menu-category">System Health & Alerts</div>
            <a href="emergency.html" class="menu-item active text-danger" style="color: #f87171;"><i data-lucide="alert-octagon"></i><span>Emergency Alerts</span></a>
            <a href="notifications.html" class="menu-item"><i data-lucide="bell"></i><span>Broadcast Notifications</span></a>
            <a href="profile.html" class="menu-item"><i data-lucide="user"></i><span>My Profile</span></a>
        `;
    }
    if (window.lucide) lucide.createIcons();
}

function setupUI() {
    if (userRole === 'PATIENT') {
        const btn = document.getElementById('btnTriggerSos');
        if (btn) btn.style.display = 'inline-flex';
    }

    const toggleBtn = document.getElementById('sidebarToggle');
    const sidebar = document.getElementById('sidebar');
    if (toggleBtn && sidebar) {
        toggleBtn.addEventListener('click', () => {
            sidebar.classList.toggle('open');
        });
    }
}

async function loadEmergencies() {
    try {
        const data = await api.get('/emergency');
        allEmergencies = Array.isArray(data) ? data : [];
        filterEmergencies();
    } catch (err) {
        console.error('Failed to load emergencies:', err);
    }
}

function renderEmergenciesTable(emergencies = []) {
    const tbody = document.getElementById('emergencyTbody');
    if (!emergencies || emergencies.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="6" style="text-align: center; padding: 2.5rem 1rem; color: var(--text-muted);">
                    <i data-lucide="shield-check" style="width: 36px; height: 36px; margin: 0 auto 0.5rem; display: block; color: var(--success); opacity: 0.8;"></i>
                    No emergency incidents found matching filter.
                </td>
            </tr>
        `;
        if (window.lucide) lucide.createIcons({ root: tbody });
        return;
    }

    tbody.innerHTML = emergencies.map(e => {
        const isActive = (e.status === 'ACTIVE');
        const statusBadge = isActive 
            ? '<span class="badge badge-danger">ACTIVE</span>' 
            : '<span class="badge badge-success">RESOLVED</span>';

        const rowBg = isActive ? 'background: rgba(239, 68, 68, 0.04);' : '';
        const patientName = e.patient?.user?.name || (userRole === 'PATIENT' ? 'Self (You)' : 'Patient');
        const created = Utils.formatDateTime(e.createdAt);
        const resolved = e.resolvedAt ? Utils.formatDateTime(e.resolvedAt) : '—';

        let actionBtn = '<span style="color: var(--text-muted); font-size: 0.8rem;">—</span>';
        if (isActive && userRole !== 'PATIENT') {
            actionBtn = `
                <button class="btn btn-sm btn-outline" style="border-color: var(--danger); color: var(--danger);" onclick="resolveEmergencyAlert(${e.id})">
                    <i data-lucide="check" style="width: 12px; height: 12px;"></i>
                    Resolve Alert
                </button>
            `;
        }

        return `
            <tr style="${rowBg}">
                <td style="font-weight: 600;">
                    <div style="color: var(--text-main);">${Utils.escapeHtml(patientName)}</div>
                    ${e.patient?.emergencyContact ? `<div style="font-size: 0.75rem; color: var(--text-muted);">Contact: ${Utils.escapeHtml(e.patient.emergencyContact)}</div>` : ''}
                </td>
                <td style="font-weight: 500;">${Utils.escapeHtml(e.message)}</td>
                <td>${statusBadge}</td>
                <td>${created}</td>
                <td>${resolved}</td>
                <td>${actionBtn}</td>
            </tr>
        `;
    }).join('');

    if (window.lucide) lucide.createIcons({ root: tbody });
}

window.filterEmergencies = function() {
    const status = document.getElementById('emergencyStatusFilter')?.value || 'All';
    const query = (document.getElementById('emergencySearchInput')?.value || '').toLowerCase().trim();

    let filtered = allEmergencies;

    if (status !== 'All') {
        filtered = filtered.filter(e => String(e.status).toUpperCase() === status);
    }

    if (query) {
        filtered = filtered.filter(e => {
            const pName = (e.patient?.user?.name || '').toLowerCase();
            const msg = (e.message || '').toLowerCase();
            return pName.includes(query) || msg.includes(query);
        });
    }

    renderEmergenciesTable(filtered);
};

window.openSosModal = function() {
    Utils.openModal('patientSosModal');
};

async function handleSosAlertSubmit(e) {
    e.preventDefault();
    const msg = document.getElementById('sosMessageText').value.trim();
    const submitBtn = document.getElementById('btnSubmitSosAlert');

    if (!msg) {
        Utils.showToast('Please enter emergency details.', 'warning');
        return;
    }

    try {
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<span class="spinner" style="width: 16px; height: 16px; border-width: 2px;"></span> Broadcasting SOS...';

        const res = await api.post('/emergency', { message: msg });
        Utils.closeModal('patientSosModal');

        const count = res.caregiversNotified || 0;
        Utils.showToast(`🚨 Emergency Alert broadcasted! ${count} caregiver(s) notified.`, 'success', 6000);

        await loadEmergencies();

    } catch (err) {
        console.error('Failed to trigger emergency alert:', err);
    } finally {
        submitBtn.disabled = false;
        submitBtn.innerHTML = '<i data-lucide="send"></i> Confirm & Send SOS';
        if (window.lucide) lucide.createIcons({ root: submitBtn });
    }
}

window.resolveEmergencyAlert = async function(id) {
    const confirmed = await Utils.confirmAction(
        'Are you sure you want to resolve this emergency incident? This records that caregiver or clinical assistance has been provided.',
        'Resolve Emergency'
    );
    if (!confirmed) return;

    try {
        await api.put(`/emergency/${id}/resolve`);
        Utils.showToast('Emergency incident marked as resolved.', 'success');
        await loadEmergencies();
    } catch (err) {
        console.error('Failed to resolve emergency alert:', err);
    }
};
