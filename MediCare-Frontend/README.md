# MediCare — Smart Medicine Reminder & Family Monitoring System (Frontend)

A modern, responsive, healthcare-grade web application built entirely using **HTML5, CSS3, Vanilla JavaScript (ES6+), Fetch API, and Chart.js**. This frontend integrates natively with the existing ASP.NET Core Web API backend without modifying any existing endpoints, DTOs, or database schemas.

---

## 🎨 Dual Design System Architecture

MediCare combines two specialized healthcare design paradigms into a single unified application:

### 1. Design A — Classic Enterprise Sidebar Dashboard
- **Target Audience:** Medical Administrators & Family Caregivers.
- **Pages:** `admin-dashboard.html`, `caregiver-dashboard.html`, `patients.html`, `caregiver-patient-details.html`, `medicines.html`, `schedule.html`, `adherence.html`.
- **Features:**
  - Fixed & collapsible navigation drawer with responsive mobile support.
  - Quick action header with role badges, theme toggler, and notification bell.
  - High-density KPI cards, clinical adherence line charts, active emergencies management, and searchable inventory tables.
  - Contextual modal dialogs with input validation and destructive action confirmations.

### 2. Design B — Minimal Bento-Grid Hub
- **Target Audience:** Patients.
- **Pages:** `patient-dashboard.html`.
- **Features:**
  - 12-column responsive Bento Grid layout.
  - Personalized time-aware greeting hero (`Good morning`, `Good afternoon`, `Good evening`).
  - Next dose highlight card with real-time countdown calculation.
  - Circular SVG adherence progress dial showing compliance percentage.
  - Interactive daily dose checklist with instant **"Mark as Taken"** action.
  - Medicine inventory stock status pills with automated low-stock warnings.
  - Connected caregiver cards and a prominent one-tap **Emergency SOS Dispatch**.

---

## 📁 Project Structure

```text
MediCare-Frontend/
├── index.html                      # Landing page with Bento feature showcase & role portals
├── login.html                      # JWT session authentication with password visibility toggle
├── register.html                   # Patient & Caregiver registration (Admin disallowed publicly)
├── patient-dashboard.html          # Minimal Bento-Grid Hub for patients
├── caregiver-dashboard.html        # Enterprise dashboard for caregivers
├── caregiver-patient-details.html  # Caregiver dossier & clinical report for a linked patient
├── admin-dashboard.html            # System administrator command center & KPI analytics
├── patients.html                   # Caregiver linked patient management & linking modal
├── medicines.html                  # Prescription & inventory management with low-stock filters
├── schedule.html                   # Daily dosage schedule configuration
├── medicine-logs.html              # Intake history table with status filters
├── adherence.html                  # Adherence compliance trends & printable medical reports
├── notifications.html              # Notification center with read/unread flags & admin broadcast
├── emergency.html                  # SOS emergency alert center & resolution workflow
├── profile.html                    # Personal identity details, role, and session credentials
├── serve.ps1                       # Built-in lightweight local static HTTP server
├── css/
│   ├── variables.css               # Design tokens (colors, typography, spacing, shadows, radii)
│   ├── base.css                    # Global resets, buttons, form controls, tables, badges, toasts
│   ├── dashboard.css               # Enterprise sidebar, navbar, KPI cards, charts
│   ├── bento.css                   # 12-column Bento-Grid styles, circular SVG gauge, dose pills
│   ├── responsive.css              # Mobile drawers, responsive tables, grid collapsing
│   ├── animations.css              # Smooth fade-in and scale transitions
│   └── style.css                   # Master CSS bundle importing all stylesheets
├── js/
│   ├── config.js                   # API base URL (http://localhost:5095/api) & storage keys
│   ├── utils.js                    # XSS escaping, date/time formatters, toast system, modals, theme
│   ├── api.js                      # Centralized Fetch wrapper with Bearer token, error handling
│   ├── auth.js                     # Session management, JWT claim extraction, role routing & guards
│   ├── patient-dashboard.js        # Bento hub controller, countdown, intake checklist
│   ├── caregiver-dashboard.js      # Caregiver KPI metrics, emergency alerts, patient cards
│   ├── caregiver-patient-details.js# Patient clinical report, adherence chart, medicine logs
│   ├── admin-dashboard.js          # Admin overview metrics, Chart.js breakdown charts
│   ├── patients.js                 # Patient directory & caregiver linking controller
│   ├── medicines.js                # Medicine CRUD and low-stock filter
│   ├── schedules.js                # Medicine schedule CRUD (TimeSpan HH:mm:00 conversion)
│   ├── medicine-logs.js            # History table with status filtering
│   ├── adherence.js                # Daily/Weekly/Monthly Chart.js trends & report generation
│   ├── notifications.js            # Notification center & admin broadcast modal
│   ├── emergency.js                # SOS alert modal & emergency resolution
│   ├── profile.js                  # User profile and security specifications
│   └── common.js                   # Cross-page legacy compatibility bridge
└── docs/
    └── API-MAPPING.md              # Complete backend API inventory (Controllers, routes, DTOs, roles)
```

