// caregiver-dashboard.js

document.addEventListener('DOMContentLoaded', async () => {
    const user = requireAuth(['caregiver']);
    if (!user) return;

    await loadDashboard();
});

async function loadDashboard() {
    try {
        const data = await api.get('/dashboard/caregiver');
        
        // Update KPIs
        document.getElementById('kpi-patients').textContent = data.patients ? data.patients.length : 0;
        document.getElementById('kpi-emergencies').textContent = data.activeEmergencies ? data.activeEmergencies.length : 0;
        document.getElementById('kpi-pending').textContent = data.pendingMedicines ? data.pendingMedicines.length : 0;
        document.getElementById('kpi-missed').textContent = data.missedMedicines ? data.missedMedicines.length : 0;

        // Navbar Notif
        if (data.unreadNotifications > 0) {
            const notifBadge = document.getElementById('navUnreadCount');
            notifBadge.textContent = data.unreadNotifications;
            notifBadge.style.display = 'block';
        }

        renderEmergencyBanner(data.activeEmergencies);
        renderPatientOverview(data.adherence);
        renderMissedTable(data.missedMedicines, data.patients);
        renderPendingTable(data.pendingMedicines, data.patients);

    } catch (error) {
        console.error("Failed to load dashboard", error);
    }
}

function renderEmergencyBanner(emergencies) {
    const container = document.getElementById('emergencyBannerContainer');
    if (!emergencies || emergencies.length === 0) {
        container.innerHTML = '';
        return;
    }

    container.innerHTML = emergencies.map(e => `
        <div class="card animate-fade-in emergency-pulse" style="background: rgba(239, 68, 68, 0.1); border-color: var(--danger); margin-bottom: 1.5rem; display: flex; justify-content: space-between; align-items: center;">
            <div>
                <h3 style="color: var(--danger); margin-bottom: 0.5rem; display: flex; align-items: center; gap: 0.5rem;">
                    <i data-lucide="alert-triangle"></i> EMERGENCY ALERT
                </h3>
                <p style="color: var(--text-main);"><strong>${e.patient?.user?.name || 'Unknown Patient'}</strong> requires immediate assistance.</p>
                <p style="font-size: 0.875rem; color: var(--text-muted);">Triggered at: ${new Date(e.createdAt).toLocaleString()}</p>
            </div>
            <a href="emergency.html" class="btn btn-danger">View Alerts</a>
        </div>
    `).join('');
    
    lucide.createIcons({ root: container });
}

function renderPatientOverview(adherenceList) {
    const grid = document.getElementById('patientsOverviewGrid');
    if (!adherenceList || adherenceList.length === 0) {
        grid.innerHTML = '<div style="color: var(--text-muted); grid-column: 1 / -1;">No patients linked yet.</div>';
        return;
    }

    grid.innerHTML = adherenceList.map(a => {
        let colorClass = 'success';
        if (a.adherencePercentage < 80) colorClass = 'warning';
        if (a.adherencePercentage < 50) colorClass = 'danger';

        return `
            <div class="card animate-fade-in" style="cursor: pointer;" onclick="window.location.href='patients.html?id=${a.patientId}'">
                <div style="display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 1rem;">
                    <div>
                        <h3 style="font-size: 1.1rem;">${a.patientName}</h3>
                        <span style="font-size: 0.875rem; color: var(--text-muted);">Patient ID: ${a.patientId}</span>
                    </div>
                    <div class="avatar" style="background: var(--${colorClass});">${Math.round(a.adherencePercentage)}%</div>
                </div>
                <div style="display: flex; justify-content: space-between; font-size: 0.875rem;">
                    <span style="color: var(--success);"><i data-lucide="check" style="width: 14px;"></i> Taken: ${a.takenDoses}</span>
                    <span style="color: var(--danger);"><i data-lucide="x" style="width: 14px;"></i> Missed: ${a.missedDoses}</span>
                </div>
            </div>
        `;
    }).join('');
    
    lucide.createIcons({ root: grid });
}

function formatTime(timeStr) {
    if (!timeStr) return '';
    return new Date(timeStr).toLocaleString([], { dateStyle: 'short', timeStyle: 'short' });
}

function renderMissedTable(logs, patientsList = []) {
    const tbody = document.querySelector('#missedTable tbody');
    if (!logs || logs.length === 0) {
        tbody.innerHTML = '<tr><td colspan="3" style="text-align: center; color: var(--text-muted)">No missed medicines.</td></tr>';
        return;
    }

    tbody.innerHTML = logs.map(l => {
        let pName = l.patient?.user?.name;
        if (!pName && l.patientId) {
            const p = patientsList.find(x => x.patientId === l.patientId);
            if (p) pName = p.name;
        }
        return `
        <tr>
            <td>${pName || 'Unknown'}</td>
            <td style="font-weight: 500;">${l.medicine?.name || 'Unknown'}</td>
            <td>${formatTime(l.scheduledTime)}</td>
        </tr>
    `}).join('');
}

function renderPendingTable(logs, patientsList = []) {
    const tbody = document.querySelector('#pendingTable tbody');
    if (!logs || logs.length === 0) {
        tbody.innerHTML = '<tr><td colspan="3" style="text-align: center; color: var(--text-muted)">No pending medicines.</td></tr>';
        return;
    }

    tbody.innerHTML = logs.map(l => {
        let pName = l.patient?.user?.name;
        if (!pName && l.patientId) {
            const p = patientsList.find(x => x.patientId === l.patientId);
            if (p) pName = p.name;
        }
        return `
        <tr>
            <td>${pName || 'Unknown'}</td>
            <td style="font-weight: 500;">${l.medicine?.name || 'Unknown'}</td>
            <td>${formatTime(l.scheduledTime)}</td>
        </tr>
    `}).join('');
}
