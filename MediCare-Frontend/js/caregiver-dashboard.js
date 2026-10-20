// Caregiver Enterprise Dashboard Controller
let previousEmergencyCount = 0;
let isPollingActive = false;
let pollingTimer = null;

document.addEventListener('DOMContentLoaded', async () => {
    const user = Auth.requireAuth(['CAREGIVER']);
    if (!user) return;

    setupUI();
    await loadCaregiverDashboard();

    // Start real-time polling every 8 seconds for instant emergency and schedule updates
    startRealtimePolling();
});

function setupUI() {
    const toggleBtn = document.getElementById('sidebarToggle');
    const sidebar = document.getElementById('sidebar');
    if (toggleBtn && sidebar) {
        toggleBtn.addEventListener('click', () => {
            sidebar.classList.toggle('open');
        });
    }
}

function startRealtimePolling() {
    if (pollingTimer) clearInterval(pollingTimer);
    pollingTimer = setInterval(async () => {
        if (!document.hidden) {
            await loadCaregiverDashboard(true);
        }
    }, 8000);
}

async function loadCaregiverDashboard(isSilent = false) {
    try {
        const [dashData, medicinesData] = await Promise.all([
            api.get('/dashboard/caregiver'),
            api.get('/medicine').catch(() => [])
        ]);

        const patientsList = dashData.patients || [];
        const activeEmergencies = dashData.activeEmergencies || [];
        const todaySchedules = dashData.todaySchedules || [];
        const pendingMedicines = dashData.pendingMedicines || [];
        const missedMedicines = dashData.missedMedicines || [];
        const adherenceList = dashData.adherence || [];
        const allMedicines = Array.isArray(medicinesData) ? medicinesData : [];

        // Detect new active emergency alerts and notify caregiver immediately
        if (activeEmergencies.length > previousEmergencyCount && isSilent) {
            Utils.showToast(`🚨 CRITICAL: New Emergency Alert received from patient!`, 'error', 10000);
            try {
                // If browser supports audio, play a subtle attention beep
                const audioCtx = new (window.AudioContext || window.webkitAudioContext)();
                const osc = audioCtx.createOscillator();
                osc.type = 'triangle';
                osc.frequency.setValueAtTime(440, audioCtx.currentTime);
                osc.connect(audioCtx.destination);
                osc.start();
                osc.stop(audioCtx.currentTime + 0.3);
            } catch (_) {}
        }
        previousEmergencyCount = activeEmergencies.length;

        // Update KPIs
        const kpiPatients = document.getElementById('kpiPatients');
        const kpiEmergencies = document.getElementById('kpiEmergencies');
        const kpiPending = document.getElementById('kpiPending');
        const kpiMissed = document.getElementById('kpiMissed');

        if (kpiPatients) kpiPatients.textContent = patientsList.length;
        if (kpiEmergencies) kpiEmergencies.textContent = activeEmergencies.length;
        if (kpiPending) kpiPending.textContent = pendingMedicines.length;
        if (kpiMissed) kpiMissed.textContent = missedMedicines.length;

        // Unread Notifications Badge
        const navBadge = document.getElementById('navUnreadBadge');
        const sideBadge = document.getElementById('notifSidebarBadge');
        if (dashData.unreadNotifications > 0) {
            if (navBadge) {
                navBadge.textContent = dashData.unreadNotifications;
                navBadge.style.display = 'flex';
            }
            if (sideBadge) {
                sideBadge.textContent = dashData.unreadNotifications;
                sideBadge.style.display = 'inline-block';
            }
        } else {
            if (navBadge) navBadge.style.display = 'none';
            if (sideBadge) sideBadge.style.display = 'none';
        }

        // Active Emergency Sidebar Badge
        const emBadge = document.getElementById('emergencySidebarBadge');
        if (emBadge) {
            if (activeEmergencies.length > 0) {
                emBadge.textContent = activeEmergencies.length;
                emBadge.style.display = 'inline-block';
            } else {
                emBadge.style.display = 'none';
            }
        }

        // Render Active Emergency Alerts Banner
        renderEmergencyBanner(activeEmergencies);

        // Render Patient Adherence Cards
        renderPatientAdherenceCards(adherenceList, patientsList);

        // Render Missed & Pending tables
        renderMissedTable(missedMedicines, patientsList);
        renderPendingTable(pendingMedicines, patientsList);

        // Render Today's Schedules & Medicine Stock tables
        renderTodaySchedulesTable(todaySchedules, patientsList);
        renderMedicineStockTable(allMedicines, patientsList);

        if (window.lucide) {
            lucide.createIcons();
        }

    } catch (err) {
        if (!isSilent) {
            console.error('Failed to load caregiver dashboard:', err);
        }
    }
}

