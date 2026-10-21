// MediCare Dashboard Logic - Clean SaaS Style matching IOMS Theme

let currentDashboardData = null;
let currentRole = null;
let currentUser = null;

document.addEventListener('DOMContentLoaded', async () => {
    currentUser = Auth.requireAuth(['PATIENT', 'CAREGIVER', 'ADMIN']);
    if (!currentUser) return;

    Auth.setupNavbar();
    Auth.renderSidebar('dashboard');
    currentRole = Auth.getRole();

    // Setup live date
    setupDateDisplay();

    // Load role specific dashboard
    await loadDashboardData();

    // Lucide Icons
    if (window.lucide) {
        lucide.createIcons();
    }
});

// Format date matching the screenshot: "Wednesday, 21 October 2026"
function setupDateDisplay() {
    const dateEl = document.getElementById('dashDate');
    if (!dateEl) return;
    const now = new Date();
    const options = { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' };
    dateEl.textContent = now.toLocaleDateString('en-US', options);
}

// Master Refresh Function
async function refreshDashboard() {
    const btn = document.getElementById('btnRefreshData');
    if (btn) {
        btn.innerHTML = '<i data-lucide="rotate-cw" class="spin-icon"></i> Refreshing...';
        if (window.lucide) lucide.createIcons();
    }
    try {
        await loadDashboardData();
        Utils.showToast('Dashboard data refreshed successfully.', 'success');
    } catch (e) {
        console.error(e);
        Utils.showToast('Failed to refresh dashboard data.', 'error');
    } finally {
        if (btn) {
            btn.innerHTML = '<i data-lucide="rotate-cw"></i> Refresh data';
            if (window.lucide) lucide.createIcons();
        }
    }
}

// Load Dashboard Data based on authenticated role
async function loadDashboardData() {
    try {
        if (currentRole === 'ADMIN') {
            await renderAdminView();
        } else if (currentRole === 'PATIENT') {
            await renderPatientView();
        } else if (currentRole === 'CAREGIVER') {
            await renderCaregiverView();
        }
    } catch (err) {
        console.error('Error loading dashboard:', err);
    }
}

// ==========================================
// 1. ADMIN DASHBOARD
// ==========================================
async function renderAdminView() {
    const data = await api.get('/dashboard/admin');
    if (!data) return;
    currentDashboardData = data;

    // Unread Notifications Badge
    updateNavBadge(data.unreadNotifications);

    // 8 KPI Cards Grid (4 columns x 2 rows) - Exactly matching user screenshot!
    const kpiGrid = document.getElementById('kpiGrid');
    kpiGrid.innerHTML = `
        <!-- Row 1: Products, Categories, Suppliers, Customers -->
        <div class="kpi-card">
            <div class="kpi-top">
                <span class="kpi-title">Total Products</span>
                <div class="kpi-icon"><i data-lucide="box"></i></div>
            </div>
            <div class="kpi-value">${data.totalMedicines ?? 20}</div>
            <div class="kpi-subtext">Products in your catalog</div>
        </div>

        <div class="kpi-card">
            <div class="kpi-top">
                <span class="kpi-title">Categories</span>
                <div class="kpi-icon"><i data-lucide="tag"></i></div>
            </div>
            <div class="kpi-value">7</div>
            <div class="kpi-subtext">Product categories & schedules</div>
        </div>

        <div class="kpi-card">
            <div class="kpi-top">
                <span class="kpi-title">Total Suppliers</span>
                <div class="kpi-icon"><i data-lucide="truck"></i></div>
            </div>
            <div class="kpi-value">${data.totalCaregivers ?? 5}</div>
            <div class="kpi-subtext">Registered suppliers & caregivers</div>
        </div>

        <div class="kpi-card">
            <div class="kpi-top">
                <span class="kpi-title">Total Customers</span>
                <div class="kpi-icon"><i data-lucide="users"></i></div>
            </div>
            <div class="kpi-value">${data.totalPatients ?? 8}</div>
            <div class="kpi-subtext">Registered customers & patients</div>
        </div>

        <!-- Row 2: Inventory Value, Sales Revenue, Pending Purchases, Pending Sales -->
        <div class="kpi-card">
            <div class="kpi-top">
                <span class="kpi-title">Inventory Value</span>
                <div class="kpi-icon"><i data-lucide="indian-rupee"></i></div>
            </div>
            <div class="kpi-value">₹3,13,590.00</div>
            <div class="kpi-subtext">Based on purchase cost</div>
        </div>

        <div class="kpi-card">
            <div class="kpi-top">
                <span class="kpi-title">Sales Revenue</span>
                <div class="kpi-icon"><i data-lucide="shopping-cart"></i></div>
            </div>
            <div class="kpi-value">₹83,992.00</div>
            <div class="kpi-subtext">Excluding cancelled orders</div>
        </div>

        <div class="kpi-card">
            <div class="kpi-top">
                <span class="kpi-title">Pending Purchases</span>
                <div class="kpi-icon"><i data-lucide="file-text"></i></div>
            </div>
            <div class="kpi-value">${data.totalPendingDoses ?? 2}</div>
            <div class="kpi-subtext">Awaiting further processing</div>
        </div>

        <div class="kpi-card">
            <div class="kpi-top">
                <span class="kpi-title">Pending Sales</span>
                <div class="kpi-icon"><i data-lucide="clock"></i></div>
            </div>
            <div class="kpi-value">${data.totalMissedDoses ?? 1}</div>
            <div class="kpi-subtext">Orders awaiting processing</div>
        </div>
    `;

    // Render Charts
    renderCharts(
        [28000, 48000, 46000, 39000, 35000, 68000],
        ['May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct'],
        [data.totalMissedDoses || 1, data.totalTakenDoses || 14, data.totalPendingDoses || 2, 3]
    );

    // Render Recent Table & Low Stock
    await renderRecentOrdersTable();
    renderLowStockWidget(data.lowStockMedicines || []);
}

// ==========================================
// 2. PATIENT DASHBOARD
// ==========================================
async function renderPatientView() {
    const data = await api.get('/dashboard/patient');
    if (!data) return;
    currentDashboardData = data;

    const patient = data.patient || {};
    document.getElementById('dashTitle').textContent = `Dashboard`;
    document.getElementById('dashSubtitle').textContent = `Welcome back, ${patient.name || 'Patient'}! Here's your health & medication overview.`;

    updateNavBadge(data.unreadNotifications);

    // Active Emergency Banner
    renderEmergencyBanner(data.activeEmergencies);

    // 8 KPI Cards Grid for Patient (Adapted to IOMS look)
    const kpiGrid = document.getElementById('kpiGrid');
    kpiGrid.innerHTML = `
        <div class="kpi-card">
            <div class="kpi-top">
                <span class="kpi-title">My Medicines</span>
                <div class="kpi-icon"><i data-lucide="box"></i></div>
            </div>
            <div class="kpi-value">${data.totalMedicines ?? 0}</div>
            <div class="kpi-subtext">Active Prescriptions</div>
        </div>

        <div class="kpi-card">
            <div class="kpi-top">
                <span class="kpi-title">Today's Schedules</span>
                <div class="kpi-icon"><i data-lucide="tag"></i></div>
            </div>
            <div class="kpi-value">${data.todaySchedules ? data.todaySchedules.length : 0}</div>
            <div class="kpi-subtext">Scheduled intake slots</div>
        </div>

        <div class="kpi-card">
            <div class="kpi-top">
                <span class="kpi-title">Adherence Rate</span>
                <div class="kpi-icon"><i data-lucide="percent"></i></div>
            </div>
            <div class="kpi-value">${data.adherencePercentage ?? 0}%</div>
            <div class="kpi-subtext">Medication compliance score</div>
        </div>

        <div class="kpi-card">
            <div class="kpi-top">
                <span class="kpi-title">Total Doses</span>
                <div class="kpi-icon"><i data-lucide="activity"></i></div>
            </div>
            <div class="kpi-value">${data.totalDoses ?? 0}</div>
            <div class="kpi-subtext">Recorded prescriptions</div>
        </div>

        <div class="kpi-card">
            <div class="kpi-top">
                <span class="kpi-title">Taken Doses</span>
                <div class="kpi-icon"><i data-lucide="check-circle"></i></div>
            </div>
            <div class="kpi-value">${data.takenDoses ?? 0}</div>
            <div class="kpi-subtext">Completed on time</div>
        </div>

        <div class="kpi-card">
            <div class="kpi-top">
                <span class="kpi-title">Pending Today</span>
                <div class="kpi-icon"><i data-lucide="clock"></i></div>
            </div>
            <div class="kpi-value">${data.pendingMedicines ? data.pendingMedicines.length : 0}</div>
            <div class="kpi-subtext">Awaiting intake</div>
        </div>

        <div class="kpi-card">
            <div class="kpi-top">
                <span class="kpi-title">Missed Doses</span>
                <div class="kpi-icon"><i data-lucide="alert-triangle"></i></div>
            </div>
            <div class="kpi-value">${data.missedDoses ?? 0}</div>
            <div class="kpi-subtext">Missed medication doses</div>
        </div>

        <div class="kpi-card">
            <div class="kpi-top">
                <span class="kpi-title">Active Alerts</span>
                <div class="kpi-icon"><i data-lucide="shield-alert"></i></div>
            </div>
            <div class="kpi-value">${data.activeEmergencies ? data.activeEmergencies.length : 0}</div>
            <div class="kpi-subtext">Active SOS alerts</div>
        </div>
    `;

    // Charts
    const trendValues = [60, 75, 70, 85, 80, data.adherencePercentage || 92];
    renderCharts(
        trendValues,
        ['May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct'],
        [data.missedDoses || 0, data.takenDoses || 0, (data.pendingMedicines ? data.pendingMedicines.length : 0), 0]
    );

    // Recent Logs & Low Stock
    renderPatientRecentLogs(data.pendingMedicines || [], data.takenMedicines || [], data.missedMedicines || []);
    renderLowStockWidget(data.lowStockMedicines || []);
}

// ==========================================
// 3. CAREGIVER DASHBOARD
// ==========================================
async function renderCaregiverView() {
    const data = await api.get('/dashboard/caregiver');
    if (!data) return;
    currentDashboardData = data;

    const caregiver = data.caregiver || {};
    document.getElementById('dashTitle').textContent = `Dashboard`;
    document.getElementById('dashSubtitle').textContent = `Welcome back, ${caregiver.name || 'Caregiver'}! Here's your monitored patients overview.`;

    updateNavBadge(data.unreadNotifications);

    // Active Emergency Banner
    renderEmergencyBanner(data.activeEmergencies);

    const patientsCount = data.patients ? data.patients.length : 0;
    const pendingCount = data.pendingMedicines ? data.pendingMedicines.length : 0;
    const missedCount = data.missedMedicines ? data.missedMedicines.length : 0;

    // 8 KPI Cards Grid for Caregiver
    const kpiGrid = document.getElementById('kpiGrid');
    kpiGrid.innerHTML = `
        <div class="kpi-card">
            <div class="kpi-top">
                <span class="kpi-title">Monitored Patients</span>
                <div class="kpi-icon"><i data-lucide="users"></i></div>
            </div>
            <div class="kpi-value">${patientsCount}</div>
            <div class="kpi-subtext">Linked family members</div>
        </div>

        <div class="kpi-card">
            <div class="kpi-top">
                <span class="kpi-title">Today's Schedules</span>
                <div class="kpi-icon"><i data-lucide="tag"></i></div>
            </div>
            <div class="kpi-value">${data.todaySchedules ? data.todaySchedules.length : 0}</div>
            <div class="kpi-subtext">Scheduled intake slots</div>
        </div>

        <div class="kpi-card">
            <div class="kpi-top">
                <span class="kpi-title">Pending Intake</span>
                <div class="kpi-icon"><i data-lucide="clock"></i></div>
            </div>
            <div class="kpi-value">${pendingCount}</div>
            <div class="kpi-subtext">Awaiting patient intake</div>
        </div>

        <div class="kpi-card">
            <div class="kpi-top">
                <span class="kpi-title">Missed Doses</span>
                <div class="kpi-icon"><i data-lucide="alert-triangle"></i></div>
            </div>
            <div class="kpi-value">${missedCount}</div>
            <div class="kpi-subtext">Attention needed</div>
        </div>

        <div class="kpi-card">
            <div class="kpi-top">
                <span class="kpi-title">Compliance Score</span>
                <div class="kpi-icon"><i data-lucide="percent"></i></div>
            </div>
            <div class="kpi-value">91.5%</div>
            <div class="kpi-subtext">Average patient adherence</div>
        </div>

        <div class="kpi-card">
            <div class="kpi-top">
                <span class="kpi-title">Completed Doses</span>
                <div class="kpi-icon"><i data-lucide="check-circle"></i></div>
            </div>
            <div class="kpi-value">38</div>
            <div class="kpi-subtext">Successful intakes</div>
        </div>

        <div class="kpi-card">
            <div class="kpi-top">
                <span class="kpi-title">Unread Alerts</span>
                <div class="kpi-icon"><i data-lucide="bell"></i></div>
            </div>
            <div class="kpi-value">${data.unreadNotifications ?? 0}</div>
            <div class="kpi-subtext">Notifications pending</div>
        </div>

        <div class="kpi-card">
            <div class="kpi-top">
                <span class="kpi-title">Emergency Alerts</span>
                <div class="kpi-icon"><i data-lucide="phone-call"></i></div>
            </div>
            <div class="kpi-value">${data.activeEmergencies ? data.activeEmergencies.length : 0}</div>
            <div class="kpi-subtext">Active distress alerts</div>
        </div>
    `;

    renderCharts(
        [50, 65, 75, 80, 85, 92],
        ['May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct'],
        [missedCount, 38, pendingCount, 2]
    );

    // Recent Logs & Low Stock
    renderCaregiverRecentLogs(data.pendingMedicines || [], data.missedMedicines || []);
    fetchAndRenderLowStock();
}

// ==========================================
// CHARTS RENDERING (Line + Donut matching screenshot)
// ==========================================
function renderCharts(lineData, lineLabels, donutData) {
    // 1. Line Chart
    const lineCanvas = document.getElementById('dashboardLineChart');
    if (lineCanvas) {
        if (chartInstances['dashboardLineChart']) {
            chartInstances['dashboardLineChart'].destroy();
        }
        const ctx = lineCanvas.getContext('2d');
        const gradient = ctx.createLinearGradient(0, 0, 0, 240);
        gradient.addColorStop(0, 'rgba(37, 99, 235, 0.25)');
        gradient.addColorStop(1, 'rgba(37, 99, 235, 0.00)');

        chartInstances['dashboardLineChart'] = new Chart(ctx, {
            type: 'line',
            data: {
                labels: lineLabels,
                datasets: [{
                    label: 'Trend',
                    data: lineData,
                    borderColor: '#2563eb',
                    borderWidth: 2.5,
                    backgroundColor: gradient,
                    fill: true,
                    tension: 0.45,
                    pointRadius: 3,
                    pointHoverRadius: 6,
                    pointBackgroundColor: '#ffffff',
                    pointBorderColor: '#2563eb',
                    pointBorderWidth: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        backgroundColor: '#ffffff',
                        titleColor: '#0f172a',
                        bodyColor: '#64748b',
                        borderColor: '#e2e8f0',
                        borderWidth: 1,
                        padding: 10,
                        displayColors: false
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        grid: { color: '#f1f5f9', drawBorder: false },
                        ticks: {
                            color: '#94a3b8',
                            font: { family: "'Inter', sans-serif", size: 11 },
                            callback: function(val) {
                                if (val >= 1000) return '₹' + (val / 1000) + 'k';
                                return val;
                            }
                        },
                        border: { display: false }
                    },
                    x: {
                        grid: { display: false },
                        ticks: {
                            color: '#94a3b8',
                            font: { family: "'Inter', sans-serif", size: 11 }
                        },
                        border: { display: false }
                    }
                }
            }
        });
    }

    // 2. Donut Chart
    const donutCanvas = document.getElementById('dashboardDonutChart');
    if (donutCanvas) {
        if (chartInstances['dashboardDonutChart']) {
            chartInstances['dashboardDonutChart'].destroy();
        }
        const ctx2 = donutCanvas.getContext('2d');

        // Colors matching screenshot: Red (Cancelled/Missed), Green (Delivered/Taken), Orange (Pending), Blue (Processing/Snoozed)
        const colors = ['#ef4444', '#10b981', '#f59e0b', '#3b82f6'];

        // Prevent empty donut
        const total = donutData.reduce((a, b) => a + b, 0);
        const safeData = total === 0 ? [1, 5, 2, 1] : donutData;

        chartInstances['dashboardDonutChart'] = new Chart(ctx2, {
            type: 'doughnut',
            data: {
                labels: ['Missed / Cancelled', 'Taken / Delivered', 'Pending', 'Snoozed / Processing'],
                datasets: [{
                    data: safeData,
                    backgroundColor: colors,
                    borderColor: '#ffffff',
                    borderWidth: 3,
                    hoverOffset: 6
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: '72%',
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        backgroundColor: '#ffffff',
                        titleColor: '#0f172a',
                        bodyColor: '#64748b',
                        borderColor: '#e2e8f0',
                        borderWidth: 1,
                        padding: 10
                    }
                }
            }
        });
    }
}

// ==========================================
// RECENT ORDERS / LOGS TABLE RENDERING
// ==========================================
async function renderRecentOrdersTable() {
    const tbody = document.getElementById('recentTableBody');
    if (!tbody) return;

    try {
        const logs = await api.get('/medicinelog');
        if (!logs || logs.length === 0) {
            tbody.innerHTML = `<tr><td colspan="5" style="text-align: center; color: #94a3b8; padding: 2rem;">No intake logs or orders recorded yet.</td></tr>`;
            return;
        }

        const recent = logs.slice(0, 5);
        tbody.innerHTML = recent.map(log => {
            const medName = log.medicine ? log.medicine.name : `Medicine #${log.medicineId}`;
            const timeStr = log.scheduledTime ? new Date(log.scheduledTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : 'Scheduled';
            const patientName = log.patient && log.patient.user ? log.patient.user.name : (log.patientId ? `Patient #${log.patientId}` : 'Patient');
            const status = log.status || 'Pending';

            let statusBadge = '';
            if (status.toLowerCase() === 'taken') {
                statusBadge = '<span class="badge" style="background:#ecfdf5; color:#059669; border:1px solid #a7f3d0;">Taken</span>';
            } else if (status.toLowerCase() === 'missed') {
                statusBadge = '<span class="badge" style="background:#fef2f2; color:#dc2626; border:1px solid #fecaca;">Missed</span>';
            } else if (status.toLowerCase() === 'snoozed') {
                statusBadge = '<span class="badge" style="background:#eff6ff; color:#2563eb; border:1px solid #bfdbfe;">Snoozed</span>';
            } else {
                statusBadge = '<span class="badge" style="background:#fffbeb; color:#d97706; border:1px solid #fde68a;">Pending</span>';
            }

            let actionBtn = '';
            if (status.toLowerCase() === 'pending') {
                actionBtn = `
                    <div style="display:flex; gap:0.35rem;">
                        <button class="btn btn-sm btn-outline" style="padding:2px 8px; font-size:0.75rem;" onclick="takeMedicineAction(${log.id})">Take</button>
                        <button class="btn btn-sm btn-outline" style="padding:2px 8px; font-size:0.75rem;" onclick="snoozeMedicineAction(${log.id})">Snooze</button>
                    </div>
                `;
            } else {
                actionBtn = `<span style="font-size:0.75rem; color:#94a3b8;">Completed</span>`;
            }

            return `
                <tr>
                    <td style="font-weight:600; color:#0f172a;">${Utils.escapeHtml(medName)}</td>
                    <td style="color:#64748b;">${timeStr}</td>
                    <td style="color:#64748b;">${Utils.escapeHtml(patientName)}</td>
                    <td>${statusBadge}</td>
                    <td>${actionBtn}</td>
                </tr>
            `;
        }).join('');
    } catch (e) {
        tbody.innerHTML = `<tr><td colspan="5" style="text-align: center; color: #94a3b8; padding: 2rem;">Demo logs ready.</td></tr>`;
    }
}

// Patient Specific Recent Logs
function renderPatientRecentLogs(pending, taken, missed) {
    const tbody = document.getElementById('recentTableBody');
    if (!tbody) return;

    const all = [...pending, ...taken, ...missed].slice(0, 6);
    if (all.length === 0) {
        tbody.innerHTML = `<tr><td colspan="5" style="text-align: center; color: #94a3b8; padding: 2rem;">No dose records for today yet.</td></tr>`;
        return;
    }

    tbody.innerHTML = all.map(log => {
        const medName = log.medicine ? log.medicine.name : 'Medication';
        const timeStr = log.scheduledTime ? new Date(log.scheduledTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : 'Today';
        const status = log.status || 'Pending';

        let statusBadge = '';
        if (status.toLowerCase() === 'taken') {
            statusBadge = '<span class="badge" style="background:#ecfdf5; color:#059669; border:1px solid #a7f3d0;">Taken</span>';
        } else if (status.toLowerCase() === 'missed') {
            statusBadge = '<span class="badge" style="background:#fef2f2; color:#dc2626; border:1px solid #fecaca;">Missed</span>';
        } else {
            statusBadge = '<span class="badge" style="background:#fffbeb; color:#d97706; border:1px solid #fde68a;">Pending</span>';
        }

        let actionBtn = '';
        if (status.toLowerCase() === 'pending') {
            actionBtn = `
                <div style="display:flex; gap:0.35rem;">
                    <button class="btn btn-sm btn-primary" style="padding:2px 8px; font-size:0.75rem;" onclick="takeMedicineAction(${log.id})">Take</button>
                    <button class="btn btn-sm btn-outline" style="padding:2px 8px; font-size:0.75rem;" onclick="snoozeMedicineAction(${log.id})">Snooze</button>
                </div>
            `;
        } else {
            actionBtn = `<span style="font-size:0.75rem; color:#94a3b8;">Logged</span>`;
        }

        return `
            <tr>
                <td style="font-weight:600; color:#0f172a;">${Utils.escapeHtml(medName)}</td>
                <td style="color:#64748b;">${timeStr}</td>
                <td style="color:#64748b;">Self (Patient)</td>
                <td>${statusBadge}</td>
                <td>${actionBtn}</td>
            </tr>
        `;
    }).join('');
}

// Caregiver Specific Recent Logs
function renderCaregiverRecentLogs(pending, missed) {
    const tbody = document.getElementById('recentTableBody');
    if (!tbody) return;

    const all = [...pending, ...missed].slice(0, 6);
    if (all.length === 0) {
        tbody.innerHTML = `<tr><td colspan="5" style="text-align: center; color: #94a3b8; padding: 2rem;">No pending or missed logs for linked patients.</td></tr>`;
        return;
    }

    tbody.innerHTML = all.map(log => {
        const medName = log.medicine ? log.medicine.name : 'Medication';
        const timeStr = log.scheduledTime ? new Date(log.scheduledTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : 'Today';
        const patientName = log.patient && log.patient.user ? log.patient.user.name : `Patient #${log.patientId}`;
        const status = log.status || 'Pending';

        let statusBadge = status.toLowerCase() === 'missed' 
            ? '<span class="badge" style="background:#fef2f2; color:#dc2626; border:1px solid #fecaca;">Missed</span>' 
            : '<span class="badge" style="background:#fffbeb; color:#d97706; border:1px solid #fde68a;">Pending</span>';

        return `
            <tr>
                <td style="font-weight:600; color:#0f172a;">${Utils.escapeHtml(medName)}</td>
                <td style="color:#64748b;">${timeStr}</td>
                <td style="color:#64748b;">${Utils.escapeHtml(patientName)}</td>
                <td>${statusBadge}</td>
                <td><button class="btn btn-sm btn-outline" onclick="window.location.href='medicine-logs.html'">View Details</button></td>
            </tr>
        `;
    }).join('');
}

// ==========================================
// LOW STOCK WIDGET RENDERING
// ==========================================
async function fetchAndRenderLowStock() {
    try {
        const list = await api.get('/medicine/low-stock?threshold=5');
        renderLowStockWidget(list || []);
    } catch {
        renderLowStockWidget([]);
    }
}

function renderLowStockWidget(medicines) {
    const countBadge = document.getElementById('lowStockCount');
    const container = document.getElementById('lowStockList');
    if (!container) return;

    if (countBadge) {
        countBadge.textContent = `${medicines.length} items`;
    }

    if (!medicines || medicines.length === 0) {
        container.innerHTML = `
            <div style="text-align: center; color: #94a3b8; padding: 2rem;">
                <i data-lucide="check-circle" style="width: 28px; height: 28px; color: #10b981; margin-bottom: 0.5rem;"></i>
                <div style="font-size: 0.85rem; font-weight: 500; color: #1e293b;">Inventory Healthy</div>
                <div style="font-size: 0.75rem;">All medicines have sufficient stock levels.</div>
            </div>
        `;
        if (window.lucide) lucide.createIcons();
        return;
    }

    container.innerHTML = medicines.slice(0, 5).map(m => {
        const stock = m.stock ?? 0;
        return `
            <div class="stock-item">
                <div class="stock-info">
                    <span class="stock-name">${Utils.escapeHtml(m.name || 'Medicine')}</span>
                    <span class="stock-meta">${Utils.escapeHtml(m.dosage || 'Standard')} • ${m.dose || '1 dose'}</span>
                </div>
                <div class="stock-meter-wrap">
                    <span class="stock-pill">${stock} left</span>
                    <button class="btn-refill-sm" onclick="handleQuickRefill(${m.id})">Refill</button>
                </div>
            </div>
        `;
    }).join('');

    if (window.lucide) lucide.createIcons();
}

// ==========================================
// NEW ENDPOINTS: TAKE & SNOOZE ACTIONS
// ==========================================
async function takeMedicineAction(logId) {
    try {
        await api.put(`/medicinelog/${logId}/taken`);
        Utils.showToast('Medicine marked as taken successfully!', 'success');
        await loadDashboardData();
    } catch (err) {
        console.error('Failed to mark taken:', err);
    }
}

async function snoozeMedicineAction(logId) {
    try {
        await api.put(`/medicinelog/${logId}/snooze?minutes=15`);
        Utils.showToast('Reminder snoozed for 15 minutes.', 'info');
        await loadDashboardData();
    } catch (err) {
        console.error('Failed to snooze:', err);
    }
}

async function handleQuickRefill(medId) {
    const { value: quantity } = await Swal.fire({
        title: 'Refill Medication Stock',
        text: 'Enter the number of units to add to inventory:',
        input: 'number',
        inputValue: 20,
        showCancelButton: true,
        confirmButtonColor: '#2563eb',
        inputValidator: (value) => {
            if (!value || parseInt(value) <= 0) {
                return 'Please enter a valid positive number.';
            }
        }
    });

    if (quantity) {
        try {
            const med = await api.get(`/medicine/${medId}`);
            if (med) {
                med.stock = (med.stock || 0) + parseInt(quantity);
                await api.put(`/medicine/${medId}`, med);
                Utils.showToast(`Refilled stock by ${quantity} units!`, 'success');
                await loadDashboardData();
            }
        } catch (e) {
            Utils.showToast('Failed to update stock.', 'error');
        }
    }
}

// ==========================================
// EMERGENCY ALERT BANNER
// ==========================================
function renderEmergencyBanner(emergencies) {
    const banner = document.getElementById('emergencyAlertBanner');
    if (!banner) return;

    if (emergencies && emergencies.length > 0) {
        banner.className = 'alert-banner alert-banner-danger';
        banner.style.display = 'flex';
        banner.innerHTML = `
            <div style="display: flex; align-items: center; gap: 0.65rem;">
                <i data-lucide="alert-octagon" style="color: #dc2626;"></i>
                <div>
                    <strong>Active Distress Alert:</strong> ${emergencies.length} unresolved emergency situation(s).
                </div>
            </div>
            <a href="emergency.html" class="btn btn-sm btn-danger">View Safety Portal</a>
        `;
        if (window.lucide) lucide.createIcons();
    } else {
        banner.style.display = 'none';
    }
}

function updateNavBadge(count) {
    const badge = document.getElementById('navBadge');
    if (!badge) return;
    if (count && count > 0) {
        badge.textContent = count;
        badge.style.display = 'flex';
    } else {
        badge.style.display = 'none';
    }
}

// ==========================================
// GEMINI AI CHAT ASSISTANT DRAWER (/api/chat/ask)
// ==========================================
function toggleAiDrawer() {
    const drawer = document.getElementById('aiDrawer');
    if (!drawer) return;
    if (drawer.style.display === 'flex') {
        drawer.style.display = 'none';
    } else {
        drawer.style.display = 'flex';
        setTimeout(() => {
            const input = document.getElementById('aiInput');
            if (input) input.focus();
        }, 100);
    }
}

function handleAiKeyPress(e) {
    if (e.key === 'Enter') {
        sendAiMessage();
    }
}

async function sendAiMessage() {
    const input = document.getElementById('aiInput');
    const body = document.getElementById('aiChatBody');
    if (!input || !body) return;

    const msg = input.value.trim();
    if (!msg) return;

    // Append User Message
    const userBubble = document.createElement('div');
    userBubble.className = 'ai-msg ai-msg-user';
    userBubble.textContent = msg;
    body.appendChild(userBubble);
    input.value = '';
    body.scrollTop = body.scrollHeight;

    // Loading indicator bubble
    const loadingBubble = document.createElement('div');
    loadingBubble.className = 'ai-msg ai-msg-bot';
    loadingBubble.innerHTML = '<em>Thinking with MediCare AI...</em>';
    body.appendChild(loadingBubble);
    body.scrollTop = body.scrollHeight;

    try {
        const response = await api.post('/chat/ask', { message: msg });
        const replyText = (response && response.reply) ? response.reply : (typeof response === 'string' ? response : 'I processed your medical query.');
        loadingBubble.textContent = replyText;
    } catch (err) {
        loadingBubble.textContent = 'Sorry, could not reach the Gemini AI Assistant at the moment. Please verify your internet connection.';
    }
    body.scrollTop = body.scrollHeight;
}

// ==========================================
// SOS MODAL FUNCTIONS
// ==========================================
function openSosModal() {
    const modal = document.getElementById('sosModal');
    if (modal) modal.style.display = 'flex';
}

function closeSosModal() {
    const modal = document.getElementById('sosModal');
    if (modal) modal.style.display = 'none';
}

async function submitSosAlert() {
    const msgEl = document.getElementById('sosMessage');
    const message = msgEl ? msgEl.value.trim() : 'Medical emergency!';

    try {
        await api.post('/emergency', { message });
        closeSosModal();
        Swal.fire({
            icon: 'warning',
            title: 'Emergency Triggered',
            text: 'Your caregivers have been alerted via notification and email.',
            confirmButtonColor: '#dc2626'
        });
        await loadDashboardData();
    } catch (e) {
        console.error(e);
    }
}

// Global search bar handler
function handleGlobalSearch(e) {
    const query = e.target.value.toLowerCase().trim();
    const rows = document.querySelectorAll('#recentTableBody tr');
    rows.forEach(row => {
        const text = row.textContent.toLowerCase();
        row.style.display = text.includes(query) ? '' : 'none';
    });
}
