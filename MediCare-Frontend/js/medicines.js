// Medicines Management Controller
let currentMedicines = [];
let isLowStockFiltered = false;
let userRole = null;
let currentPatientId = 0;

document.addEventListener('DOMContentLoaded', async () => {
    const user = Auth.requireAuth(['PATIENT', 'CAREGIVER', 'ADMIN']);
    if (!user) return;

    userRole = Auth.getRole();
    setupSidebar(userRole);
    setupUI();

    await initPatientContext();
    await loadMedicines();

    const form = document.getElementById('medicineForm');
    if (form) form.addEventListener('submit', handleMedicineSubmit);
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
            <a href="medicines.html" class="menu-item active"><i data-lucide="pill"></i><span>My Medicines</span></a>
            <a href="schedule.html" class="menu-item"><i data-lucide="calendar"></i><span>Dose Schedule</span></a>
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
            <a href="medicines.html" class="menu-item active"><i data-lucide="pill"></i><span>Prescriptions</span></a>
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
            <a href="medicines.html" class="menu-item active"><i data-lucide="pill"></i><span>All Medicines</span></a>
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

let cachedPatients = [];

async function initPatientContext() {
    try {
        if (userRole === 'PATIENT') {
            const dash = await api.get('/dashboard/patient');
            currentPatientId = dash.patient?.id || 0;
        } else if (userRole === 'CAREGIVER') {
            const dash = await api.get('/dashboard/caregiver');
            cachedPatients = dash.patients || [];

            const selectGroup = document.getElementById('patientSelectGroup');
            const select = document.getElementById('medPatientId');
            if (selectGroup && select) {
                selectGroup.style.display = 'block';
                select.innerHTML = '<option value="">-- Choose Patient --</option>' + 
                    cachedPatients.map(p => `<option value="${p.patientId}">${Utils.escapeHtml(p.name)} (${Utils.escapeHtml(p.email)})</option>`).join('');
            }
        } else if (userRole === 'ADMIN') {
            const selectGroup = document.getElementById('patientSelectGroup');
            const select = document.getElementById('medPatientId');
            if (selectGroup && select) {
                selectGroup.style.display = 'block';
                select.innerHTML = '<option value="1">Patient #1 (Default)</option>';
            }
        }
    } catch (err) {
        console.warn('Could not initialize patient context:', err);
    }
}

async function loadMedicines() {
    try {
        let endpoint = '/medicine';
        if (isLowStockFiltered) {
            endpoint = '/medicine/low-stock?threshold=5';
        }

        const data = await api.get(endpoint);
        currentMedicines = Array.isArray(data) ? data : [];
        renderMedicinesTable(currentMedicines);

    } catch (err) {
        console.error('Failed to load medicines:', err);
    }
}

function renderMedicinesTable(medicines = []) {
    const tbody = document.getElementById('medicinesTbody');
    if (!medicines || medicines.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="7" style="text-align: center; padding: 2.5rem 1rem; color: var(--text-muted);">
                    <i data-lucide="pill" style="width: 36px; height: 36px; margin: 0 auto 0.5rem; display: block; opacity: 0.5;"></i>
                    No medications found. Click <strong>"Add Medication"</strong> to record a prescription.
                </td>
            </tr>
        `;
        if (window.lucide) lucide.createIcons({ root: tbody });
        return;
    }

    tbody.innerHTML = medicines.map(m => {
        const start = Utils.formatDate(m.startDate);
        const end = Utils.formatDate(m.endDate);
        const stockBadge = Utils.getStockBadge(m.stock);

        let pName = m.patient?.user?.name;
        if (!pName && m.patientId) {
            const p = cachedPatients.find(x => x.patientId === m.patientId);
            if (p) pName = p.name;
        }

        return `
            <tr>
                <td style="font-weight: 600;">
                    <div style="color: var(--text-main);">${Utils.escapeHtml(m.name)}</div>
                    ${pName ? `<div style="font-size: 0.75rem; color: var(--text-muted);">Patient: <strong>${Utils.escapeHtml(pName)}</strong></div>` : ''}
                </td>
                <td>${Utils.escapeHtml(m.dosage)}</td>
                <td>${m.dose} unit(s)</td>
                <td style="font-weight: 700;">${m.stock}</td>
                <td><span style="font-size: 0.8rem; color: var(--text-muted);">${start} — ${end}</span></td>
                <td>${stockBadge}</td>
                <td>
                    <div style="display: flex; gap: 0.5rem;">
                        <button class="btn btn-sm btn-outline" onclick="openEditMedicineModal(${m.id})" title="Edit medication">
                            <i data-lucide="edit-3" style="width: 14px; height: 14px;"></i>
                            Edit
                        </button>
                        <button class="btn btn-sm btn-outline" style="color: var(--danger); border-color: rgba(239, 68, 68, 0.3);" onclick="deleteMedicineRecord(${m.id})" title="Delete medication">
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

window.filterMedicines = function() {
    const q = (document.getElementById('medicineSearchInput')?.value || '').toLowerCase().trim();
    if (!q) {
        renderMedicinesTable(currentMedicines);
        return;
    }

    const filtered = currentMedicines.filter(m => 
        (m.name && m.name.toLowerCase().includes(q)) ||
        (m.dosage && m.dosage.toLowerCase().includes(q))
    );
    renderMedicinesTable(filtered);
};

window.toggleLowStockFilter = async function() {
    isLowStockFiltered = !isLowStockFiltered;
    const btn = document.getElementById('btnFilterLowStock');
    const btnText = document.getElementById('lowStockBtnText');

    if (isLowStockFiltered) {
        btn.classList.add('btn-primary');
        btn.classList.remove('btn-outline');
        btnText.textContent = 'Showing Low Stock (Clear)';
    } else {
        btn.classList.remove('btn-primary');
        btn.classList.add('btn-outline');
        btnText.textContent = 'Low Stock Only';
    }

    await loadMedicines();
};

window.openAddMedicineModal = function() {
    document.getElementById('modalTitle').innerHTML = '<i data-lucide="pill"></i> Add Medication';
    document.getElementById('medicineForm').reset();
    document.getElementById('medId').value = '';

    // Default dates
    const today = new Date().toISOString().split('T')[0];
    document.getElementById('medStart').value = today;

    const nextMonth = new Date();
    nextMonth.setMonth(nextMonth.getMonth() + 1);
    document.getElementById('medEnd').value = nextMonth.toISOString().split('T')[0];

    Utils.openModal('medicineModal');
};

window.openEditMedicineModal = function(id) {
    const med = currentMedicines.find(m => m.id === id);
    if (!med) return;

    document.getElementById('modalTitle').innerHTML = '<i data-lucide="edit-3"></i> Edit Medication';
    document.getElementById('medId').value = med.id;
    document.getElementById('medName').value = med.name;
    document.getElementById('medDosage').value = med.dosage;
    document.getElementById('medDose').value = med.dose;
    document.getElementById('medStock').value = med.stock;
    document.getElementById('medStart').value = med.startDate.split('T')[0];
    document.getElementById('medEnd').value = med.endDate.split('T')[0];

    const patSelect = document.getElementById('medPatientId');
    if (patSelect && med.patientId) {
        patSelect.value = med.patientId;
    }

    Utils.openModal('medicineModal');
};

async function handleMedicineSubmit(e) {
    e.preventDefault();
    const id = document.getElementById('medId').value;
    const submitBtn = document.getElementById('btnSaveMedicine');

    let patientId = currentPatientId;
    if (userRole !== 'PATIENT') {
        const selected = document.getElementById('medPatientId')?.value;
        if (selected) patientId = parseInt(selected, 10);
    }

    const payload = {
        patientId: patientId,
        name: document.getElementById('medName').value.trim(),
        dosage: document.getElementById('medDosage').value.trim(),
        dose: parseInt(document.getElementById('medDose').value, 10),
        stock: parseInt(document.getElementById('medStock').value, 10),
        startDate: document.getElementById('medStart').value,
        endDate: document.getElementById('medEnd').value
    };

    if (new Date(payload.endDate) < new Date(payload.startDate)) {
        Utils.showToast('Treatment end date cannot be earlier than start date.', 'warning');
        return;
    }

    try {
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<span class="spinner" style="width: 16px; height: 16px; border-width: 2px;"></span> Saving...';

        if (id) {
            await api.put(`/medicine/${id}`, { id: parseInt(id, 10), ...payload });
            Utils.showToast('Medication updated successfully.', 'success');
        } else {
            await api.post('/medicine', payload);
            Utils.showToast('Medication added to prescription list.', 'success');
        }

        Utils.closeModal('medicineModal');
        await loadMedicines();

    } catch (err) {
        console.error('Failed to save medication:', err);
    } finally {
        submitBtn.disabled = false;
        submitBtn.innerHTML = '<i data-lucide="save"></i> Save Medication';
        if (window.lucide) lucide.createIcons({ root: submitBtn });
    }
}

window.deleteMedicineRecord = async function(id) {
    const confirmed = await Utils.confirmAction(
        'Are you sure you want to delete this medication? This will also remove associated dose schedules and logs.',
        'Delete Medication'
    );
    if (!confirmed) return;

    try {
        await api.delete(`/medicine/${id}`);
        Utils.showToast('Medication deleted successfully.', 'success');
        await loadMedicines();
    } catch (err) {
        console.error('Failed to delete medicine:', err);
    }
};