function renderEmergencyBanner(emergencies = []) {
    const container = document.getElementById('emergencyAlertBannerWrap');
    if (!container) return;

    if (!emergencies || emergencies.length === 0) {
        container.innerHTML = '';
        return;
    }

    container.innerHTML = emergencies.map(e => `
        <div class="card animate-fade-in emergency-pulse" style="background: rgba(239, 68, 68, 0.08); border: 2px solid var(--danger); margin-bottom: 1.5rem; display: flex; flex-wrap: wrap; justify-content: space-between; align-items: center; gap: 1rem; padding: 1.25rem 1.5rem;">
            <div>
                <h3 style="color: var(--danger); font-size: 1.15rem; font-weight: 800; display: flex; align-items: center; gap: 0.5rem; margin-bottom: 0.35rem;">
                    <i data-lucide="alert-octagon"></i>
                    ACTIVE EMERGENCY ALERT DISPATCHED
                </h3>
                <p style="font-size: 1rem; color: var(--text-main); margin-bottom: 0.35rem;">
                    Patient: <strong>${Utils.escapeHtml(e.patient?.user?.name || 'Linked Patient')}</strong> — "${Utils.escapeHtml(e.message)}"
                </p>
                <div style="font-size: 0.8rem; color: var(--text-muted);">
                    Dispatched at <strong>${Utils.formatDateTime(e.createdAt)}</strong> • Status: <span class="badge badge-danger">ACTION REQUIRED</span>
                </div>
            </div>

            <div style="display: flex; gap: 0.75rem;">
                <button class="btn btn-sm btn-outline" style="border-color: var(--danger); color: var(--danger); background: var(--card-bg);" onclick="resolveEmergencyAlert(${e.id})">
                    <i data-lucide="check"></i>
                    Mark as Resolved
                </button>
                <a href="emergency.html" class="btn btn-sm btn-danger">
                    <i data-lucide="eye"></i>
                    View Incident Dossier
                </a>
            </div>
        </div>
    `).join('');

    if (window.lucide) lucide.createIcons({ root: container });
}

