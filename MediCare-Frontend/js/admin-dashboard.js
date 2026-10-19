// admin-dashboard.js

document.addEventListener('DOMContentLoaded', async () => {
    const user = requireAuth(['admin']);
    if (!user) return;

    await loadDashboard();
});

async function loadDashboard() {
    try {
        const data = await api.get('/dashboard/admin');

        document.getElementById('kpi-users').textContent = data.totalUsers;
        document.getElementById('kpi-patients').textContent = data.totalPatients;
        document.getElementById('kpi-caregivers').textContent = data.totalCaregivers;
        document.getElementById('kpi-adherence').textContent = `${data.overallAdherencePercentage || 0}%`;

        document.getElementById('activeEmergencyCount').textContent = data.activeEmergencyAlerts;

        // Colors
        const primary = getComputedStyle(document.body).getPropertyValue('--primary').trim();
        const info = getComputedStyle(document.body).getPropertyValue('--info').trim();
        const success = getComputedStyle(document.body).getPropertyValue('--success').trim();
        const warning = getComputedStyle(document.body).getPropertyValue('--warning').trim();
        const danger = getComputedStyle(document.body).getPropertyValue('--danger').trim();

        // User Chart
        createDoughnutChart(
            'userDistChart',
            [data.totalPatients, data.totalCaregivers, data.totalUsers - data.totalPatients - data.totalCaregivers],
            ['Patients', 'Caregivers', 'Admins'],
            [primary, info, '#64748b']
        );

        // Logs Chart
        createBarChart(
            'logsChart',
            [data.totalTakenDoses, data.totalMissedDoses, data.totalPendingDoses],
            ['Taken', 'Missed', 'Pending'],
            [success, danger, warning],
            'Doses'
        );

        renderActiveEmergencies(data.activeEmergencies);
        renderLowStock(data.lowStockMedicines);

    } catch (error) {
        console.error(error);
    }
}

function renderActiveEmergencies(emergencies) {
    const tbody = document.querySelector('#adminEmergencyTable tbody');
    if (!emergencies || emergencies.length === 0) {
        tbody.innerHTML = '<tr><td colspan="3" style="text-align: center; color: var(--text-muted)">No active emergencies.</td></tr>';
        return;
    }

    tbody.innerHTML = emergencies.map(e => `
        <tr style="background: rgba(239, 68, 68, 0.05);">
            <td style="font-weight: 500;">${e.patient?.user?.name || 'Unknown'}</td>
            <td>${e.message}</td>
            <td>${new Date(e.createdAt).toLocaleString()}</td>
        </tr>
    `).join('');
}

function renderLowStock(medicines) {
    const tbody = document.querySelector('#adminLowStockTable tbody');
    if (!medicines || medicines.length === 0) {
        tbody.innerHTML = '<tr><td colspan="3" style="text-align: center; color: var(--text-muted)">No low stock medicines.</td></tr>';
        return;
    }

    tbody.innerHTML = medicines.map(m => `
        <tr>
            <td style="font-weight: 500;">${m.name}</td>
            <td>${m.patient?.user?.name || 'Unknown'}</td>
            <td><span class="badge ${m.stock === 0 ? 'badge-danger' : 'badge-warning'}">${m.stock}</span></td>
        </tr>
    `).join('');
}
