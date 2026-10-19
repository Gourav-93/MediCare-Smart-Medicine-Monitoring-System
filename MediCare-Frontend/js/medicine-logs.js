// medicine-logs.js

let userPayload;
let allLogs = [];
let medicines = [];

document.addEventListener('DOMContentLoaded', async () => {
    userPayload = requireAuth(['patient']);
    if (!userPayload) return;

    await loadData();
});

async function loadData() {
    try {
        medicines = await api.get('/medicine');
        const logs = await api.get('/medicinelog');

        // Backend already filters logs for this user/patient.
        allLogs = logs;
        
        // Sort descending by ScheduledTime
        allLogs.sort((a, b) => new Date(b.scheduledTime) - new Date(a.scheduledTime));

        renderLogs(allLogs);
    } catch (e) {
        // Handled
    }
}

function filterLogs() {
    const status = document.getElementById('filterStatus').value;
    if (status === 'All') {
        renderLogs(allLogs);
    } else {
        renderLogs(allLogs.filter(l => l.status === status));
    }
}

function renderLogs(logs) {
    const tbody = document.querySelector('#logsTable tbody');
    if (!logs || logs.length === 0) {
        tbody.innerHTML = '<tr><td colspan="5" style="text-align: center; color: var(--text-muted)">No medicine activity recorded yet.</td></tr>';
        return;
    }

    tbody.innerHTML = logs.map(l => {
        const med = medicines.find(m => m.id === l.medicineId);
        const medName = med ? med.name : 'Unknown';

        const scheduled = new Date(l.scheduledTime).toLocaleString([], { dateStyle: 'short', timeStyle: 'short' });
        const taken = l.takenTime ? new Date(l.takenTime).toLocaleString([], { dateStyle: 'short', timeStyle: 'short' }) : '-';

        let badgeClass = 'badge-info';
        if (l.status === 'Taken') badgeClass = 'badge-success';
        if (l.status === 'Missed') badgeClass = 'badge-danger';
        if (l.status === 'Pending') badgeClass = 'badge-warning';

        let actionBtn = '-';
        if (l.status === 'Pending') {
            actionBtn = `<button class="btn btn-primary" style="padding: 0.25rem 0.5rem; font-size: 0.75rem;" onclick="markAsTaken(${l.id})">Mark Taken</button>`;
        }

        return `
            <tr>
                <td style="font-weight: 500;">${medName}</td>
                <td>${scheduled}</td>
                <td>${taken}</td>
                <td><span class="badge ${badgeClass}">${l.status}</span></td>
                <td>${actionBtn}</td>
            </tr>
        `;
    }).join('');
}

async function markAsTaken(id) {
    try {
        await api.put(`/medicinelog/${id}/taken`, {});
        showToast("Medicine marked as taken.", "success");
        await loadData();
    } catch (e) {
        // Handled
    }
}
