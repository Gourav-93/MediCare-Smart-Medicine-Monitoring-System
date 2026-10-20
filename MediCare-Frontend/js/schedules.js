// Schedules Controller
let currentSchedules = [];
let availableMedicines = [];
let userRole = null;

document.addEventListener('DOMContentLoaded', async () => {
    const user = Auth.requireAuth(['PATIENT', 'CAREGIVER', 'ADMIN']);
    if (!user) return;

    userRole = Auth.getRole();
    setupSidebar(userRole);
    setupUI();

    await loadScheduleData();

    const form = document.getElementById('scheduleForm');
    if (form) form.addEventListener('submit', handleScheduleSubmit);
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
            <a href="schedule.html" class="menu-item active"><i data-lucide="calendar"></i><span>Dose Schedule</span></a>
            <a href="medicine-logs.html" class="menu-item"><i data-lucide="clipboard-list"></i><span>Intake History</span></a>
            <a href="adherence.html" class="menu-item"><i data-lucide="trending-up"></i><span>Adherence Reports</span></a>
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
            <a href="schedule.html" class="menu-item active"><i data-lucide="calendar"></i><span>Schedules</span></a>
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
            <a href="schedule.html" class="menu-item active"><i data-lucide="calendar"></i><span>Dose Schedules</span></a>
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

let cachedPatients = [];

async function loadScheduleData() {
    try {
        const promises = [
            api.get('/medicine'),
            api.get('/medicineschedule')
        ];

        if (userRole === 'CAREGIVER') {
            promises.push(api.get('/dashboard/caregiver').catch(() => ({})));
        }

        const [meds, schs, dash] = await Promise.all(promises);

        availableMedicines = Array.isArray(meds) ? meds : [];
        currentSchedules = Array.isArray(schs) ? schs : [];
        if (dash && dash.patients) {
            cachedPatients = dash.patients;
        }

        populateMedicineDropdown();
        renderSchedulesTable(currentSchedules);

    } catch (err) {
        console.error('Failed to load schedule data:', err);
    }
}

function populateMedicineDropdown() {
    const select = document.getElementById('schMedicine');
    if (!select) return;

    if (availableMedicines.length === 0) {
        select.innerHTML = '<option value="">No medications available. Add medicine first.</option>';
        return;
    }

    select.innerHTML = '<option value="">-- Choose Prescribed Medicine --</option>' + 
        availableMedicines.map(m => {
            let patName = m.patient?.user?.name;
            if (!patName && m.patientId) {
                const p = cachedPatients.find(x => x.patientId === m.patientId);
                if (p) patName = p.name;
            }
            const pat = patName ? ` [Patient: ${patName}]` : '';
            return `<option value="${m.id}">${Utils.escapeHtml(m.name)} (${Utils.escapeHtml(m.dosage)})${Utils.escapeHtml(pat)}</option>`;
        }).join('');
}

function renderSchedulesTable(schedules = []) {
    const tbody = document.getElementById('schedulesTbody');
    if (!schedules || schedules.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="5" style="text-align: center; padding: 2.5rem 1rem; color: var(--text-muted);">
                    <i data-lucide="calendar-x" style="width: 36px; height: 36px; margin: 0 auto 0.5rem; display: block; opacity: 0.5;"></i>
                    No dose schedules configured yet. Click <strong>"Add Schedule Time"</strong> to set reminder hours.
                </td>
            </tr>
        `;
        if (window.lucide) lucide.createIcons({ root: tbody });
        return;
    }

    // Sort chronologically by time
    schedules.sort((a, b) => String(a.time).localeCompare(String(b.time)));

    tbody.innerHTML = schedules.map(s => {
        const med = s.medicine || availableMedicines.find(m => m.id === s.medicineId);
        const medName = med?.name || 'Prescribed Medicine';
        const dosage = med?.dosage || '';
        
        let patientName = med?.patient?.user?.name;
        if (!patientName && med?.patientId) {
            const p = cachedPatients.find(x => x.patientId === med.patientId);
            if (p) patientName = p.name;
        }
        if (!patientName) patientName = userRole === 'PATIENT' ? 'Self' : 'Patient';

        const timeFormatted = Utils.formatTime(s.time);

        return `
            <tr>
                <td style="font-weight: 600;">
                    <div style="color: var(--text-main);">${Utils.escapeHtml(medName)}</div>
                    <div style="font-size: 0.75rem; color: var(--text-muted);">${Utils.escapeHtml(dosage)}</div>
                </td>
                <td>
                    <span class="badge badge-info" style="font-size: 0.85rem; padding: 0.35rem 0.65rem;">
                        <i data-lucide="clock" style="width: 12px; height: 12px;"></i>
                        ${timeFormatted}
                    </span>
                </td>
                <td><span class="badge badge-neutral">${Utils.escapeHtml(s.frequency || 'Daily')}</span></td>
                <td><span style="font-size: 0.85rem; color: var(--text-muted);">${Utils.escapeHtml(patientName)}</span></td>
                <td>
                    <div style="display: flex; gap: 0.5rem;">
                        <button class="btn btn-sm btn-outline" onclick="openEditScheduleModal(${s.id})" title="Edit schedule">
                            <i data-lucide="edit-3" style="width: 14px; height: 14px;"></i>
                            Edit
                        </button>
                        <button class="btn btn-sm btn-outline" style="color: var(--danger); border-color: rgba(239, 68, 68, 0.3);" onclick="deleteScheduleRecord(${s.id})" title="Delete schedule">
                            <i data-lucide="trash-2" style="width: 14px; height: 14px;"></i>
                            Delete
                        </button>
                    </div>
                </td>
            </tr>
        `;
    }).join('');

    if (window.lucide) lucide.createIcons({ root: tbody });
}

window.openAddScheduleModal = function() {
    document.getElementById('modalTitle').innerHTML = '<i data-lucide="calendar"></i> Add Schedule Time';
    document.getElementById('scheduleForm').reset();
    document.getElementById('schId').value = '';
    document.getElementById('schTime').value = '08:00';
    Utils.openModal('scheduleModal');
};

window.openEditScheduleModal = function(id) {
    const sch = currentSchedules.find(s => s.id === id);
    if (!sch) return;

    document.getElementById('modalTitle').innerHTML = '<i data-lucide="edit-3"></i> Edit Schedule Time';
    document.getElementById('schId').value = sch.id;
    document.getElementById('schMedicine').value = sch.medicineId;

    // Time from backend can be "08:00:00" or TimeSpan
    const rawTime = String(sch.time || '08:00');
    const timeParts = rawTime.split(':');
    if (timeParts.length >= 2) {
        document.getElementById('schTime').value = `${timeParts[0].padStart(2, '0')}:${timeParts[1].padStart(2, '0')}`;
    }

    document.getElementById('schFrequency').value = sch.frequency || 'Daily';
    Utils.openModal('scheduleModal');
};

async function handleScheduleSubmit(e) {
    e.preventDefault();
    const id = document.getElementById('schId').value;
    const submitBtn = document.getElementById('btnSaveSchedule');

    const medId = parseInt(document.getElementById('schMedicine').value, 10);
    const timeInput = document.getElementById('schTime').value; // "08:00"
    const frequency = document.getElementById('schFrequency').value;

    if (!medId || !timeInput) {
        Utils.showToast('Please select a medicine and specify the dose time.', 'warning');
        return;
    }

    // Format time to "HH:mm:ss" for TimeSpan deserialization
    const formattedTime = timeInput.length === 5 ? `${timeInput}:00` : timeInput;

    const payload = {
        medicineId: medId,
        time: formattedTime,
        frequency: frequency
    };

    try {
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<span class="spinner" style="width: 16px; height: 16px; border-width: 2px;"></span> Saving...';

        if (id) {
            await api.put(`/medicineschedule/${id}`, { id: parseInt(id, 10), ...payload });
            Utils.showToast('Schedule updated successfully.', 'success');
        } else {
            await api.post('/medicineschedule', payload);
            Utils.showToast('Schedule created successfully.', 'success');
        }

        Utils.closeModal('scheduleModal');
        await loadScheduleData();

    } catch (err) {
        console.error('Failed to save schedule:', err);
    } finally {
        submitBtn.disabled = false;
        submitBtn.innerHTML = '<i data-lucide="save"></i> Save Schedule';
        if (window.lucide) lucide.createIcons({ root: submitBtn });
    }
}

window.deleteScheduleRecord = async function(id) {
    const confirmed = await Utils.confirmAction(
        'Are you sure you want to delete this dose schedule?',
        'Delete Schedule'
    );
    if (!confirmed) return;

    try {
        await api.delete(`/medicineschedule/${id}`);
        Utils.showToast('Schedule deleted successfully.', 'success');
        await loadScheduleData();
    } catch (err) {
        console.error('Failed to delete schedule:', err);
    }
};
