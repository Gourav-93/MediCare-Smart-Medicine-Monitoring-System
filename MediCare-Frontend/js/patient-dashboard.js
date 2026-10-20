// Patient Bento-Grid Dashboard Controller
document.addEventListener('DOMContentLoaded', async () => {
    const user = Auth.requireAuth(['PATIENT']);
    if (!user) return;

    setupUI();
    await loadPatientDashboard();
});

let cachedDashboard = null;

function setupUI() {
    // Current date display
    const dateEl = document.getElementById('currentDateDisplay');
    if (dateEl) {
        dateEl.textContent = new Date().toLocaleDateString('en-US', {
            weekday: 'short',
            month: 'short',
            day: 'numeric'
        });
    }

    // Sidebar drawer toggle for mobile
    const toggleBtn = document.getElementById('sidebarToggle');
    const sidebar = document.getElementById('sidebar');
    if (toggleBtn && sidebar) {
        toggleBtn.addEventListener('click', () => {
            sidebar.classList.toggle('open');
        });
    }

    // Wire SOS Form
    const sosForm = document.getElementById('sosForm');
    if (sosForm) {
        sosForm.addEventListener('submit', handleSosSubmit);
    }
}

async function loadPatientDashboard() {
    try {
        const data = await api.get('/dashboard/patient');
        cachedDashboard = data;

        // 1. Hero Greeting
        const name = data.patient?.name || (Auth.getUser()?.name) || 'Patient';
        document.getElementById('patientGreeting').textContent = Utils.getGreeting(name);

        const adherencePercent = Math.round(data.adherencePercentage || 0);
        const statusBadge = document.getElementById('complianceStatusBadge');
        if (adherencePercent >= 80) {
            statusBadge.innerHTML = `<span class="badge badge-success">${adherencePercent}% Adherent</span>`;
        } else if (adherencePercent >= 50) {
            statusBadge.innerHTML = `<span class="badge badge-warning">${adherencePercent}% Needs Attention</span>`;
        } else {
            statusBadge.innerHTML = `<span class="badge badge-danger">${adherencePercent}% Critical</span>`;
        }

        // Unread Notifications Badge
        if (data.unreadNotifications > 0) {
            const navBadge = document.getElementById('navUnreadBadge');
            const sideBadge = document.getElementById('sidebarNotifBadge');
            if (navBadge) {
                navBadge.textContent = data.unreadNotifications;
                navBadge.style.display = 'flex';
            }
            if (sideBadge) {
                sideBadge.textContent = data.unreadNotifications;
                sideBadge.style.display = 'inline-block';
            }
        }

        // 2. Next Scheduled Dose
        renderNextDose(data.pendingMedicines, data.todaySchedules);

        // 3. Adherence Gauge
        renderAdherenceGauge(data);

        // 4. Today's Medication Schedule
        renderTodaySchedule(data.todaySchedules, data.pendingMedicines, data.takenMedicines, data.missedMedicines);

        // 5. Inventory & Stock Status
        renderInventory(data.medicines);

        // 6. Connected Caregivers
        if (data.patient?.id) {
            loadConnectedCaregivers(data.patient.id);
        }

        // 7. Recent Activity Logs
        renderRecentLogs(data.takenMedicines, data.missedMedicines);

        if (window.lucide) {
            lucide.createIcons();
        }

    } catch (error) {
        console.error('Failed to load patient dashboard:', error);
    }
}

