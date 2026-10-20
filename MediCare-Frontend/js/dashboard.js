// MediCare Central Dashboard Logic
document.addEventListener('DOMContentLoaded', async () => {
    const user = Auth.requireAuth(['PATIENT', 'CAREGIVER', 'ADMIN']);
    if (!user) return;

    Auth.setupNavbar();
    Auth.renderSidebar('dashboard');

    const role = Auth.getRole();
    const greetingEl = document.getElementById('navbarGreeting');
    if (greetingEl) {
        greetingEl.textContent = `${Utils.getGreeting(user.name || user.email)}`;
    }

    try {
        if (role === 'PATIENT') {
            await loadPatientDashboard();
        } else if (role === 'CAREGIVER') {
            await loadCaregiverDashboard();
        } else if (role === 'ADMIN') {
            await loadAdminDashboard();
        }
    } catch (err) {
        console.error('Failed to load dashboard:', err);
    }
});

// ==========================================
// 1. PATIENT DASHBOARD
// ==========================================
async function loadPatientDashboard() {
    const data = await api.get('/dashboard/patient');
    if (!data) return;

    const patient = data.patient || {};
    document.getElementById('dashTitle').textContent = `Welcome back, ${patient.name || 'Patient'}!`;
    document.getElementById('dashSubtitle').textContent = `Today is ${new Date().toLocaleDateString('en-US', { weekday: 'long', month: 'short', day: 'numeric', year: 'numeric' })}. Here is your daily medication status.`;

    // Actions
    const actionsEl = document.getElementById('dashActions');
    actionsEl.innerHTML = `
        <button class="btn btn-outline" onclick="window.location.href='medicines.html'">
            <i data-lucide="pill"></i> My Medicines
        </button>
        <button class="btn btn-danger" onclick="openSosModal()">
            <i data-lucide="phone-call"></i> Emergency Alert
        </button>
    `;

    // Emergency Alert Banner
    const emergencyBanner = document.getElementById('emergencyAlertBanner');
    if (data.activeEmergencies && data.activeEmergencies.length > 0) {
        emergencyBanner.className = 'alert-banner alert-banner-danger';
        emergencyBanner.style.display = 'flex';
        emergencyBanner.innerHTML = `
            <div>
                <strong>🚨 Active Distress Alert:</strong> You have ${data.activeEmergencies.length} unresolved emergency alert(s). Caregivers have been notified.
            </div>
            <a href="emergency.html" class="btn btn-sm btn-danger">View Alerts</a>
        `;
    } else {
        emergencyBanner.style.display = 'none';
    }

    // Low Stock Banner
    const lowStockBanner = document.getElementById('lowStockBanner');
    if (data.lowStockMedicines && data.lowStockMedicines.length > 0) {
        lowStockBanner.className = 'alert-banner alert-banner-warning';
        lowStockBanner.style.display = 'flex';
        const medNames = data.lowStockMedicines.map(m => `${m.name} (${m.stock} left)`).join(', ');
        lowStockBanner.innerHTML = `
            <div>
                <strong>⚠️ Low Medication Stock:</strong> Please refill soon: ${Utils.escapeHtml(medNames)}.
            </div>
            <a href="medicines.html" class="btn btn-sm btn-outline">Check Stock</a>
        `;
    } else {
        lowStockBanner.style.display = 'none';
    }

    // Notification Badge
    if (data.unreadNotifications > 0) {
        const badge = document.getElementById('navBadge');
        if (badge) {
            badge.textContent = data.unreadNotifications;
            badge.style.display = 'flex';
        }
    }

    // KPI Cards
    const kpiGrid = document.getElementById('kpiGrid');
    kpiGrid.innerHTML = `
        <div class="card kpi-card">
            <div class="kpi-info">
                <h3>Total Medicines</h3>
                <div class="kpi-value">${data.totalMedicines || 0}</div>
                <div class="kpi-subtext">Active Prescriptions</div>
            </div>
            <div class="kpi-icon" style="background: #eff6ff; color: #1d4ed8;">
                <i data-lucide="pill"></i>
            </div>
        </div>

        <div class="card kpi-card">
            <div class="kpi-info">
                <h3>Pending Today</h3>
                <div class="kpi-value" style="color: #d97706;">${(data.pendingMedicines || []).length}</div>
                <div class="kpi-subtext">Doses Awaiting Intake</div>
            </div>
            <div class="kpi-icon" style="background: #fffbeb; color: #d97706;">
                <i data-lucide="clock"></i>
            </div>
        </div>

        <div class="card kpi-card">
            <div class="kpi-info">
                <h3>Taken Doses</h3>
                <div class="kpi-value" style="color: #10b981;">${data.takenDoses || 0}</div>
                <div class="kpi-subtext">Successfully Recorded</div>
            </div>
            <div class="kpi-icon" style="background: #ecfdf5; color: #10b981;">
                <i data-lucide="check-circle-2"></i>
            </div>
        </div>

        <div class="card kpi-card">
            <div class="kpi-info">
                <h3>Adherence Rate</h3>
                <div class="kpi-value">${data.adherencePercentage || 0}%</div>
                <div class="kpi-subtext">${data.adherencePercentage >= 80 ? 'Good Compliance' : 'Needs Attention'}</div>
            </div>
            <div class="kpi-icon" style="background: #f0fdfa; color: #0d9488;">
                <i data-lucide="trending-up"></i>
            </div>
        </div>
    `;

    // Role Section (Today's Pending Doses + Recent Logs)
    const section = document.getElementById('roleDashboardSection');
    const todayLogs = data.pendingMedicines || [];
    const recentLogs = data.takenMedicines || [];

    let pendingRowsHtml = '';
    if (todayLogs.length === 0) {
        pendingRowsHtml = `<tr><td colspan="6" class="empty-state">No pending doses due for today! Great job staying on track.</td></tr>`;
    } else {
        todayLogs.forEach(item => {
            const med = item.medicine || {};
            const timeStr = Utils.formatTime(item.scheduledTime);
            pendingRowsHtml += `
                <tr>
                    <td><strong>${timeStr}</strong></td>
                    <td><strong>${Utils.escapeHtml(med.name || 'Medicine')}</strong></td>
                    <td>${Utils.escapeHtml(med.dosage || '-')}</td>
                    <td>${med.dose || 1} dose</td>
                    <td><span class="badge badge-warning">Pending</span></td>
                    <td>
                        ${item.id ? `
                            <button class="btn btn-sm btn-success" onclick="markDoseTaken(${item.id})">
                                <i data-lucide="check"></i> Mark as Taken
                            </button>
                        ` : `
                            <span style="font-size: 0.75rem; color: #64748b;">Scheduled</span>
                        `}
                    </td>
                </tr>
            `;
        });
    }

    let takenRowsHtml = '';
    if (recentLogs.length === 0) {
        takenRowsHtml = `<tr><td colspan="4" class="empty-state">No intake history recorded yet.</td></tr>`;
    } else {
        recentLogs.slice(0, 5).forEach(item => {
            const med = item.medicine || {};
            takenRowsHtml += `
                <tr>
                    <td><strong>${Utils.escapeHtml(med.name || 'Medicine')}</strong></td>
                    <td>${Utils.formatDateTime(item.scheduledTime)}</td>
                    <td><span class="badge badge-success">Taken</span></td>
                    <td>${Utils.formatDateTime(item.takenTime)}</td>
                </tr>
            `;
        });
    }

    section.innerHTML = `
        <div style="display: grid; grid-template-columns: 1fr; gap: 1.5rem;">
            <!-- Today's Due Doses -->
            <div class="card">
                <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem;">
                    <div>
                        <h3 style="font-size: 1.1rem; font-weight: 700; color: #0f172a;">Today's Prescribed Doses</h3>
                        <p style="font-size: 0.8rem; color: #64748b;">Medicines scheduled for intake today</p>
                    </div>
                    <a href="schedules.html" class="btn btn-sm btn-outline">View All Schedules</a>
                </div>
                <div class="table-container">
                    <table>
                        <thead>
                            <tr>
                                <th>Dose Time</th>
                                <th>Medicine</th>
                                <th>Dosage</th>
                                <th>Quantity</th>
                                <th>Status</th>
                                <th>Action</th>
                            </tr>
                        </thead>
                        <tbody>${pendingRowsHtml}</tbody>
                    </table>
                </div>
            </div>

            <!-- Recent Intake History -->
            <div class="card">
                <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem;">
                    <div>
                        <h3 style="font-size: 1.1rem; font-weight: 700; color: #0f172a;">Recent Medication Logs</h3>
                        <p style="font-size: 0.8rem; color: #64748b;">Your most recent confirmed medication intakes</p>
                    </div>
                    <a href="medicine-logs.html" class="btn btn-sm btn-outline">Full History</a>
                </div>
                <div class="table-container">
                    <table>
                        <thead>
                            <tr>
                                <th>Medicine</th>
                                <th>Scheduled Time</th>
                                <th>Status</th>
                                <th>Taken Time</th>
                            </tr>
                        </thead>
                        <tbody>${takenRowsHtml}</tbody>
                    </table>
                </div>
            </div>
        </div>
    `;

    if (window.lucide) {
        lucide.createIcons();
    }
}

