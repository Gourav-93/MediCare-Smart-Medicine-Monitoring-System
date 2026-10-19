// patient-dashboard.js

document.addEventListener('DOMContentLoaded', async () => {
    // Ensure auth
    const user = requireAuth(['patient']);
    if (!user) return;

    await loadDashboard();
});

async function loadDashboard() {
    try {
        const data = await api.get('/dashboard/patient');
        
        // Update KPIs
        document.getElementById('kpi-total-meds').textContent = data.totalMedicines;
        document.getElementById('kpi-today-doses').textContent = data.todaySchedules ? data.todaySchedules.length : 0;
        document.getElementById('kpi-pending-doses').textContent = data.pendingMedicines ? data.pendingMedicines.length : 0;
        document.getElementById('kpi-adherence').textContent = `${data.adherencePercentage || 0}%`;

        // Navbar Notif
        if (data.unreadNotifications > 0) {
            const notifBadge = document.getElementById('navUnreadCount');
            notifBadge.textContent = data.unreadNotifications;
            notifBadge.style.display = 'block';
        }

        // Charts
        const primaryColor = getComputedStyle(document.body).getPropertyValue('--primary').trim();
        const successColor = getComputedStyle(document.body).getPropertyValue('--success').trim();
        const warningColor = getComputedStyle(document.body).getPropertyValue('--warning').trim();
        const dangerColor = getComputedStyle(document.body).getPropertyValue('--danger').trim();
        const mutedColor = getComputedStyle(document.body).getPropertyValue('--border-color').trim();

        // Adherence Chart
        const pendingCount = data.totalDoses - data.takenDoses - data.missedDoses;
        createDoughnutChart(
            'adherenceChart',
            [data.takenDoses, data.missedDoses, Math.max(pendingCount, 0)],
            ['Taken', 'Missed', 'Pending'],
            [successColor, dangerColor, warningColor]
        );

        // Status Bar Chart (simulate last 7 days based on current stats for visual)
        createBarChart(
            'statusChart',
            [data.takenDoses, data.missedDoses, Math.max(pendingCount, 0)],
            ['Taken', 'Missed', 'Pending'],
            [successColor, dangerColor, warningColor],
            'Medicine Status'
        );

        // Today's Schedule Table
        renderTodaySchedule(data.todaySchedules);

    } catch (error) {
        console.error("Failed to load dashboard", error);
    }
}

function renderTodaySchedule(schedules, pending, taken, missed) {
    const tbody = document.querySelector('#todayScheduleTable tbody');
    if (!schedules || schedules.length === 0) {
        tbody.innerHTML = '<tr><td colspan="5" style="text-align: center; color: var(--text-muted)">No medicines scheduled for today.</td></tr>';
        return;
    }

    tbody.innerHTML = schedules.map(s => {
        let status = 'Scheduled';
        let badgeClass = 'badge-info';
        
        // s.time is like "13:00:00". We check if this time exists in taken, missed, or pending arrays.
        const isTaken = taken && taken.some(t => t.medicineId === s.medicineId && t.scheduledTime.includes(s.time));
        const isMissed = missed && missed.some(m => m.medicineId === s.medicineId && m.scheduledTime.includes(s.time));
        const isPending = pending && pending.some(p => p.medicineId === s.medicineId && p.scheduledTime.includes(s.time));
        
        let pendingLogId = null;

        if (isTaken) {
            status = 'Taken';
            badgeClass = 'badge-success';
        } else if (isMissed) {
            status = 'Missed';
            badgeClass = 'badge-danger';
        } else if (isPending) {
            status = 'Pending';
            badgeClass = 'badge-warning';
            const pendingLog = pending.find(p => p.medicineId === s.medicineId && p.scheduledTime.includes(s.time));
            if (pendingLog && pendingLog.id) {
                pendingLogId = pendingLog.id;
            }
        }

        let actionBtn = '-';
        if (status === 'Pending' && pendingLogId) {
            actionBtn = `<button class="btn btn-primary" style="padding: 0.25rem 0.5rem; font-size: 0.75rem;" onclick="markAsTakenDashboard(${pendingLogId})">Mark Taken</button>`;
        } else if (status === 'Pending') {
             // It's pending but no ID yet (future dose not yet in DB)
             actionBtn = `<span style="font-size:0.8rem; color:var(--text-muted)">Upcoming</span>`;
        }

        return `
            <tr>
                <td style="font-weight: 500;">${s.medicine?.name || 'Unknown'}</td>
                <td>${formatTime(s.time)}</td>
                <td>${s.medicine?.dose || 1} ${s.medicine?.dosage || ''}</td>
                <td><span class="badge ${badgeClass}">${status}</span></td>
                <td>${actionBtn}</td>
            </tr>
        `;
    }).join('');
}

window.markAsTakenDashboard = async function(id) {
    try {
        await api.put(`/medicinelog/${id}/taken`, {});
        showToast("Medicine marked as taken.", "success");
        await loadDashboard();
    } catch (e) {
        // Handled
    }
}

function formatTime(timeStr) {
    if (!timeStr) return '';
    // timeStr could be "08:00:00"
    const parts = timeStr.split(':');
    if (parts.length >= 2) {
        const d = new Date();
        d.setHours(parseInt(parts[0], 10));
        d.setMinutes(parseInt(parts[1], 10));
        return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    }
    return timeStr;
}

async function triggerEmergency() {
    if (confirm("Are you sure you want to send an emergency alert to your caregivers?")) {
        try {
            await api.post('/emergency', { message: 'Emergency Alert Triggered by Patient' });
            showToast('Emergency alert sent successfully.', 'success');
        } catch (e) {
            // Error handled by api
        }
    }
}