function renderNextDose(pendingMedicines = [], todaySchedules = []) {
    const medNameEl = document.getElementById('nextMedName');
    const medDetailsEl = document.getElementById('nextMedDetails');
    const badgeEl = document.getElementById('nextDoseBadge');
    const actionWrap = document.getElementById('nextDoseActionWrap');

    if (!pendingMedicines || pendingMedicines.length === 0) {
        medNameEl.textContent = 'All Caught Up! 🎉';
        medDetailsEl.textContent = 'No pending medicine doses due right now.';
        badgeEl.textContent = 'Done';
        badgeEl.className = 'badge badge-success';
        actionWrap.innerHTML = '';
        return;
    }

    // Earliest pending dose
    const nextLog = pendingMedicines[0];
    const medName = nextLog.medicine?.name || 'Prescribed Medicine';
    const doseDesc = `${nextLog.medicine?.dose || 1} dose (${nextLog.medicine?.dosage || ''})`;
    const scheduledTimeStr = Utils.formatTime(nextLog.scheduledTime);

    medNameEl.textContent = medName;
    medDetailsEl.textContent = `Scheduled for ${scheduledTimeStr} • ${doseDesc}`;
    badgeEl.textContent = `Due at ${scheduledTimeStr}`;
    badgeEl.className = 'dose-badge-time';

    if (nextLog.id) {
        actionWrap.innerHTML = `
            <button class="btn btn-primary" onclick="markLogAsTaken(${nextLog.id})">
                <i data-lucide="check-circle-2"></i>
                Mark as Taken
            </button>
        `;
    } else {
        actionWrap.innerHTML = `
            <span style="font-size: 0.85rem; color: var(--text-muted);">
                Scheduled for today
            </span>
        `;
    }
}

function renderAdherenceGauge(data) {
    const percent = Math.min(100, Math.max(0, Math.round(data.adherencePercentage || 0)));
    document.getElementById('adherencePercentNum').textContent = `${percent}%`;

    // SVG Circumference for r=42 is 2 * PI * 42 = ~263.89
    const circleCircumference = 264;
    const offset = circleCircumference - (percent / 100) * circleCircumference;
    const progressBar = document.getElementById('adherenceSvgBar');
    if (progressBar) {
        progressBar.style.strokeDashoffset = offset;
        if (percent < 50) progressBar.style.stroke = 'var(--danger)';
        else if (percent < 80) progressBar.style.stroke = 'var(--warning)';
        else progressBar.style.stroke = 'var(--primary)';
    }

    document.getElementById('adherenceTakenCount').textContent = data.takenDoses || 0;
    document.getElementById('adherenceMissedCount').textContent = data.missedDoses || 0;
    const pendingCount = (data.pendingMedicines ? data.pendingMedicines.length : 0);
    document.getElementById('adherencePendingCount').textContent = pendingCount;
}

function renderTodaySchedule(schedules = [], pending = [], taken = [], missed = []) {
    const listEl = document.getElementById('todayScheduleList');
    if (!schedules || schedules.length === 0) {
        listEl.innerHTML = `
            <div class="empty-state" style="padding: 1.5rem;">
                <i data-lucide="calendar-x" class="empty-state-icon"></i>
                <div class="empty-state-title">No medicines scheduled for today</div>
                <div class="empty-state-desc">You don't have any prescriptions set for today's date.</div>
            </div>
        `;
        return;
    }

    listEl.innerHTML = schedules.map(s => {
        const timeFormatted = Utils.formatTime(s.time);
        const med = s.medicine;
        const medName = med?.name || 'Medicine';
        const doseInfo = `${med?.dose || 1} dose (${med?.dosage || ''}) • ${s.frequency || 'Daily'}`;

        // Determine status
        let statusHtml = '<span class="badge badge-info">Scheduled</span>';
        let actionBtnHtml = '';

        // Check if there is an intake log for this schedule time
        const timePart = s.time ? s.time.substring(0, 5) : '';
        const takenMatch = taken.find(t => t.medicineId === s.medicineId && t.scheduledTime.includes(timePart));
        const missedMatch = missed.find(m => m.medicineId === s.medicineId && m.scheduledTime.includes(timePart));
        const pendingMatch = pending.find(p => p.medicineId === s.medicineId && p.scheduledTime.includes(timePart));

        if (takenMatch) {
            statusHtml = '<span class="badge badge-success"><i data-lucide="check" style="width:12px;height:12px;"></i> Taken</span>';
        } else if (missedMatch) {
            statusHtml = '<span class="badge badge-danger">Missed</span>';
        } else if (pendingMatch) {
            statusHtml = '<span class="badge badge-warning">Due</span>';
            if (pendingMatch.id) {
                actionBtnHtml = `
                    <button class="btn btn-sm btn-primary" onclick="markLogAsTaken(${pendingMatch.id})" title="Mark this dose as taken">
                        <i data-lucide="check" style="width: 12px; height: 12px;"></i>
                        Take
                    </button>
                `;
            }
        }

        return `
            <div class="schedule-item">
                <div class="schedule-item-time">${timeFormatted}</div>
                <div class="schedule-item-med">
                    <div class="schedule-item-title">${Utils.escapeHtml(medName)}</div>
                    <div class="schedule-item-dose">${Utils.escapeHtml(doseInfo)}</div>
                </div>
                <div style="display: flex; align-items: center; gap: 0.5rem;">
                    ${statusHtml}
                    ${actionBtnHtml}
                </div>
            </div>
        `;
    }).join('');
}

