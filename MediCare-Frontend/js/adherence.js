// Adherence Reports Controller
let selectedPatientId = null;
let selectedPeriod = 'Daily';
let userRole = null;

document.addEventListener('DOMContentLoaded', async () => {
    const user = Auth.requireAuth(['PATIENT', 'CAREGIVER', 'ADMIN']);
    if (!user) return;

    Auth.setupNavbar();
    Auth.renderSidebar('adherence');

    userRole = Auth.getRole();
    setupUI();

    await initPatient();
});

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
                } else if (userRole === 'ADMIN') {
                    const meds = await api.get('/medicine');
                    const patientMap = new Map();
                    (meds || []).forEach(m => {
                        if (m.patient && !patientMap.has(m.patientId)) {
                            patientMap.set(m.patientId, {
                                patientId: m.patientId,
                                name: m.patient.user ? m.patient.user.name : `Patient #${m.patientId}`,
                                email: m.patient.user ? m.patient.user.email : ''
                            });
                        }
                    });
                    patients = Array.from(patientMap.values());
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
