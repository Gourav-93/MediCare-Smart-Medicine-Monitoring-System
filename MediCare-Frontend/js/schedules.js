// schedules.js

let userPayload;
let currentSchedules = [];
let currentMedicines = [];

document.addEventListener('DOMContentLoaded', async () => {
    userPayload = requireAuth(['patient']);
    if (!userPayload) return;

    await loadData();

    document.getElementById('scheduleForm').addEventListener('submit', handleScheduleSubmit);
});

async function loadData() {
    try {
        const meds = await api.get('/medicine');
        currentMedicines = meds;

        const schs = await api.get('/medicineschedule');
        // Filter schedules for the current patient's medicines
        const medIds = currentMedicines.map(m => m.id);
        
        // Ensure sch.medicineId matches one of currentMedicines
        // If the API doesn't return Medicine object, we link it manually
        currentSchedules = schs.filter(s => medIds.includes(s.medicineId));

        renderSchedules(currentSchedules);
        populateMedicineDropdown();
    } catch (e) {
        // Handled
    }
}

function formatTime(timeStr) {
    if (!timeStr) return '';
    const parts = timeStr.split(':');
    if (parts.length >= 2) {
        const d = new Date();
        d.setHours(parseInt(parts[0], 10));
        d.setMinutes(parseInt(parts[1], 10));
        return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    }
    return timeStr;
}

function renderSchedules(schedules) {
    const tbody = document.querySelector('#scheduleTable tbody');
    if (!schedules || schedules.length === 0) {
        tbody.innerHTML = '<tr><td colspan="4" style="text-align: center; color: var(--text-muted)">No schedules added yet.</td></tr>';
        return;
    }

    // Sort by time
    schedules.sort((a, b) => a.time.localeCompare(b.time));

    tbody.innerHTML = schedules.map(s => {
        const med = currentMedicines.find(m => m.id === s.medicineId);
        const medName = med ? med.name : 'Unknown';

        return `
            <tr>
                <td style="font-weight: 500;">${medName}</td>
                <td>${formatTime(s.time)}</td>
                <td><span class="badge badge-info">${s.frequency}</span></td>
                <td>
                    <button class="btn btn-outline" style="padding: 0.25rem 0.5rem; margin-right: 0.5rem;" onclick="openEditModal(${s.id})">
                        <i data-lucide="edit"></i>
                    </button>
                    <button class="btn btn-danger" style="padding: 0.25rem 0.5rem;" onclick="deleteSchedule(${s.id})">
                        <i data-lucide="trash-2"></i>
                    </button>
                </td>
            </tr>
        `;
    }).join('');

    lucide.createIcons();
}

function populateMedicineDropdown() {
    const select = document.getElementById('schMedicine');
    select.innerHTML = '<option value="">Select Medicine</option>' + 
        currentMedicines.map(m => `<option value="${m.id}">${m.name} (${m.dosage})</option>`).join('');
}

function openAddModal() {
    document.getElementById('modalTitle').textContent = 'Add Schedule';
    document.getElementById('scheduleForm').reset();
    document.getElementById('schId').value = '';
    document.getElementById('scheduleModal').classList.add('active');
}

function openEditModal(id) {
    const sch = currentSchedules.find(s => s.id === id);
    if (!sch) return;

    document.getElementById('modalTitle').textContent = 'Edit Schedule';
    document.getElementById('schId').value = sch.id;
    document.getElementById('schMedicine').value = sch.medicineId;
    
    // time format from backend is usually "HH:mm:ss"
    const timeParts = sch.time.split(':');
    document.getElementById('schTime').value = `${timeParts[0]}:${timeParts[1]}`;
    document.getElementById('schFrequency').value = sch.frequency;

    document.getElementById('scheduleModal').classList.add('active');
}

function closeModal() {
    document.getElementById('scheduleModal').classList.remove('active');
}

async function handleScheduleSubmit(e) {
    e.preventDefault();
    const id = document.getElementById('schId').value;
    
    // TimeSpan format required by backend
    const timeVal = document.getElementById('schTime').value; // "HH:mm"
    const formattedTime = timeVal.length === 5 ? `${timeVal}:00` : timeVal;

    const payload = {
        medicineId: parseInt(document.getElementById('schMedicine').value),
        time: formattedTime,
        frequency: document.getElementById('schFrequency').value
    };

    try {
        if (id) {
            await api.put(`/medicineschedule/${id}`, { id: parseInt(id), ...payload });
            showToast("Schedule updated.", "success");
        } else {
            await api.post('/medicineschedule', payload);
            showToast("Schedule added.", "success");
        }
        closeModal();
        await loadData();
    } catch (e) {
        // Error handled in API
    }
}

async function deleteSchedule(id) {
    if (confirm("Are you sure you want to delete this schedule?")) {
        try {
            await api.delete(`/medicineschedule/${id}`);
            showToast("Schedule deleted.", "success");
            await loadData();
        } catch (e) {
            // Handled
        }
    }
}