function renderPatientAdherenceCards(adherenceList = [], patientsList = []) {
    const wrap = document.getElementById('patientAdherenceCardsWrap');
    if (!wrap) return;

    if (!patientsList || patientsList.length === 0) {
        wrap.innerHTML = `
            <div class="empty-state" style="grid-column: 1 / -1; padding: 2.5rem 1.5rem; background: var(--card-bg); border: 2px dashed var(--border-color); border-radius: var(--radius-lg); text-align: center;">
                <i data-lucide="user-x" style="width: 44px; height: 44px; margin: 0 auto 0.75rem; color: var(--warning-dark); opacity: 0.8;"></i>
                <div class="empty-state-title" style="font-size: 1.15rem; font-weight: 700; color: var(--text-main); margin-bottom: 0.4rem;">No Patients Linked Yet</div>
                <div class="empty-state-desc" style="max-width: 520px; margin: 0 auto 1.25rem; font-size: 0.9rem; color: var(--text-muted);">
                    You must link your family member's registered email address to monitor their medicine schedules, stock levels, and receive real-time emergency notifications.
                </div>
                <a href="patients.html" class="btn btn-primary">
                    <i data-lucide="user-plus"></i>
                    Link Patient Account Now
                </a>
            </div>
        `;
        if (window.lucide) lucide.createIcons({ root: wrap });
        return;
    }

    wrap.innerHTML = adherenceList.map(a => {
        const percent = Math.round(Number(a.adherencePercentage) || 0);
        let statusBadge = '<span class="badge badge-success">Good</span>';
        let borderColor = 'var(--border-color)';

        if (percent < 50) {
            statusBadge = '<span class="badge badge-danger">Critical</span>';
            borderColor = 'rgba(239, 68, 68, 0.4)';
        } else if (percent < 80) {
            statusBadge = '<span class="badge badge-warning">Attention</span>';
            borderColor = 'rgba(245, 158, 11, 0.4)';
        }

        const patientRecord = patientsList.find(p => p.patientId === a.patientId);
        const contact = patientRecord?.emergencyContact || 'No contact set';

        return `
            <div class="card animate-fade-in" style="border-top: 4px solid ${borderColor}; cursor: pointer;" onclick="window.location.href='caregiver-patient-details.html?patientId=${a.patientId}'">
                <div style="display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 0.75rem;">
                    <div>
                        <h3 style="font-size: 1.05rem; font-weight: 700; color: var(--text-main); margin-bottom: 0.2rem;">
                            ${Utils.escapeHtml(a.patientName)}
                        </h3>
                        <div style="font-size: 0.75rem; color: var(--text-muted);">Emergency: ${Utils.escapeHtml(contact)}</div>
                    </div>
                    ${statusBadge}
                </div>

                <div style="display: flex; justify-content: space-between; align-items: baseline; margin-bottom: 0.75rem;">
                    <div style="font-size: 1.75rem; font-weight: 800; color: var(--text-main);">${percent}%</div>
                    <div style="font-size: 0.75rem; color: var(--text-muted);">Overall Adherence</div>
                </div>

                <div style="display: flex; justify-content: space-between; font-size: 0.8rem; border-top: 1px solid var(--border-color); padding-top: 0.65rem;">
                    <span style="color: var(--success); font-weight: 600;">Taken: ${a.takenDoses}</span>
                    <span style="color: var(--danger); font-weight: 600;">Missed: ${a.missedDoses}</span>
                    <span style="color: var(--text-muted);">Total: ${a.totalDoses}</span>
                </div>
            </div>
        `;
    }).join('');

    if (window.lucide) lucide.createIcons({ root: wrap });
}

function renderMissedTable(missedLogs = [], patientsList = []) {
    const tbody = document.getElementById('missedMedicinesTbody');
    const badge = document.getElementById('missedCountBadge');
    if (!tbody) return;
    if (badge) badge.textContent = missedLogs.length;

    if (!missedLogs || missedLogs.length === 0) {
        tbody.innerHTML = '<tr><td colspan="3" style="text-align: center; color: var(--text-muted); padding: 1.5rem;">No missed medicine records.</td></tr>';
        return;
    }

    tbody.innerHTML = missedLogs.map(log => {
        let pName = log.patient?.user?.name;
        if (!pName && log.patientId) {
            const p = patientsList.find(x => x.patientId === log.patientId);
            if (p) pName = p.name;
        }

        return `
            <tr>
                <td style="font-weight: 600;">${Utils.escapeHtml(pName || 'Patient')}</td>
                <td>${Utils.escapeHtml(log.medicine?.name || 'Medicine')}</td>
                <td><span class="badge badge-danger">${Utils.formatDateTime(log.scheduledTime)}</span></td>
            </tr>
        `;
    }).join('');
}

function renderPendingTable(pendingLogs = [], patientsList = []) {
    const tbody = document.getElementById('pendingMedicinesTbody');
    const badge = document.getElementById('pendingCountBadge');
    if (!tbody) return;
    if (badge) badge.textContent = pendingLogs.length;

    if (!pendingLogs || pendingLogs.length === 0) {
        tbody.innerHTML = '<tr><td colspan="3" style="text-align: center; color: var(--text-muted); padding: 1.5rem;">No pending doses due today.</td></tr>';
        return;
    }

    tbody.innerHTML = pendingLogs.map(log => {
        let pName = log.patient?.user?.name;
        if (!pName && log.patientId) {
            const p = patientsList.find(x => x.patientId === log.patientId);
            if (p) pName = p.name;
        }

        return `
            <tr>
                <td style="font-weight: 600;">${Utils.escapeHtml(pName || 'Patient')}</td>
                <td>${Utils.escapeHtml(log.medicine?.name || 'Medicine')}</td>
                <td><span class="badge badge-warning">${Utils.formatTime(log.scheduledTime)}</span></td>
            </tr>
        `;
    }).join('');
}

