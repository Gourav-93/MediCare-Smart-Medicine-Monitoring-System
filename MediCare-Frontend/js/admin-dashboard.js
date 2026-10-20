// Admin Enterprise Dashboard Controller
document.addEventListener('DOMContentLoaded', async () => {
    const user = Auth.requireAuth(['ADMIN']);
    if (!user) return;

    setupUI();
    await loadAdminDashboard();
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

async function loadAdminDashboard() {
    try {
        const data = await api.get('/dashboard/admin');

        // Populate KPIs
        document.getElementById('kpiUsers').textContent = data.totalUsers || 0;
        document.getElementById('kpiUserBreakdown').textContent = `${data.totalPatients || 0} Patients • ${data.totalCaregivers || 0} Caregivers`;
        document.getElementById('kpiOverallAdherence').textContent = `${Math.round(data.overallAdherencePercentage || 0)}%`;
        document.getElementById('kpiMedicines').textContent = data.totalMedicines || 0;
        document.getElementById('kpiActiveEmergencies').textContent = data.activeEmergencyAlerts || 0;
        document.getElementById('kpiTotalEmergencies').textContent = `${data.totalEmergencyAlerts || 0} total lifetime`;

        document.getElementById('kpiTakenDoses').textContent = data.totalTakenDoses || 0;
        document.getElementById('kpiMissedDoses').textContent = data.totalMissedDoses || 0;
        document.getElementById('kpiPendingDoses').textContent = data.totalPendingDoses || 0;
        document.getElementById('kpiCaregiverLinks').textContent = data.caregiverPatientRelationships || 0;

        // Active Emergency CTA
        const activeEmCount = data.activeEmergencyAlerts || 0;
        const alertBtn = document.getElementById('btnActiveEmergenciesAlert');
        const emBadge = document.getElementById('adminEmergencyBadge');
        if (activeEmCount > 0) {
            if (alertBtn) {
                document.getElementById('activeEmergenciesBtnCount').textContent = activeEmCount;
                alertBtn.style.display = 'inline-flex';
            }
            if (emBadge) {
                emBadge.textContent = activeEmCount;
                emBadge.style.display = 'inline-block';
            }
        }

        // Unread Notifications Badge
        if (data.unreadNotifications > 0) {
            const navBadge = document.getElementById('navUnreadBadge');
            const sideBadge = document.getElementById('adminNotifBadge');
            if (navBadge) {
                navBadge.textContent = data.unreadNotifications;
                navBadge.style.display = 'flex';
            }
            if (sideBadge) {
                sideBadge.textContent = data.unreadNotifications;
                sideBadge.style.display = 'inline-block';
            }
        }

        // Render Charts
        renderAdminCharts(data);

        // Render Active Emergencies Table
        renderActiveEmergencies(data.activeEmergencies || []);

        // Render Low Stock Medicines Table
        renderLowStock(data.lowStockMedicines || []);

        if (window.lucide) {
            lucide.createIcons();
        }

    } catch (err) {
        console.error('Failed to load admin dashboard:', err);
    }
}

function renderAdminCharts(data) {
    if (!window.createDoughnutChart || !window.createBarChart) return;

    // 1. User Distribution Doughnut
    const adminsCount = Math.max(0, (data.totalUsers || 0) - (data.totalPatients || 0) - (data.totalCaregivers || 0));
    createDoughnutChart(
        'userDistChart',
        [data.totalPatients || 0, data.totalCaregivers || 0, adminsCount],
        ['Patients', 'Caregivers', 'Administrators'],
        ['#1d4ed8', '#0284c7', '#7c3aed']
    );

    // 2. Intake Logs Breakdown Bar
    createBarChart(
        'logsChart',
        [data.totalTakenDoses || 0, data.totalMissedDoses || 0, data.totalPendingDoses || 0],
        ['Taken', 'Missed', 'Pending'],
        ['#10b981', '#ef4444', '#f59e0b'],
        'Doses'
    );
}

function renderActiveEmergencies(emergencies = []) {
    const tbody = document.getElementById('adminEmergencyTbody');
    if (!emergencies || emergencies.length === 0) {
        tbody.innerHTML = '<tr><td colspan="4" style="text-align: center; color: var(--text-muted);">No active emergency alerts. All clear!</td></tr>';
        return;
    }

    tbody.innerHTML = emergencies.map(e => `
        <tr style="background: rgba(239, 68, 68, 0.04);">
            <td style="font-weight: 600;">${Utils.escapeHtml(e.patient?.user?.name || 'Patient')}</td>
            <td>${Utils.escapeHtml(e.message)}</td>
            <td><span class="badge badge-danger">${Utils.formatDateTime(e.createdAt)}</span></td>
            <td>
                <button class="btn btn-sm btn-outline" style="border-color: var(--danger); color: var(--danger);" onclick="resolveEmergency(${e.id})">
                    <i data-lucide="check" style="width: 12px; height: 12px;"></i>
                    Resolve
                </button>
            </td>
        </tr>
    `).join('');

    if (window.lucide) lucide.createIcons({ root: tbody });
}

function renderLowStock(medicines = []) {
    const tbody = document.getElementById('adminLowStockTbody');
    if (!medicines || medicines.length === 0) {
        tbody.innerHTML = '<tr><td colspan="4" style="text-align: center; color: var(--text-muted);">No low-stock medicines recorded.</td></tr>';
        return;
    }

    tbody.innerHTML = medicines.map(m => `
        <tr>
            <td style="font-weight: 600;">${Utils.escapeHtml(m.name)}</td>
            <td>${Utils.escapeHtml(m.patient?.user?.name || 'Patient')}</td>
            <td>${Utils.getStockBadge(m.stock)}</td>
            <td>${Utils.escapeHtml(m.dosage)}</td>
        </tr>
    `).join('');
}

window.resolveEmergency = async function(emergencyId) {
    const confirmed = await Utils.confirmAction('Resolve this emergency alert? This indicates medical staff or caregivers have addressed the incident.', 'Resolve Alert');
    if (!confirmed) return;

    try {
        await api.put(`/emergency/${emergencyId}/resolve`);
        Utils.showToast('Emergency alert resolved.', 'success');
        await loadAdminDashboard();
    } catch (err) {
        console.error('Failed to resolve emergency:', err);
    }
};
