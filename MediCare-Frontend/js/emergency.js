// emergency.js

let userPayload;

document.addEventListener('DOMContentLoaded', async () => {
    // Both Patient and Caregiver can access
    userPayload = getJwtPayload();
    if (!userPayload) {
        window.location.href = 'login.html';
        return;
    }

    const role = (userPayload.role || userPayload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']).toLowerCase();

    setupSidebar(role);

    if (role === 'patient') {
        document.querySelectorAll('.hide-patient').forEach(el => el.style.display = 'none');
        document.getElementById('nav-initial').textContent = 'P';
    } else if (role === 'caregiver') {
        document.querySelectorAll('.hide-caregiver').forEach(el => el.style.display = 'none');
        document.getElementById('nav-initial').textContent = 'C';
    } else if (role === 'admin') {
        document.querySelectorAll('.hide-caregiver').forEach(el => el.style.display = 'none');
        document.getElementById('nav-initial').textContent = 'A';
    }

    await loadEmergencies(role);
});

function setupSidebar(role) {
    const menu = document.getElementById('sidebarMenu');
    let html = '';
    
    if (role === 'patient') {
        html = `
            <a href="patient-dashboard.html" class="menu-item"><i data-lucide="layout-dashboard"></i> Dashboard</a>
            <a href="medicines.html" class="menu-item"><i data-lucide="pill"></i> My Medicines</a>
            <a href="schedule.html" class="menu-item"><i data-lucide="calendar"></i> Schedule</a>
            <a href="medicine-logs.html" class="menu-item"><i data-lucide="clipboard-list"></i> History</a>
            <a href="notifications.html" class="menu-item"><i data-lucide="bell"></i> Notifications</a>
            <a href="emergency.html" class="menu-item active text-danger" style="color: var(--danger);"><i data-lucide="alert-circle"></i> Emergency</a>
        `;
    } else if (role === 'caregiver') {
        html = `
            <a href="caregiver-dashboard.html" class="menu-item"><i data-lucide="layout-dashboard"></i> Dashboard</a>
            <a href="patients.html" class="menu-item"><i data-lucide="users"></i> Patients</a>
            <a href="notifications.html" class="menu-item"><i data-lucide="bell"></i> Notifications</a>
            <a href="emergency.html" class="menu-item active text-danger" style="color: var(--danger);"><i data-lucide="alert-circle"></i> Emergency Alerts</a>
        `;
    } else if (role === 'admin') {
        html = `
            <a href="admin-dashboard.html" class="menu-item"><i data-lucide="layout-dashboard"></i> Dashboard</a>
            <a href="emergency.html" class="menu-item active text-danger" style="color: var(--danger);"><i data-lucide="alert-circle"></i> Emergency Alerts</a>
        `;
    }
    
    menu.innerHTML = html;
    lucide.createIcons();
}

async function loadEmergencies(role) {
    try {
        const emergencies = await api.get('/emergency');
        renderEmergencies(emergencies, role);
    } catch (e) {
        // Handled
    }
}

function renderEmergencies(emergencies, role) {
    const tbody = document.querySelector('#emergencyTable tbody');
    if (!emergencies || emergencies.length === 0) {
        tbody.innerHTML = `<tr><td colspan="${role==='patient'?4:5}" style="text-align: center; color: var(--text-muted)">No emergency alerts.</td></tr>`;
        return;
    }

    tbody.innerHTML = emergencies.map(e => {
        let statusBadge = '<span class="badge badge-success">Resolved</span>';
        if (e.status === 'ACTIVE') {
            statusBadge = '<span class="badge badge-danger">ACTIVE</span>';
        }

        const time = new Date(e.createdAt).toLocaleString();
        
        let actionBtn = '-';
        if (role !== 'patient' && e.status === 'ACTIVE') {
            actionBtn = `<button class="btn btn-primary" style="padding: 0.25rem 0.5rem; font-size: 0.75rem;" onclick="resolveEmergency(${e.id})">Resolve</button>`;
        }

        return `
            <tr style="${e.status === 'ACTIVE' ? 'background: rgba(239, 68, 68, 0.05);' : ''}">
                <td style="font-weight: 500;">${e.patient?.user?.name || 'Unknown'}</td>
                <td>${e.message}</td>
                <td>${statusBadge}</td>
                <td>${time}</td>
                <td class="${role === 'patient' ? 'hide-patient' : ''}">${actionBtn}</td>
            </tr>
        `;
    }).join('');

    if (role === 'patient') {
        document.querySelectorAll('.hide-patient').forEach(el => el.style.display = 'none');
    }
}

async function triggerEmergency() {
    if (confirm("Are you sure you want to send an emergency alert to your caregivers?")) {
        try {
            await api.post('/emergency', { message: 'Emergency Alert Triggered by Patient' });
            showToast('Emergency alert sent successfully.', 'success');
            const role = (userPayload.role || userPayload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']).toLowerCase();
            await loadEmergencies(role);
        } catch (e) {
            // Handled
        }
    }
}

async function resolveEmergency(id) {
    if (confirm("Mark this emergency as resolved?")) {
        try {
            await api.put(`/emergency/${id}/resolve`, {});
            showToast("Emergency resolved.", "success");
            const role = (userPayload.role || userPayload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']).toLowerCase();
            await loadEmergencies(role);
        } catch (e) {
            // Handled
        }
    }
}