---

## 🔌 API Integration Summary

| Module | Method & Route | Authorization | Description |
|---|---|---|---|
| **Auth** | `POST /api/AuthController/register` | Public | Register PATIENT or CAREGIVER |
| **Auth** | `POST /api/AuthController/login` | Public | JWT login with role redirection |
| **Admin** | `GET /api/dashboard/admin` | ADMIN | Metrics (users, patients, stock, alerts) |
| **Caregiver** | `GET /api/dashboard/caregiver` | CAREGIVER | Linked patients, schedules, emergencies |
| **Medicines** | `GET /api/medicine` | PATIENT, CAREGIVER, ADMIN | Prescription list |
| **Medicines** | `POST /api/medicine` | PATIENT, CAREGIVER, ADMIN | Add new prescription |
| **Medicines** | `PUT /api/medicine/{id}` | PATIENT, CAREGIVER, ADMIN | Edit prescription |
| **Medicines** | `DELETE /api/medicine/{id}` | PATIENT, CAREGIVER, ADMIN | Delete prescription |
| **Medicines** | `GET /api/medicine/low-stock` | PATIENT, CAREGIVER, ADMIN | Filter low-stock medicines |
| **Schedules** | `GET /api/medicineschedule` | PATIENT, CAREGIVER, ADMIN | All dosage schedules |
| **Schedules** | `POST /api/medicineschedule` | PATIENT, CAREGIVER, ADMIN | Create dosage schedule |
| **Schedules** | `DELETE /api/medicineschedule/{id}`| PATIENT, CAREGIVER, ADMIN | Remove dosage schedule |
| **Logs** | `GET /api/medicinelog` | PATIENT, CAREGIVER, ADMIN | All intake logs |
| **Logs** | `PUT /api/medicinelog/{id}/taken` | PATIENT, CAREGIVER, ADMIN | Mark dose as Taken |
| **Logs** | `GET /api/medicinelog/patient/{id}/history` | ALL ROLES | Patient intake history |
| **Adherence**| `GET /api/adherence/patient/{id}` | ALL ROLES | Overall compliance summary |
| **Adherence**| `GET /api/adherence/patient/{id}/report`| ALL ROLES | Periodic report (Daily, Weekly, Monthly) |
| **Reports** | `GET /api/report/patient/{id}/print`| ALL ROLES | Printable clinical HTML summary |
| **Links** | `GET /api/Caregiverpatient/patients`| CAREGIVER | View linked patients |
| **Links** | `POST /api/Caregiverpatient/link`| CAREGIVER | Link patient to caregiver |
| **Links** | `DELETE /api/Caregiverpatient/unlink` | CAREGIVER | Unlink patient |
| **Emergency**| `GET /api/emergency` | ALL ROLES | Active and historical emergency alerts |
| **Emergency**| `POST /api/emergency` | PATIENT | Dispatch new SOS alert |
| **Emergency**| `PUT /api/emergency/{id}/resolve`| CAREGIVER, ADMIN | Resolve active emergency |
| **Notifications**| `GET /api/notification` | ALL ROLES | User notification center |
| **Notifications**| `PUT /api/notification/{id}/read`| ALL ROLES | Mark notification as read |
| **Notifications**| `POST /api/notification/broadcast`| ADMIN | Broadcast notification to users |

---

## 🚀 How to Run the Application

### 1. Ensure ASP.NET Core Backend is Running
The backend must run on `http://localhost:5095`:
```bash
dotnet run --launch-profile http
```

### 2. Launch the Frontend Local Server
Inside the `MediCare-Frontend` folder, launch the included lightweight server:
```powershell
powershell -ExecutionPolicy Bypass -File .\serve.ps1
```
The application will be accessible at:
👉 **`http://localhost:3000`**

### 3. Navigate the Portals
- **Landing Page:** `http://localhost:3000/index.html`
- **Register Account:** `http://localhost:3000/register.html` (Register as Patient or Caregiver)
- **Sign In:** `http://localhost:3000/login.html` (Signs in and automatically redirects to your assigned portal)
  - **Patient:** Redirected to `patient-dashboard.html` (Minimal Bento-Grid Hub)
  - **Caregiver:** Redirected to `caregiver-dashboard.html` (Enterprise Monitoring Center)
  - **Admin:** Redirected to `admin-dashboard.html` (Executive Command Center)

---

## 🔒 Security Best Practices Implemented
1. **No Backend Alterations:** Strict adherence to existing routes, DTOs, parameters, and claims.
2. **Safe DOM Rendering:** Avoided unsafe `innerHTML` injection of unvalidated server strings by utilizing `Utils.escapeHtml()` and secure DOM node construction.
3. **Session-Based Storage:** Stored access tokens in `sessionStorage` with graceful multi-tab `localStorage` fallback.
4. **Active Route Guards:** Every protected page executes `Auth.requireAuth([...])` before fetching data. Unauthorized roles are immediately redirected.
5. **No Public Admin Registration:** Blocked administrative self-registration at the form and API validation layer.
