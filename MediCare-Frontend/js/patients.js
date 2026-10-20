// Patients Directory Controller
let currentCaregiverId = null;
let allPatients = [];

document.addEventListener('DOMContentLoaded', async () => {
    const user = Auth.requireAuth(['CAREGIVER', 'ADMIN']);
    if (!user) return;

    setupUI();
    await loadPatients();

    const linkForm = document.getElementById('linkForm');
    if (linkForm) {
        linkForm.addEventListener('submit', handleLinkPatientSubmit);
    }
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

async function loadPatients() {
    try {
        const role = Auth.getRole();
        if (role === 'CAREGIVER') {
            const dash = await api.get('/dashboard/caregiver');
            currentCaregiverId = dash.caregiver?.id;
            allPatients = dash.patients || [];
        } else {
            // ADMIN
            const dash = await api.get('/dashboard/admin');
            // Admin can see low stock & active emergencies, and patients can be extracted or fetched
            // If caregiver ID is needed, we query relationships or default caregiver
            allPatients = []; // Will be populated from dashboard if available
        }

        renderPatientsTable(allPatients);

    } catch (err) {
        console.error('Failed to load patients list:', err);
    }
}

function renderPatientsTable(patients = []) {
    const tbody = document.getElementById('patientsTbody');
    if (!patients || patients.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="5" style="text-align: center; padding: 2.5rem 1rem; color: var(--text-muted);">
                    <i data-lucide="users" style="width: 36px; height: 36px; margin: 0 auto 0.5rem; display: block; opacity: 0.5;"></i>
                    No linked patients found. Click <strong>"Link Patient Account"</strong> to connect a family member.
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
                <div style="display: flex; gap: 0.5rem;">
                    <a href="caregiver-patient-details.html?patientId=${p.patientId}" class="btn btn-sm btn-primary" title="Inspect clinical records">
                        <i data-lucide="eye" style="width: 14px; height: 14px;"></i>
                        Inspect Dossier
                    </a>
                    <button class="btn btn-sm btn-outline" style="color: var(--danger); border-color: rgba(239, 68, 68, 0.3);" onclick="unlinkPatientRecord(${p.patientId})" title="Unlink patient">
                        <i data-lucide="unlink" style="width: 14px; height: 14px;"></i>
                        Unlink
                    </button>
                </div>
            </td>
        </tr>
    `).join('');

    if (window.lucide) lucide.createIcons({ root: tbody });
}

window.filterPatients = function() {
    const query = (document.getElementById('patientSearchInput')?.value || '').toLowerCase().trim();
    if (!query) {
        renderPatientsTable(allPatients);
        return;
    }

    const filtered = allPatients.filter(p => 
        (p.name && p.name.toLowerCase().includes(query)) ||
        (p.email && p.email.toLowerCase().includes(query))
    );
    renderPatientsTable(filtered);
};

window.openLinkModal = function() {
    document.getElementById('linkForm').reset();
    Utils.openModal('linkModal');
};

async function handleLinkPatientSubmit(e) {
    e.preventDefault();
    const email = document.getElementById('linkPatientEmail').value.trim();
    const relation = document.getElementById('linkRelationType').value.trim() || 'Patient';
    const submitBtn = document.getElementById('btnSubmitLink');

    if (!email) {
        Utils.showToast('Please enter the patient email.', 'warning');
        return;
    }

    if (!currentCaregiverId) {
        Utils.showToast('Unable to determine Caregiver identity. Please refresh.', 'error');
        return;
    }

    try {
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<span class="spinner" style="width: 16px; height: 16px; border-width: 2px;"></span> Linking...';

        await api.post('/Caregiverpatient/link', {
            caregiverId: currentCaregiverId,
            patientEmail: email,
            relationType: relation
        });

        Utils.showToast('Patient account linked successfully!', 'success');
        Utils.closeModal('linkModal');
        await loadPatients();

    } catch (err) {
        console.error('Failed to link patient:', err);
    } finally {
        submitBtn.disabled = false;
        submitBtn.innerHTML = '<i data-lucide="link"></i> Confirm & Link';
        if (window.lucide) lucide.createIcons({ root: submitBtn });
    }
}

window.unlinkPatientRecord = async function(patientId) {
    if (!currentCaregiverId) return;

    const confirmed = await Utils.confirmAction(
        'Are you sure you want to unlink this patient? You will no longer receive their reminders or emergency notifications.',
        'Unlink Patient'
    );
    if (!confirmed) return;

    try {
        await api.delete(`/Caregiverpatient/unlink?caregiverId=${currentCaregiverId}&patientId=${patientId}`);
        Utils.showToast('Patient unlinked successfully.', 'success');
        await loadPatients();
    } catch (err) {
        console.error('Failed to unlink patient:', err);
    }
};