// Mark dose as taken directly from dashboard
async function markDoseTaken(logId) {
    if (!logId) return;
    try {
        await api.put(`/medicinelog/${logId}/taken`);
        Utils.showToast('Medicine marked as taken successfully!', 'success');
        await loadPatientDashboard();
    } catch (err) {
        console.error('Error marking as taken:', err);
    }
}

// ==========================================
// 2. CAREGIVER DASHBOARD
// ==========================================
async function loadCaregiverDashboard() {
    const data = await api.get('/dashboard/caregiver');
    if (!data) return;

    const caregiver = data.caregiver || {};
    document.getElementById('dashTitle').textContent = `Caregiver Portal — ${caregiver.name || 'Caregiver'}`;
    document.getElementById('dashSubtitle').textContent = `Supervising ${(data.patients || []).length} connected patient(s). Monitoring schedules, compliance, and safety alerts.`;

    // Actions
    const actionsEl = document.getElementById('dashActions');
    actionsEl.innerHTML = `
        <button class="btn btn-primary" onclick="window.location.href='caregiver-patient.html'">
            <i data-lucide="user-plus"></i> Link New Patient
        </button>
    `;

    // Active Emergency Alerts
    const emergencyBanner = document.getElementById('emergencyAlertBanner');
    if (data.activeEmergencies && data.activeEmergencies.length > 0) {
        emergencyBanner.className = 'alert-banner alert-banner-danger';
        emergencyBanner.style.display = 'flex';
        const firstAlert = data.activeEmergencies[0];
        const pName = firstAlert.patient && firstAlert.patient.user ? firstAlert.patient.user.name : 'Patient';
        emergencyBanner.innerHTML = `
            <div>
                <strong>🚨 Urgent Emergency Alert!</strong> Patient <strong>${Utils.escapeHtml(pName)}</strong> triggered an alert: "${Utils.escapeHtml(firstAlert.message)}"
            </div>
            <div style="display: flex; gap: 0.5rem;">
                <button class="btn btn-sm btn-danger" onclick="resolveEmergencyAlert(${firstAlert.id})">Resolve Alert</button>
                <a href="emergency.html" class="btn btn-sm btn-outline">View All</a>
            </div>
        `;
    } else {
        emergencyBanner.style.display = 'none';
    }

    // KPI Cards
    const kpiGrid = document.getElementById('kpiGrid');
    kpiGrid.innerHTML = `
        <div class="card kpi-card">
            <div class="kpi-info">
                <h3>Linked Patients</h3>
                <div class="kpi-value">${(data.patients || []).length}</div>
                <div class="kpi-subtext">Under Supervision</div>
            </div>
            <div class="kpi-icon" style="background: #eff6ff; color: #1d4ed8;">
                <i data-lucide="users"></i>
            </div>
        </div>

        <div class="card kpi-card">
            <div class="kpi-info">
                <h3>Active Emergencies</h3>
                <div class="kpi-value" style="color: ${(data.activeEmergencies || []).length > 0 ? '#ef4444' : '#10b981'};">
                    ${(data.activeEmergencies || []).length}
                </div>
                <div class="kpi-subtext">${(data.activeEmergencies || []).length > 0 ? 'Urgent Action Required' : 'All Patients Safe'}</div>
            </div>
            <div class="kpi-icon" style="background: #fef2f2; color: #ef4444;">
                <i data-lucide="alert-octagon"></i>
            </div>
        </div>

        <div class="card kpi-card">
            <div class="kpi-info">
                <h3>Missed Doses</h3>
                <div class="kpi-value" style="color: #dc2626;">${(data.missedMedicines || []).length}</div>
                <div class="kpi-subtext">Recent Missed Alerts</div>
            </div>
            <div class="kpi-icon" style="background: #fef2f2; color: #dc2626;">
                <i data-lucide="alert-triangle"></i>
            </div>
        </div>

        <div class="card kpi-card">
            <div class="kpi-info">
                <h3>Pending Doses</h3>
                <div class="kpi-value" style="color: #d97706;">${(data.pendingMedicines || []).length}</div>
                <div class="kpi-subtext">Today's Remaining</div>
            </div>
            <div class="kpi-icon" style="background: #fffbeb; color: #d97706;">
                <i data-lucide="clock"></i>
            </div>
        </div>
    `;

    // Section (Linked Patients Table + Today's Pending/Missed)
    const section = document.getElementById('roleDashboardSection');
    const patients = data.patients || [];
    const adherenceList = data.adherence || [];

    let patientRows = '';
    if (patients.length === 0) {
        patientRows = `<tr><td colspan="5" class="empty-state">No patients linked to your account yet. Click "Link New Patient" above to connect family members.</td></tr>`;
    } else {
        patients.forEach(p => {
            const adh = adherenceList.find(a => a.patientId === p.patientId) || {};
            const pct = adh.adherencePercentage !== undefined ? adh.adherencePercentage : 0;
            patientRows += `
                <tr>
                    <td><strong>${Utils.escapeHtml(p.name || 'Unknown')}</strong></td>
                    <td>${Utils.escapeHtml(p.email || '-')}</td>
                    <td>${Utils.escapeHtml(p.emergencyContact || 'Not provided')}</td>
                    <td>${Utils.getAdherenceBadge(pct)}</td>
                    <td>
                        <a href="caregiver-patient.html" class="btn btn-sm btn-outline">Details</a>
                    </td>
                </tr>
            `;
        });
    }

    let missedRows = '';
    const missedList = data.missedMedicines || [];
    if (missedList.length === 0) {
        missedRows = `<tr><td colspan="4" class="empty-state">No recent missed doses recorded across linked patients.</td></tr>`;
    } else {
        missedList.slice(0, 5).forEach(m => {
            const pName = m.patient && m.patient.user ? m.patient.user.name : 'Patient';
            const medName = m.medicine ? m.medicine.name : 'Medicine';
            missedRows += `
                <tr>
                    <td><strong>${Utils.escapeHtml(pName)}</strong></td>
                    <td>${Utils.escapeHtml(medName)}</td>
                    <td>${Utils.formatDateTime(m.scheduledTime)}</td>
                    <td><span class="badge badge-danger">Missed</span></td>
                </tr>
            `;
        });
    }

    section.innerHTML = `
        <div style="display: grid; grid-template-columns: 1fr; gap: 1.5rem;">
            <!-- Linked Patients -->
            <div class="card">
                <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem;">
                    <div>
                        <h3 style="font-size: 1.1rem; font-weight: 700; color: #0f172a;">Supervised Patients</h3>
                        <p style="font-size: 0.8rem; color: #64748b;">Patients currently under your care</p>
                    </div>
                    <a href="caregiver-patient.html" class="btn btn-sm btn-outline">Manage Relationships</a>
                </div>
                <div class="table-container">
                    <table>
                        <thead>
                            <tr>
                                <th>Patient Name</th>
                                <th>Email</th>
                                <th>Emergency Contact</th>
                                <th>Adherence Rate</th>
                                <th>Action</th>
                            </tr>
                        </thead>
                        <tbody>${patientRows}</tbody>
                    </table>
                </div>
            </div>

            <!-- Missed Doses Log -->
            <div class="card">
                <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem;">
                    <div>
                        <h3 style="font-size: 1.1rem; font-weight: 700; color: #0f172a;">Recent Missed Doses</h3>
                        <p style="font-size: 0.8rem; color: #64748b;">Alerts for doses not taken on schedule</p>
                    </div>
                    <a href="medicine-logs.html" class="btn btn-sm btn-outline">View All Intake Logs</a>
                </div>
                <div class="table-container">
                    <table>
                        <thead>
                            <tr>
                                <th>Patient</th>
                                <th>Medicine</th>
                                <th>Scheduled Time</th>
                                <th>Status</th>
                            </tr>
                        </thead>
                        <tbody>${missedRows}</tbody>
                    </table>
                </div>
            </div>
        </div>
    `;

    if (window.lucide) {
        lucide.createIcons();
    }
}

