// patients.js

let userPayload;
let caregiverId;

document.addEventListener('DOMContentLoaded', async () => {
    userPayload = requireAuth(['caregiver']);
    if (!userPayload) return;

    // We can fetch caregiver id from dashboard response since token doesn't always have exact caregiver DB ID
    // but the backend `GetPatients(caregiverId)` needs caregiverId.
    // Wait, let's fetch dashboard to get caregiverId.
    try {
        const dash = await api.get('/dashboard/caregiver');
        caregiverId = dash.caregiver.id;
        renderPatients(dash.patients);
    } catch (e) {
        console.error(e);
    }

    document.getElementById('linkForm').addEventListener('submit', handleLinkSubmit);
});

function renderPatients(patients) {
    const tbody = document.querySelector('#patientsTable tbody');
    if (!patients || patients.length === 0) {
        tbody.innerHTML = '<tr><td colspan="5" style="text-align: center; color: var(--text-muted)">No linked patients.</td></tr>';
        return;
    }

    tbody.innerHTML = patients.map(p => `
        <tr>
            <td style="font-weight: 500;">${p.name}</td>
            <td>${p.email}</td>
            <td>${new Date(p.dateOfBirth).toLocaleDateString()}</td>
            <td>${p.emergencyContact || 'N/A'}</td>
            <td>
                <button class="btn btn-outline" style="padding: 0.25rem 0.5rem;" onclick="unlinkPatient(${p.patientId})" title="Unlink">
                    <i data-lucide="unlink"></i> Unlink
                </button>
            </td>
        </tr>
    `).join('');
    
    lucide.createIcons();
}

function openLinkModal() {
    document.getElementById('linkForm').reset();
    document.getElementById('linkModal').classList.add('active');
}

function closeModal() {
    document.getElementById('linkModal').classList.remove('active');
}

async function handleLinkSubmit(e) {
    e.preventDefault();
    const email = document.getElementById('patientEmail').value;
    
    try {
        await api.post('/Caregiverpatient/link', {
            caregiverId: caregiverId,
            patientEmail: email
        });
        showToast("Patient linked successfully.", "success");
        closeModal();
        
        // Reload
        const dash = await api.get('/dashboard/caregiver');
        renderPatients(dash.patients);
    } catch (e) {
        // handled
    }
}

async function unlinkPatient(patientId) {
    if (confirm("Are you sure you want to unlink this patient?")) {
        try {
            await api.delete(`/Caregiverpatient/unlink?caregiverId=${caregiverId}&patientId=${patientId}`);
            showToast("Patient unlinked.", "success");
            
            const dash = await api.get('/dashboard/caregiver');
            renderPatients(dash.patients);
        } catch (e) {
            // Handled
        }
    }
}