function renderInventory(medicines = []) {
    const gridEl = document.getElementById('inventoryGrid');
    if (!medicines || medicines.length === 0) {
        gridEl.innerHTML = `
            <div class="empty-state" style="padding: 1.5rem; grid-column: 1 / -1;">
                <div class="empty-state-title">No medications added</div>
                <a href="medicines.html" class="btn btn-outline btn-sm" style="margin-top: 0.5rem;">+ Add Prescribed Medicine</a>
            </div>
        `;
        return;
    }

    gridEl.innerHTML = medicines.map(m => {
        let stockClass = 'color: var(--success);';
        let badge = '<span class="badge badge-success">OK</span>';
        if (m.stock <= 5 && m.stock > 0) {
            stockClass = 'color: var(--warning);';
            badge = '<span class="badge badge-warning">Low</span>';
        } else if (m.stock === 0) {
            stockClass = 'color: var(--danger);';
            badge = '<span class="badge badge-danger">Empty</span>';
        }

        return `
            <div class="stock-pill-card">
                <div style="display: flex; justify-content: space-between; align-items: center;">
                    <span class="stock-pill-name" title="${Utils.escapeHtml(m.name)}">${Utils.escapeHtml(m.name)}</span>
                    ${badge}
                </div>
                <div class="stock-pill-qty" style="${stockClass}">${m.stock}</div>
                <div style="font-size: 0.7rem; color: var(--text-muted);">${Utils.escapeHtml(m.dosage)}</div>
            </div>
        `;
    }).join('');
}

let connectedCaregiversList = [];

async function loadConnectedCaregivers(patientId) {
    const listEl = document.getElementById('caregiversList');
    const user = Auth.getUser();
    const userEmail = user?.email || 'your email';

    try {
        const caregivers = await api.get(`/Caregiverpatient/patient/${patientId}`);
        connectedCaregiversList = Array.isArray(caregivers) ? caregivers : [];

        if (connectedCaregiversList.length === 0) {
            listEl.innerHTML = `
                <div style="font-size: 0.85rem; color: var(--text-muted); padding: 0.5rem 0;">
                    <div style="color: var(--warning-dark); font-weight: 600; margin-bottom: 0.25rem;">No caregivers linked yet</div>
                    Ask your caregiver to link with your email: <strong>${Utils.escapeHtml(userEmail)}</strong>
                </div>
            `;
            return;
        }

        listEl.innerHTML = connectedCaregiversList.map(c => `
            <div class="caregiver-mini-card">
                <div class="avatar" style="width: 32px; height: 32px; font-size: 0.75rem;">
                    ${Utils.getInitials(c.user?.name || c.relationship || 'CG')}
                </div>
                <div style="flex: 1; overflow: hidden;">
                    <div style="font-size: 0.85rem; font-weight: 600; color: var(--text-main); white-space: nowrap; overflow: hidden; text-overflow: ellipsis;">
                        ${Utils.escapeHtml(c.user?.name || 'Caregiver')}
                    </div>
                    <div style="font-size: 0.75rem; color: var(--text-muted);">
                        ${Utils.escapeHtml(c.relationship || 'Connected')} • ${Utils.escapeHtml(c.user?.email || '')}
                    </div>
                </div>
                <span class="badge badge-neutral"><i data-lucide="shield" style="width: 12px; height: 12px;"></i> Linked</span>
            </div>
        `).join('');

        if (window.lucide) lucide.createIcons({ root: listEl });

    } catch (e) {
        listEl.innerHTML = `<div style="font-size: 0.8rem; color: var(--text-muted);">Could not load caregivers.</div>`;
    }
}

