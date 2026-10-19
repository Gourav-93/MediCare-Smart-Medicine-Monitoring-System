// medicines.js

let userPayload;
let currentMedicines = [];

document.addEventListener('DOMContentLoaded', async () => {
    userPayload = requireAuth(['patient']);
    if (!userPayload) return;

    await loadMedicines();

    document.getElementById('medicineForm').addEventListener('submit', handleMedicineSubmit);
});

async function loadMedicines() {
    try {
        const response = await api.get('/medicine');
        // The API returns all medicines. Since we use Patient role, we only want medicines for this patient.
        // Wait, the API might filter it if it's correct. Let's see. If not, we filter by patientId locally.
        const patientId = userPayload.id || getJwtPayload().id; 
        
        currentMedicines = response; 
        renderMedicines(currentMedicines);
    } catch (e) {
        // Error handled in api
    }
}

function renderMedicines(medicines) {
    const tbody = document.querySelector('#medicinesTable tbody');
    if (!medicines || medicines.length === 0) {
        tbody.innerHTML = '<tr><td colspan="7" style="text-align: center; color: var(--text-muted)">No medicines added yet.</td></tr>';
        return;
    }

    const today = new Date();

    tbody.innerHTML = medicines.map(m => {
        const start = new Date(m.startDate).toLocaleDateString();
        const end = new Date(m.endDate).toLocaleDateString();
        
        let stockBadge = '<span class="badge badge-success">Healthy</span>';
        if (m.stock <= 5 && m.stock > 0) stockBadge = '<span class="badge badge-warning">Low</span>';
        if (m.stock === 0) stockBadge = '<span class="badge badge-danger">Empty</span>';

        return `
            <tr>
                <td style="font-weight: 500;">${m.name}</td>
                <td>${m.dosage}</td>
                <td>${m.dose}</td>
                <td>${m.stock}</td>
                <td>${start} - ${end}</td>
                <td>${stockBadge}</td>
                <td>
                    <button class="btn btn-outline" style="padding: 0.25rem 0.5rem; margin-right: 0.5rem;" onclick="openEditModal(${m.id})">
                        <i data-lucide="edit"></i>
                    </button>
                    <button class="btn btn-danger" style="padding: 0.25rem 0.5rem;" onclick="deleteMedicine(${m.id})">
                        <i data-lucide="trash-2"></i>
                    </button>
                </td>
            </tr>
        `;
    }).join('');

    lucide.createIcons();
}

function openAddModal() {
    document.getElementById('modalTitle').textContent = 'Add Medicine';
    document.getElementById('medicineForm').reset();
    document.getElementById('medId').value = '';
    
    // Set default dates
    const today = new Date().toISOString().split('T')[0];
    document.getElementById('medStart').value = today;
    
    const nextMonth = new Date();
    nextMonth.setMonth(nextMonth.getMonth() + 1);
    document.getElementById('medEnd').value = nextMonth.toISOString().split('T')[0];

    document.getElementById('medicineModal').classList.add('active');
}

function openEditModal(id) {
    const med = currentMedicines.find(m => m.id === id);
    if (!med) return;

    document.getElementById('modalTitle').textContent = 'Edit Medicine';
    document.getElementById('medId').value = med.id;
    document.getElementById('medName').value = med.name;
    document.getElementById('medDosage').value = med.dosage;
    document.getElementById('medDose').value = med.dose;
    document.getElementById('medStock').value = med.stock;
    document.getElementById('medStart').value = med.startDate.split('T')[0];
    document.getElementById('medEnd').value = med.endDate.split('T')[0];

    document.getElementById('medicineModal').classList.add('active');
}

function closeModal() {
    document.getElementById('medicineModal').classList.remove('active');
}

async function handleMedicineSubmit(e) {
    e.preventDefault();
    const id = document.getElementById('medId').value;
    
    // Convert patientId from JWT claim to int
    const patientIdStr = getJwtPayload().id || getJwtPayload()['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];
    
    const payload = {
        name: document.getElementById('medName').value,
        dosage: document.getElementById('medDosage').value,
        dose: parseInt(document.getElementById('medDose').value),
        stock: parseInt(document.getElementById('medStock').value),
        startDate: document.getElementById('medStart').value,
        endDate: document.getElementById('medEnd').value,
        patientId: parseInt(patientIdStr)
    };

    if (new Date(payload.endDate) < new Date(payload.startDate)) {
        showToast("End date cannot be before start date.", "error");
        return;
    }

    try {
        if (id) {
            await api.put(`/medicine/${id}`, { id: parseInt(id), ...payload });
            showToast("Medicine updated.", "success");
        } else {
            await api.post('/medicine', payload);
            showToast("Medicine added.", "success");
        }
        closeModal();
        await loadMedicines();
    } catch (e) {
        // Error handled in API
    }
}

async function deleteMedicine(id) {
    if (confirm("Are you sure you want to delete this medicine?")) {
        try {
            await api.delete(`/medicine/${id}`);
            showToast("Medicine deleted.", "success");
            await loadMedicines();
        } catch (e) {
            // Handled
        }
    }
}
