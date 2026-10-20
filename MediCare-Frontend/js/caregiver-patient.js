// Caregiver-Patient Network Controller
let currentRole = null;
let currentCaregiverId = null;
let currentPatientId = null;
let recordsList = [];

document.addEventListener('DOMContentLoaded', async () => {
    const user = Auth.requireAuth(['PATIENT', 'CAREGIVER', 'ADMIN']);
    if (!user) return;

    currentRole = Auth.getRole();
    Auth.setupNavbar();
    Auth.renderSidebar('caregiver-patient');

    const form = document.getElementById('linkPatientForm');
    if (form) form.addEventListener('submit', handleLinkSubmit);

    await loadNetworkData();
});

async function loadNetworkData() {
    try {
        if (currentRole === 'CAREGIVER') {
            await loadCaregiverView();
        } else if (currentRole === 'PATIENT') {
            await loadPatientView();
        } else if (currentRole === 'ADMIN') {
            await loadAdminView();
        }
    } catch (err) {
        console.error('Failed to load network data:', err);
    }
}

// 1. Caregiver View: List of linked patients
async function loadCaregiverView() {
    document.getElementById('networkPageTitle').textContent = 'Linked Patients Under Supervision';
    document.getElementById('networkPageSubtitle').textContent = 'Supervise medication adherence, review treatment plans, and manage family relationships.';
    document.getElementById('tableCardHeading').textContent = 'Supervised Patients';

    const actions = document.getElementById('networkPageActions');
    actions.innerHTML = `
        <button class="btn btn-primary" onclick="openLinkModal()">
            <i data-lucide="user-plus"></i> Link New Patient
        </button>
    `;

    // Fetch caregiver dashboard to get profile & linked patients
    const dash = await api.get('/dashboard/caregiver');
    currentCaregiverId = dash.caregiver?.id;
    recordsList = dash.patients || [];

    // Table Header
    document.getElementById('networkTableHead').innerHTML = `
        <tr>
            <th>Patient Name</th>
            <th>Email Address</th>
            <th>Date of Birth</th>
            <th>Emergency Contact</th>
            <th>Actions</th>
        </tr>
    `;

    renderCaregiverTable(recordsList);
    if (window.lucide) lucide.createIcons();
}

function renderCaregiverTable(patients = []) {
    const tbody = document.getElementById('networkTableBody');
    if (!patients || patients.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="5" class="empty-state">
                    <i data-lucide="users"></i>
                    <h4>No Patients Linked Yet</h4>
                    <p>Click "Link New Patient" above to connect family members using their email address.</p>
                </td>
            </tr>
        `;
        if (window.lucide) lucide.createIcons({ root: tbody });
        return;
    }

    tbody.innerHTML = patients.map(p => `
        <tr>
            <td style="font-weight: 600;">
                <div style="display: flex; align-items: center; gap: 0.75rem;">
                    <div class="avatar" style="width: 32px; height: 32px; font-size: 0.75rem;">
                        ${Utils.getInitials(p.name)}
                    </div>
                    <span>${Utils.escapeHtml(p.name)}</span>
                </div>
            </td>
            <td>${Utils.escapeHtml(p.email)}</td>
            <td>${Utils.formatDate(p.dateOfBirth)}</td>
            <td>${Utils.escapeHtml(p.emergencyContact || 'Not specified')}</td>
            <td>
                <button class="btn btn-sm btn-outline" style="color: #dc2626; border-color: rgba(220, 38, 38, 0.3);" onclick="unlinkPatient(${p.patientId})" title="Unlink patient">
                    <i data-lucide="unlink"></i> Unlink
                </button>
            </td>
        </tr>
    `).join('');

    if (window.lucide) lucide.createIcons({ root: tbody });
}

// 2. Patient View: List of linked caregivers
async function loadPatientView() {
    document.getElementById('networkPageTitle').textContent = 'My Caregivers & Guardians';
    document.getElementById('networkPageSubtitle').textContent = 'Family members and caregivers connected to your account for medication reminders and safety notifications.';
    document.getElementById('tableCardHeading').textContent = 'Supervising Caregivers';
    document.getElementById('patientInfoNote').style.display = 'flex';

    // Fetch patient profile to get patientId
    const dash = await api.get('/dashboard/patient');
    currentPatientId = dash.patient?.id;

    if (!currentPatientId) {
        renderPatientTable([]);
        return;
    }

    // Call GET /api/Caregiverpatient/patient/{patientId}
    const caregivers = await api.get(`/Caregiverpatient/patient/${currentPatientId}`);
    recordsList = Array.isArray(caregivers) ? caregivers : [];

    // Table Header
    document.getElementById('networkTableHead').innerHTML = `
        <tr>
            <th>Caregiver Name</th>
            <th>Email Address</th>
            <th>Relationship</th>
            <th>Status</th>
        </tr>
    `;

    renderPatientTable(recordsList);
    if (window.lucide) lucide.createIcons();
}

function renderPatientTable(caregivers = []) {
    const tbody = document.getElementById('networkTableBody');
    if (!caregivers || caregivers.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="4" class="empty-state">
                    <i data-lucide="shield-alert"></i>
                    <h4>No Caregivers Connected</h4>
                    <p>Provide your registered email to your family member or caregiver so they can link to your account.</p>
                </td>
            </tr>
        `;
        if (window.lucide) lucide.createIcons({ root: tbody });
        return;
    }

    tbody.innerHTML = caregivers.map(c => {
        const name = c.user?.name || 'Caregiver';
        const email = c.user?.email || '-';
        const rel = c.relationship || 'Guardian / Family';

        return `
            <tr>
                <td style="font-weight: 600;">
                    <div style="display: flex; align-items: center; gap: 0.75rem;">
                        <div class="avatar" style="width: 32px; height: 32px; font-size: 0.75rem;">
                            ${Utils.getInitials(name)}
                        </div>
                        <span>${Utils.escapeHtml(name)}</span>
                    </div>
                </td>
                <td>${Utils.escapeHtml(email)}</td>
                <td><span class="badge badge-info">${Utils.escapeHtml(rel)}</span></td>
                <td><span class="badge badge-success">Active Connection</span></td>
            </tr>
        `;
    }).join('');

    if (window.lucide) lucide.createIcons({ root: tbody });
}