function renderRecentLogs(taken = [], missed = []) {
    const tbody = document.getElementById('recentLogsTbody');
    const combined = [...(taken || []), ...(missed || [])];

    if (combined.length === 0) {
        tbody.innerHTML = '<tr><td colspan="4" style="text-align: center; color: var(--text-muted);">No recent logs recorded.</td></tr>';
        return;
    }

    // Sort descending by scheduledTime
    combined.sort((a, b) => new Date(b.scheduledTime) - new Date(a.scheduledTime));

    tbody.innerHTML = combined.slice(0, 5).map(log => `
        <tr>
            <td style="font-weight: 600;">${Utils.escapeHtml(log.medicine?.name || 'Medicine')}</td>
            <td>${Utils.formatDateTime(log.scheduledTime)}</td>
            <td>${log.takenTime ? Utils.formatDateTime(log.takenTime) : '-'}</td>
            <td>${Utils.getStatusBadge(log.status)}</td>
        </tr>
    `).join('');
}

// Mark pending dose as taken
window.markLogAsTaken = async function(logId) {
    if (!logId) return;
    try {
        await api.put(`/medicinelog/${logId}/taken`);
        Utils.showToast('Dose recorded as taken! Great job keeping up with your health.', 'success');
        await loadPatientDashboard();
    } catch (err) {
        console.error('Error marking log as taken:', err);
    }
};

// Open SOS Dialog
window.openEmergencyModal = function() {
    const warningBox = document.getElementById('sosCaregiverWarning');
    const user = Auth.getUser();
    const userEmail = user?.email || 'your email';

    if (!warningBox) {
        const modalBody = document.querySelector('#sosModal .modal-body');
        if (modalBody) {
            const div = document.createElement('div');
            div.id = 'sosCaregiverWarning';
            div.style.marginBottom = '1rem';
            modalBody.prepend(div);
        }
    }

    const box = document.getElementById('sosCaregiverWarning');
    if (box) {
        if (connectedCaregiversList.length === 0) {
            box.innerHTML = `
                <div style="background: rgba(245, 158, 11, 0.12); border: 1px solid var(--warning); border-radius: var(--radius-md); padding: 0.75rem; font-size: 0.85rem; color: var(--text-main);">
                    <strong>⚠️ Notice:</strong> No caregivers are currently linked to your account.
                    Alerts will be saved in the system, but your caregiver will only receive notifications if they link using your email: <strong>${Utils.escapeHtml(userEmail)}</strong>.
                </div>
            `;
        } else {
            box.innerHTML = `
                <div style="background: rgba(16, 185, 129, 0.1); border: 1px solid var(--success); border-radius: var(--radius-md); padding: 0.75rem; font-size: 0.85rem; color: var(--text-main);">
                    <strong>✓ Linked Caregivers:</strong> ${connectedCaregiversList.length} connected caregiver(s) will be notified immediately upon dispatch.
                </div>
            `;
        }
    }

    Utils.openModal('sosModal');
};

async function handleSosSubmit(e) {
    e.preventDefault();
    const messageInput = document.getElementById('sosMessage');
    const submitBtn = document.getElementById('btnSubmitSos');
    const message = messageInput.value.trim();
    const user = Auth.getUser();

    if (!message) {
        Utils.showToast('Please enter an emergency message.', 'warning');
        return;
    }

    try {
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<span class="spinner" style="width: 16px; height: 16px; border-width: 2px;"></span> Sending Emergency Alert...';

        const res = await api.post('/emergency', { message });
        Utils.closeModal('sosModal');
        
        const count = res.caregiversNotified || 0;
        if (count > 0) {
            Utils.showToast(`🚨 Emergency Alert sent! ${count} linked caregiver(s) notified immediately.`, 'success', 7000);
        } else {
            Utils.showToast(`🚨 Emergency Alert registered! Note: 0 caregivers are linked to ${user?.email || 'your account'}. Please ask your caregiver to link your email in their portal.`, 'warning', 9000);
        }

        await loadPatientDashboard();
    } catch (err) {
        console.error('Failed to trigger emergency alert:', err);
    } finally {
        submitBtn.disabled = false;
        submitBtn.innerHTML = '<i data-lucide="send"></i> Confirm & Trigger SOS';
        if (window.lucide) lucide.createIcons({ root: submitBtn });
    }
}