// Resolve emergency
async function resolveEmergencyAlert(id) {
    if (!id || !confirm('Are you sure you want to mark this emergency alert as resolved?')) return;
    try {
        await api.put(`/emergency/${id}/resolve`);
        Utils.showToast('Emergency alert resolved.', 'success');
        const role = Auth.getRole();
        if (role === 'CAREGIVER') await loadCaregiverDashboard();
        else if (role === 'ADMIN') await loadAdminDashboard();
        else await loadPatientDashboard();
    } catch (err) {
        console.error('Error resolving emergency:', err);
    }
}

// ==========================================
// 3. ADMIN DASHBOARD
// ==========================================
async function loadAdminDashboard() {
    const data = await api.get('/dashboard/admin');
    if (!data) return;

    document.getElementById('dashTitle').textContent = 'Admin Overview & System Telemetry';
    document.getElementById('dashSubtitle').textContent = `System health status, database statistics, user accounts, and medicine inventories.`;

    const actionsEl = document.getElementById('dashActions');
    actionsEl.innerHTML = `
        <button class="btn btn-outline" onclick="window.location.href='medicines.html'">
            <i data-lucide="pill"></i> All Prescriptions
        </button>
        <button class="btn btn-primary" onclick="window.location.href='notifications.html'">
            <i data-lucide="send"></i> System Broadcast
        </button>
    `;

    // KPI Cards
    const kpiGrid = document.getElementById('kpiGrid');
    kpiGrid.innerHTML = `
        <div class="card kpi-card">
            <div class="kpi-info">
                <h3>Total Users</h3>
                <div class="kpi-value">${data.totalUsers || 0}</div>
                <div class="kpi-subtext">${data.totalPatients || 0} Patients | ${data.totalCaregivers || 0} Caregivers</div>
            </div>
            <div class="kpi-icon" style="background: #eff6ff; color: #1d4ed8;">
                <i data-lucide="users"></i>
            </div>
        </div>

        <div class="card kpi-card">
            <div class="kpi-info">
                <h3>Total Medicines</h3>
                <div class="kpi-value">${data.totalMedicines || 0}</div>
                <div class="kpi-subtext">Prescriptions in System</div>
            </div>
            <div class="kpi-icon" style="background: #f0fdfa; color: #0d9488;">
                <i data-lucide="pill"></i>
            </div>
        </div>

        <div class="card kpi-card">
            <div class="kpi-info">
                <h3>System Adherence</h3>
                <div class="kpi-value">${data.overallAdherencePercentage || 0}%</div>
                <div class="kpi-subtext">${data.totalTakenDoses || 0} Taken / ${data.totalMissedDoses || 0} Missed</div>
            </div>
            <div class="kpi-icon" style="background: #ecfdf5; color: #10b981;">
                <i data-lucide="activity"></i>
            </div>
        </div>

        <div class="card kpi-card">
            <div class="kpi-info">
                <h3>Active Emergencies</h3>
                <div class="kpi-value" style="color: ${(data.activeEmergencyAlerts || 0) > 0 ? '#dc2626' : '#10b981'};">
                    ${data.activeEmergencyAlerts || 0}
                </div>
                <div class="kpi-subtext">${data.totalEmergencyAlerts || 0} Total Recorded</div>
            </div>
            <div class="kpi-icon" style="background: #fef2f2; color: #dc2626;">
                <i data-lucide="alert-octagon"></i>
            </div>
        </div>
    `;

    const section = document.getElementById('roleDashboardSection');
    const emergencies = data.activeEmergencies || [];
    const lowStock = data.lowStockMedicines || [];

    let emergencyRows = '';
    if (emergencies.length === 0) {
        emergencyRows = `<tr><td colspan="5" class="empty-state">No active emergencies currently reported. System is nominal.</td></tr>`;
    } else {
        emergencies.forEach(e => {
            const pName = e.patient && e.patient.user ? e.patient.user.name : `Patient #${e.patientId}`;
            emergencyRows += `
                <tr>
                    <td><strong>#${e.id}</strong></td>
                    <td>${Utils.escapeHtml(pName)}</td>
                    <td>${Utils.escapeHtml(e.message)}</td>
                    <td>${Utils.formatDateTime(e.createdAt)}</td>
                    <td>
                        <button class="btn btn-sm btn-danger" onclick="resolveEmergencyAlert(${e.id})">Resolve</button>
                    </td>
                </tr>
            `;
        });
    }

    let lowStockRows = '';
    if (lowStock.length === 0) {
        lowStockRows = `<tr><td colspan="4" class="empty-state">All prescribed medicines have adequate inventory levels (> 5 units).</td></tr>`;
    } else {
        lowStock.forEach(m => {
            const pName = m.patient && m.patient.user ? m.patient.user.name : `Patient #${m.patientId}`;
            lowStockRows += `
                <tr>
                    <td><strong>${Utils.escapeHtml(m.name)}</strong></td>
                    <td>${Utils.escapeHtml(pName)}</td>
                    <td>${Utils.escapeHtml(m.dosage)}</td>
                    <td>${Utils.getStockBadge(m.stock)}</td>
                </tr>
            `;
        });
    }

    section.innerHTML = `
        <div style="display: grid; grid-template-columns: 1fr; gap: 1.5rem;">
            <!-- Active Emergencies -->
            <div class="card">
                <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem;">
                    <div>
                        <h3 style="font-size: 1.1rem; font-weight: 700; color: #dc2626;">Active Emergency Alerts</h3>
                        <p style="font-size: 0.8rem; color: #64748b;">Requires immediate monitoring and intervention</p>
                    </div>
                    <a href="emergency.html" class="btn btn-sm btn-outline">View All Incidents</a>
                </div>
                <div class="table-container">
                    <table>
                        <thead>
                            <tr>
                                <th>ID</th>
                                <th>Patient</th>
                                <th>Message</th>
                                <th>Reported Time</th>
                                <th>Action</th>
                            </tr>
                        </thead>
                        <tbody>${emergencyRows}</tbody>
                    </table>
                </div>
            </div>

            <!-- Low Stock Medicines -->
            <div class="card">
                <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem;">
                    <div>
                        <h3 style="font-size: 1.1rem; font-weight: 700; color: #0f172a;">Low Stock Inventory Alerts</h3>
                        <p style="font-size: 0.8rem; color: #64748b;">Medicines with 5 or fewer remaining units</p>
                    </div>
                    <a href="medicines.html" class="btn btn-sm btn-outline">All Inventory</a>
                </div>
                <div class="table-container">
                    <table>
                        <thead>
                            <tr>
                                <th>Medicine</th>
                                <th>Patient</th>
                                <th>Dosage</th>
                                <th>Stock Status</th>
                            </tr>
                        </thead>
                        <tbody>${lowStockRows}</tbody>
                    </table>
                </div>
            </div>
        </div>
    `;

    if (window.lucide) {
        lucide.createIcons();
    }
}

// SOS Modal functions
function openSosModal() {
    document.getElementById('sosModal').style.display = 'flex';
}

function closeSosModal() {
    document.getElementById('sosModal').style.display = 'none';
}

async function submitSosAlert() {
    const msg = document.getElementById('sosMessage').value.trim();
    if (!msg) {
        Utils.showToast('Please enter an emergency description.', 'warning');
        return;
    }

    const btn = document.getElementById('btnSendSos');
    try {
        btn.disabled = true;
        btn.innerHTML = 'Sending alert...';

        await api.post('/emergency', { message: msg });
        Utils.showToast('Emergency alert triggered successfully! Caregivers have been notified.', 'success');
        closeSosModal();
        await loadPatientDashboard();
    } catch (err) {
        console.error('Error triggering SOS:', err);
    } finally {
        btn.disabled = false;
        btn.innerHTML = '<i data-lucide="phone-call"></i> Send Emergency Alert';
        if (window.lucide) lucide.createIcons();
    }
}