// 3. Admin View
async function loadAdminView() {
    document.getElementById('networkPageTitle').textContent = 'Caregiver-Patient Associations';
    document.getElementById('networkPageSubtitle').textContent = 'Administrative directory of active caregiver and patient linkages.';
    document.getElementById('tableCardHeading').textContent = 'System Network Links';

    const dash = await api.get('/dashboard/admin');
    recordsList = [];

    document.getElementById('networkTableHead').innerHTML = `
        <tr>
            <th>System Statistics</th>
            <th>Value</th>
            <th>Description</th>
        </tr>
    `;

    const tbody = document.getElementById('networkTableBody');
    tbody.innerHTML = `
        <tr>
            <td><strong>Total Patient Accounts</strong></td>
            <td>${dash.totalPatients || 0}</td>
            <td>Registered individuals receiving medication schedules</td>
        </tr>
        <tr>
            <td><strong>Total Caregiver Accounts</strong></td>
            <td>${dash.totalCaregivers || 0}</td>
            <td>Supervising family members and guardians</td>
        </tr>
        <tr>
            <td><strong>Active Linkages</strong></td>
            <td>${dash.caregiverPatientRelationships || 0}</td>
            <td>Total connected caregiver-patient pairs in database</td>
        </tr>
    `;
}

// Filter table
window.filterNetworkTable = function() {
    const q = (document.getElementById('networkSearchInput')?.value || '').toLowerCase().trim();
    if (!q) {
        if (currentRole === 'CAREGIVER') renderCaregiverTable(recordsList);
        else if (currentRole === 'PATIENT') renderPatientTable(recordsList);
        return;
    }

    if (currentRole === 'CAREGIVER') {
        const filtered = recordsList.filter(p => 
            (p.name && p.name.toLowerCase().includes(q)) ||
            (p.email && p.email.toLowerCase().includes(q))
        );
        renderCaregiverTable(filtered);
    } else if (currentRole === 'PATIENT') {
        const filtered = recordsList.filter(c => {
            const name = (c.user?.name || '').toLowerCase();
            const email = (c.user?.email || '').toLowerCase();
            return name.includes(q) || email.includes(q);
        });
        renderPatientTable(filtered);
    }
};

// Modals
window.openLinkModal = function() {
    document.getElementById('linkPatientForm').reset();
    document.getElementById('linkPatientModal').style.display = 'flex';
};

window.closeLinkModal = function() {
    document.getElementById('linkPatientModal').style.display = 'none';
};

async function handleLinkSubmit(e) {
    e.preventDefault();
    const email = document.getElementById('linkPatientEmail').value.trim();
    const relation = document.getElementById('linkPatientRelation').value.trim() || 'Patient';
    const btn = document.getElementById('btnSubmitLink');

    if (!email) {
        Utils.showToast('Please enter the patient email.', 'warning');
        return;
    }

    if (!currentCaregiverId) {
        Utils.showToast('Unable to determine Caregiver identity. Please refresh.', 'error');
        return;
    }

    try {
        btn.disabled = true;
        btn.innerHTML = 'Linking...';

        await api.post('/Caregiverpatient/link', {
            caregiverId: currentCaregiverId,
            patientEmail: email,
            relationship: relation
        });

        Utils.showToast('Patient linked successfully!', 'success');
        closeLinkModal();
        await loadNetworkData();

    } catch (err) {
        console.error('Failed to link patient:', err);
    } finally {
        btn.disabled = false;
        btn.innerHTML = '<i data-lucide="link"></i> Confirm & Link';
        if (window.lucide) lucide.createIcons();
    }
}

window.unlinkPatient = async function(patientId) {
    if (!currentCaregiverId || !patientId) return;

    const confirmed = await Utils.confirmAction(
        'Are you sure you want to unlink this patient? You will no longer receive their dose alerts or emergency notifications.',
        'Unlink Patient',
        'warning'
    );
    if (!confirmed) return;

    try {
        await api.delete('/Caregiverpatient/unlink', {
            caregiverId: currentCaregiverId,
            patientId: patientId
        });

        Utils.showToast('Patient unlinked successfully.', 'success');
        await loadNetworkData();
    } catch (err) {
        console.error('Failed to unlink patient:', err);
    }
};
