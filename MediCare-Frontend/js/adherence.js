// Adherence Reports Controller
let selectedPatientId = null;
let selectedPeriod = 'Daily';
let userRole = null;

document.addEventListener('DOMContentLoaded', async () => {
    const user = Auth.requireAuth(['PATIENT', 'CAREGIVER', 'ADMIN']);
    if (!user) return;

    userRole = Auth.getRole();
    setupSidebar(userRole);
    setupUI();

    await initPatient();
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
            <a href="adherence.html" class="menu-item active"><i data-lucide="trending-up"></i><span>Adherence Reports</span></a>
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
            <a href="medicine-logs.html" class="menu-item"><i data-lucide="clipboard-list"></i><span>Intake Logs</span></a>
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
            <a href="medicine-logs.html" class="menu-item"><i data-lucide="clipboard-list"></i><span>Intake Logs</span></a>
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

async function initPatient() {
    try {
        const urlParams = new URLSearchParams(window.location.search);
        const queryPid = urlParams.get('patientId') || urlParams.get('id');

        if (userRole === 'PATIENT') {
            const dash = await api.get('/dashboard/patient');
            selectedPatientId = dash.patient?.id || 0;
            await loadAdherenceReports();
        } else {
            // CAREGIVER or ADMIN
            const selectorWrap = document.getElementById('patientSelectorWrap');
            const selectEl = document.getElementById('adherencePatientSelect');
            if (selectorWrap && selectEl) {
                selectorWrap.style.display = 'block';

                let patients = [];
                if (userRole === 'CAREGIVER') {
                    const dash = await api.get('/dashboard/caregiver');
                    patients = dash.patients || [];
                }

                if (patients.length > 0) {
                    selectEl.innerHTML = patients.map(p => 
                        `<option value="${p.patientId}">${Utils.escapeHtml(p.name)} (${Utils.escapeHtml(p.email)})</option>`
                    ).join('');

                    if (queryPid && patients.some(p => p.patientId == queryPid)) {
                        selectEl.value = queryPid;
                    }

                    selectedPatientId = parseInt(selectEl.value, 10);
                    await loadAdherenceReports();
                } else if (queryPid) {
                    selectedPatientId = parseInt(queryPid, 10);
                    await loadAdherenceReports();
                } else {
                    document.getElementById('adherenceBreakdownTbody').innerHTML = 
                        '<tr><td colspan="5" style="text-align: center;">No linked patients found.</td></tr>';
                }
            }
        }
    } catch (err) {
        console.error('Failed to init adherence patient:', err);
    }
}

window.onPatientSelectChange = async function() {
    const selectEl = document.getElementById('adherencePatientSelect');
    if (selectEl && selectEl.value) {
        selectedPatientId = parseInt(selectEl.value, 10);
        await loadAdherenceReports();
    }
};

async function loadAdherenceReports() {
    if (!selectedPatientId) return;

    try {
        // Set Print Dossier Direct Link
        const printBtn = document.getElementById('btnPrintDossier');
        if (printBtn) {
            printBtn.href = `${CONFIG.API_BASE}/report/patient/${selectedPatientId}/print?days=30`;
        }

        // 1. Overall Summary
        const summary = await api.get(`/adherence/patient/${selectedPatientId}`);
        renderSummaryKpis(summary);

        // 2. Periodic Timeline Report
        await loadPeriodicReport(selectedPeriod);

        if (window.lucide) {
            lucide.createIcons();
        }

    } catch (err) {
        console.error('Failed to load adherence reports:', err);
    }
}

function renderSummaryKpis(summary) {
    const rate = Math.round(Number(summary.adherencePercentage) || 0);
    document.getElementById('kpiAdherenceRate').textContent = `${rate}%`;
    document.getElementById('kpiTotalDoses').textContent = summary.totalDoses || 0;
    document.getElementById('kpiTakenDoses').textContent = summary.takenDoses || 0;
    document.getElementById('kpiMissedDoses').textContent = summary.missedDoses || 0;

    const ratingEl = document.getElementById('kpiAdherenceRating');
    if (rate >= 85) {
        ratingEl.innerHTML = '<span style="color: var(--success); font-weight: 600;">Excellent Compliance</span>';
    } else if (rate >= 65) {
        ratingEl.innerHTML = '<span style="color: var(--warning-dark); font-weight: 600;">Moderate — Needs Monitoring</span>';
    } else {
        ratingEl.innerHTML = '<span style="color: var(--danger); font-weight: 600;">Critical Non-Compliance</span>';
    }
}

async function loadPeriodicReport(period) {
    try {
        const report = await api.get(`/adherence/patient/${selectedPatientId}/report`, { period });
        const timeline = report?.timeline || [];

        const labels = timeline.map(t => t.periodLabel);
        const dataPoints = timeline.map(t => Math.round(t.adherencePercentage));

        if (window.createLineChart) {
            createLineChart(
                'adherenceReportChart',
                dataPoints,
                labels,
                '#10b981',
                'Compliance Rate %'
            );
        }

        // Render breakdown table
        renderBreakdownTable(timeline);

    } catch (err) {
        console.error('Failed to load periodic report:', err);
    }
}

function renderBreakdownTable(timeline = []) {
    const tbody = document.getElementById('adherenceBreakdownTbody');
    if (!timeline || timeline.length === 0) {
        tbody.innerHTML = '<tr><td colspan="5" style="text-align: center; color: var(--text-muted);">No timeline data recorded for this period.</td></tr>';
        return;
    }

    tbody.innerHTML = timeline.map(t => {
        const pct = Math.round(t.adherencePercentage);
        return `
            <tr>
                <td style="font-weight: 600;">${Utils.escapeHtml(t.periodLabel)}</td>
                <td>${t.totalDoses}</td>
                <td style="color: var(--success); font-weight: 600;">${t.takenDoses}</td>
                <td style="color: var(--danger); font-weight: 600;">${t.missedDoses}</td>
                <td>${Utils.getAdherenceBadge(pct)}</td>
            </tr>
        `;
    }).join('');
}

window.setAdherencePeriod = async function(period) {
    selectedPeriod = period;
    document.querySelectorAll('.active-period').forEach(b => b.classList.remove('active-period'));
    const btn = document.getElementById(`btnPeriod${period}`);
    if (btn) btn.classList.add('active-period');

    await loadPeriodicReport(period);
};
