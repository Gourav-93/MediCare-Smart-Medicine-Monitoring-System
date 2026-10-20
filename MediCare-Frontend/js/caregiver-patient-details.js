// Caregiver / Admin Patient Inspection Controller
let currentPatientId = null;
let currentPeriod = 'Daily';
let allPatientLogs = [];

document.addEventListener('DOMContentLoaded', async () => {
    const user = Auth.requireAuth(['CAREGIVER', 'ADMIN']);
    if (!user) return;

    setupUI();

    // Extract patientId from query string (?patientId=1 or ?id=1)
    const urlParams = new URLSearchParams(window.location.search);
    const id = urlParams.get('patientId') || urlParams.get('id');

    if (!id || isNaN(parseInt(id, 10))) {
        Utils.showToast('No valid Patient ID specified.', 'warning');
        setTimeout(() => {
            window.location.href = 'patients.html';
        }, 1200);
        return;
    }

    currentPatientId = parseInt(id, 10);
    await loadPatientDossier();
});

function setupUI() {
    const role = Auth.getRole();
    const roleLabel = document.getElementById('sidebarRoleLabel');
    const dashLink = document.getElementById('sidebarDashboardLink');

    if (role === 'ADMIN') {
        if (roleLabel) roleLabel.textContent = 'Admin';
        if (dashLink) dashLink.href = 'admin-dashboard.html';
    } else {
        if (roleLabel) roleLabel.textContent = 'Caregiver';
        if (dashLink) dashLink.href = 'caregiver-dashboard.html';
    }

    const toggleBtn = document.getElementById('sidebarToggle');
    const sidebar = document.getElementById('sidebar');
    if (toggleBtn && sidebar) {
        toggleBtn.addEventListener('click', () => {
            sidebar.classList.toggle('open');
        });
    }
}

async function loadPatientDossier() {
    try {
        // 1. Fetch Clinical Medical Report Data
        const report = await api.get(`/report/patient/${currentPatientId}/data?days=30`);
        renderPatientDemographics(report);

        // Set Print Document Direct URL
        const printBtn = document.getElementById('btnDownloadReport');
        if (printBtn) {
            printBtn.href = `${CONFIG.API_BASE}/report/patient/${currentPatientId}/print?days=30`;
        }

        // 2. Fetch Adherence Periodic Timeline
        await loadPeriodicTimeline(currentPeriod);

        // 3. Fetch Intake History
        allPatientLogs = await api.get(`/medicinelog/patient/${currentPatientId}/history`);
        renderHistoryLogs(allPatientLogs);

        if (window.lucide) {
            lucide.createIcons();
        }

    } catch (err) {
        console.error('Failed to load patient dossier:', err);
    }
}

function renderPatientDemographics(report) {
    document.getElementById('patientFullName').textContent = report.patientName || 'Patient';
    document.getElementById('patientEmailText').textContent = report.patientEmail || 'No email provided';
    document.getElementById('patientDobText').textContent = Utils.formatDate(report.dateOfBirth);
    document.getElementById('patientEmergencyText').textContent = report.emergencyContact || 'None';
    document.getElementById('patientAvatarLarge').textContent = Utils.getInitials(report.patientName || 'P');

    // Metrics
    const adherence = Math.round(Number(report.overallAdherence) || 0);
    document.getElementById('kpiPatientAdherence').textContent = `${adherence}%`;
    document.getElementById('kpiPatientTotalDoses').textContent = report.totalDosesRecorded || 0;
    document.getElementById('kpiPatientTakenDoses').textContent = report.totalTaken || 0;
    document.getElementById('kpiPatientMissedDoses').textContent = report.totalMissed || 0;

    // Prescriptions Table
    renderPrescriptions(report.activeMedicines || []);
}

function renderPrescriptions(medicines = []) {
    const tbody = document.getElementById('patientMedicinesTbody');
    if (!medicines || medicines.length === 0) {
        tbody.innerHTML = '<tr><td colspan="5" style="text-align: center; color: var(--text-muted);">No active prescriptions recorded for this patient.</td></tr>';
        return;
    }

    tbody.innerHTML = medicines.map(m => {
        const scheduledTimes = (m.scheduledTimes && m.scheduledTimes.length > 0)
            ? m.scheduledTimes.map(t => `<span class="badge badge-neutral">${Utils.formatTime(t)}</span>`).join(' ')
            : '<span style="color: var(--text-muted); font-size: 0.8rem;">No times scheduled</span>';

        return `
            <tr>
                <td style="font-weight: 600;">${Utils.escapeHtml(m.name)}</td>
                <td>${Utils.escapeHtml(m.dosage)}</td>
                <td>${m.dose} unit(s)</td>
                <td>${Utils.getStockBadge(m.stock)}</td>
                <td>${scheduledTimes}</td>
            </tr>
        `;
    }).join('');
}

async function loadPeriodicTimeline(period) {
    try {
        const timelineReport = await api.get(`/adherence/patient/${currentPatientId}/report`, { period });
        const timeline = timelineReport?.timeline || [];

        const labels = timeline.map(t => t.periodLabel);
        const dataPoints = timeline.map(t => Math.round(t.adherencePercentage));

        if (window.createLineChart) {
            createLineChart(
                'patientAdherenceTimelineChart',
                dataPoints,
                labels,
                '#1d4ed8',
                'Adherence %'
            );
        }

    } catch (err) {
        console.warn('Periodic timeline fetch failed:', err);
    }
}

window.changePeriod = async function(period) {
    currentPeriod = period;
    document.querySelectorAll('.active-period').forEach(b => b.classList.remove('active-period'));
    const btn = document.getElementById(`btnPeriod${period}`);
    if (btn) btn.classList.add('active-period');

    await loadPeriodicTimeline(period);
};

function renderHistoryLogs(logs = []) {
    const tbody = document.getElementById('patientLogsTbody');
    if (!logs || logs.length === 0) {
        tbody.innerHTML = '<tr><td colspan="4" style="text-align: center; color: var(--text-muted);">No intake records recorded yet.</td></tr>';
        return;
    }

    tbody.innerHTML = logs.map(l => `
        <tr>
            <td style="font-weight: 600;">${Utils.escapeHtml(l.medicine?.name || 'Medicine')}</td>
            <td>${Utils.formatDateTime(l.scheduledTime)}</td>
            <td>${Utils.getStatusBadge(l.status)}</td>
            <td>${l.takenTime ? Utils.formatDateTime(l.takenTime) : '-'}</td>
        </tr>
    `).join('');
}

window.applyLogFilter = function() {
    const filter = document.getElementById('logStatusFilter').value;
    if (!filter) {
        renderHistoryLogs(allPatientLogs);
    } else {
        const filtered = allPatientLogs.filter(l => String(l.status).toLowerCase() === filter.toLowerCase());
        renderHistoryLogs(filtered);
    }
};