function renderTodaySchedulesTable(todaySchedules = [], patientsList = []) {
    const tbody = document.getElementById('todaySchedulesTbody');
    const badge = document.getElementById('todaySchedulesCountBadge');
    if (!tbody) return;
    if (badge) badge.textContent = todaySchedules.length;

    if (!todaySchedules || todaySchedules.length === 0) {
        tbody.innerHTML = '<tr><td colspan="4" style="text-align: center; color: var(--text-muted); padding: 1.5rem;">No medicine schedules found for today.</td></tr>';
        return;
    }

    tbody.innerHTML = todaySchedules.map(s => {
        let pName = s.medicine?.patient?.user?.name;
        if (!pName && s.medicine?.patientId) {
            const p = patientsList.find(x => x.patientId === s.medicine.patientId);
            if (p) pName = p.name;
        }

        return `
            <tr>
                <td style="font-weight: 600;">${Utils.escapeHtml(pName || 'Patient')}</td>
                <td>
                    <div style="font-weight: 600; color: var(--text-main);">${Utils.escapeHtml(s.medicine?.name || 'Medicine')}</div>
                    <div style="font-size: 0.75rem; color: var(--text-muted);">${Utils.escapeHtml(s.medicine?.dosage || '')}</div>
                </td>
                <td><span class="badge badge-primary">${Utils.formatTime(s.time)}</span></td>
                <td><span class="badge badge-neutral">${Utils.escapeHtml(s.frequency || 'Scheduled')}</span></td>
            </tr>
        `;
    }).join('');
}

function renderMedicineStockTable(medicines = [], patientsList = []) {
    const tbody = document.getElementById('patientStockTbody');
    if (!tbody) return;

    if (!medicines || medicines.length === 0) {
        tbody.innerHTML = '<tr><td colspan="4" style="text-align: center; color: var(--text-muted); padding: 1.5rem;">No medicine prescriptions or stock found.</td></tr>';
        return;
    }

    tbody.innerHTML = medicines.map(m => {
        let pName = m.patient?.user?.name;
        if (!pName && m.patientId) {
            const p = patientsList.find(x => x.patientId === m.patientId);
            if (p) pName = p.name;
        }

        let badge = '<span class="badge badge-success">In Stock</span>';
        if (m.stock <= 5 && m.stock > 0) {
            badge = `<span class="badge badge-warning">Low Stock (${m.stock})</span>`;
        } else if (m.stock === 0) {
            badge = '<span class="badge badge-danger">Out of Stock</span>';
        }

        return `
            <tr>
                <td style="font-weight: 600;">${Utils.escapeHtml(pName || 'Patient')}</td>
                <td>
                    <div style="font-weight: 600; color: var(--text-main);">${Utils.escapeHtml(m.name)}</div>
                    <div style="font-size: 0.75rem; color: var(--text-muted);">${Utils.escapeHtml(m.dosage)} (${m.dose || 1} dose/intake)</div>
                </td>
                <td style="font-weight: 700; font-size: 0.95rem;">${m.stock} units</td>
                <td>${badge}</td>
            </tr>
        `;
    }).join('');
}

window.resolveEmergencyAlert = async function(emergencyId) {
    const confirmed = await Utils.confirmAction('Are you sure you want to mark this emergency alert as resolved?', 'Resolve Emergency');
    if (!confirmed) return;

    try {
        await api.put(`/emergency/${emergencyId}/resolve`);
        Utils.showToast('Emergency alert resolved successfully.', 'success');
        await loadCaregiverDashboard();
    } catch (err) {
        console.error('Failed to resolve emergency:', err);
    }
};
