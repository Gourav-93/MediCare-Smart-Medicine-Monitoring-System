# MediCare Smart Medicine Monitoring System — API Mapping Document

This document provides a comprehensive mapping of every controller, endpoint, route, HTTP method, required authorization role, request/response DTO, and corresponding frontend component in the **MediCare** system.

---

## 1. Authentication & Identity (`AuthController`)

**Base Route:** `api/AuthController`  
**Authentication:** Anonymous (Public)

| Endpoint | Method | Roles | Request Body | Response Format | Frontend Usage |
| :--- | :---: | :---: | :--- | :--- | :--- |
| `/api/AuthController/register` | `POST` | Anonymous | `RegisterDto`<br>`{ name: string, email: string, password: string, role: "PATIENT" \| "CAREGIVER" }` | `{ message: string, userId: number, role: string }` | `register.html`<br>Client-side validation, restricts public ADMIN creation |
| `/api/AuthController/login` | `POST` | Anonymous | `LoginDto`<br>`{ email: string, password: string }` | `{ message: string, token: string, userId: number, name: string, email: string, role: string }` | `login.html`<br>Extracts JWT token & profile, redirects by role |

> **Security Note:** Public registration for role `ADMIN` is strictly rejected by the backend (HTTP 400).

---

## 2. Dashboard Controllers

### 2.1 Admin Dashboard (`AdminDashboardController`)

**Base Route:** `api/dashboard/admin`  
**Authentication:** Required (`ADMIN`)

| Endpoint | Method | Roles | Request Body | Response Format | Frontend Usage |
| :--- | :---: | :---: | :--- | :--- | :--- |
| `/api/dashboard/admin` | `GET` | `ADMIN` | None | `AdminDashboardDto`<br>- `totalUsers`, `totalPatients`, `totalCaregivers`<br>- `totalMedicines`, `totalMedicineLogs`<br>- `totalTakenDoses`, `totalMissedDoses`, `totalPendingDoses`<br>- `overallAdherencePercentage`<br>- `activeEmergencyAlerts`, `totalEmergencyAlerts`<br>- `unreadNotifications`, `caregiverPatientRelationships`<br>- `lowStockMedicines: Medicine[]`<br>- `activeEmergencies: EmergencyAlert[]` | `admin-dashboard.html`<br>KPI stats, Doughnut user distribution chart, dose status bar chart, low-stock table, active emergencies table |

### 2.2 Caregiver Dashboard (`CaregiverDashboardController`)

**Base Route:** `api/dashboard/caregiver`  
**Authentication:** Required (`CAREGIVER`)

| Endpoint | Method | Roles | Request Body | Response Format | Frontend Usage |
| :--- | :---: | :---: | :--- | :--- | :--- |
| `/api/dashboard/caregiver` | `GET` | `CAREGIVER` | None | `CaregiverDashboardDto`<br>- `caregiver: CaregiverInfoDto` (id, userId, name, email, relationship)<br>- `patients: CaregiverDashboardPatientDto[]`<br>- `activeEmergencies: EmergencyAlert[]`<br>- `missedMedicines: MedicineLog[]`<br>- `todaySchedules: MedicineSchedule[]`<br>- `pendingMedicines: MedicineLog[]`<br>- `adherence: PatientAdherenceDto[]`<br>- `unreadNotifications: number` | `caregiver-dashboard.html`<br>Patient overview cards, emergency banners, pending & missed queues |

### 2.3 Patient Dashboard (`PatientDashboardController`)

**Base Route:** `api/dashboard/patient`  
**Authentication:** Required (`PATIENT`)

| Endpoint | Method | Roles | Request Body | Response Format | Frontend Usage |
| :--- | :---: | :---: | :--- | :--- | :--- |
| `/api/dashboard/patient` | `GET` | `PATIENT` | None | `PatientDashboardDto`<br>- `patient: PatientInfoDto` (id, userId, name, email, dateOfBirth, emergencyContact)<br>- `totalMedicines: number`<br>- `medicines: Medicine[]`<br>- `todaySchedules: MedicineSchedule[]`<br>- `pendingMedicines: MedicineLog[]`<br>- `missedMedicines: MedicineLog[]`<br>- `takenMedicines: MedicineLog[]`<br>- `totalDoses`, `takenDoses`, `missedDoses`<br>- `adherencePercentage: number`<br>- `lowStockMedicines: Medicine[]`<br>- `activeEmergencies: EmergencyAlert[]`<br>- `unreadNotifications: number` | `patient-dashboard.html`<br>Minimal Bento-Grid Hub: Greeting, Next dose pill, Adherence circular gauge, Today's schedule checklist with instant "Mark as Taken", Low-stock warnings, SOS quick action |

---

## 3. Medicine Management (`MedicineController`)

**Base Route:** `api/medicine`  
**Authentication:** Required (`ADMIN`, `PATIENT`, `CAREGIVER`)

| Endpoint | Method | Roles | Request Body / Query | Response Format | Frontend Usage |
| :--- | :---: | :---: | :--- | :--- | :--- |
| `/api/medicine` | `GET` | All Roles | None | `Medicine[]` (Filtered by role: Admin gets all, Patient gets own, Caregiver gets linked patients') | `medicines.html` |
| `/api/medicine/{id}` | `GET` | All Roles | Route `id: number` | `Medicine` | Medicine edit modal |
| `/api/medicine` | `POST` | All Roles | `MedicineDto`<br>`{ patientId: number, name: string, dosage: string, dose: number, stock: number, startDate: DateTime, endDate: DateTime }` (Patient role auto-resolves patientId) | Created `Medicine` | Add Medicine Modal in `medicines.html` |
| `/api/medicine/{id}` | `PUT` | All Roles | Route `id`, `MedicineDto` | Updated `Medicine` | Edit Medicine Modal in `medicines.html` |
| `/api/medicine/{id}` | `DELETE` | All Roles | Route `id: number` | `"Medicine deleted successfully."` | Delete action in `medicines.html` |
| `/api/medicine/low-stock` | `GET` | All Roles | Query `threshold: number = 5` | `Medicine[]` | Low-stock badges & alerts in dashboards |

---

## 4. Medicine Schedules (`MedicineScheduleController`)

**Base Route:** `api/medicineschedule`  
**Authentication:** Required (`ADMIN`, `PATIENT`, `CAREGIVER`)

| Endpoint | Method | Roles | Request Body / Query | Response Format | Frontend Usage |
| :--- | :---: | :---: | :--- | :--- | :--- |
| `/api/medicineschedule` | `GET` | All Roles | None | `MedicineSchedule[]` (Includes `medicine`) | `schedule.html` |
| `/api/medicineschedule/{id}` | `GET` | All Roles | Route `id: number` | `MedicineSchedule` | Edit Schedule Modal |
| `/api/medicineschedule` | `POST` | All Roles | `MedicineScheduleDto`<br>`{ medicineId: number, time: TimeSpan ("HH:mm:ss"), frequency: string }` | Created `MedicineSchedule` | Add Schedule Form |
| `/api/medicineschedule/{id}` | `PUT` | All Roles | Route `id`, `MedicineScheduleDto` | Updated `MedicineSchedule` | Update Schedule Form |
| `/api/medicineschedule/{id}` | `DELETE` | All Roles | Route `id: number` | `"Schedule deleted successfully."` | Delete action in `schedule.html` |

---

## 5. Medicine Logs & History (`MedicineLogController`)

**Base Route:** `api/medicinelog`  
**Authentication:** Required (`ADMIN`, `PATIENT`, `CAREGIVER`)

| Endpoint | Method | Roles | Request Body / Query | Response Format | Frontend Usage |
| :--- | :---: | :---: | :--- | :--- | :--- |
| `/api/medicinelog` | `GET` | All Roles | None | `MedicineLog[]` | `medicine-logs.html` |
| `/api/medicinelog/{id}` | `GET` | All Roles | Route `id: number` | `MedicineLog` | Log inspector |
| `/api/medicinelog` | `POST` | All Roles | `MedicineLogDto`<br>`{ medicineId: number, patientId: number, scheduledTime: DateTime, status: string }` | Created `MedicineLog` | Log creation |
| `/api/medicinelog/{id}/taken` | `PUT` | `PATIENT` ONLY | Route `id: number` | Updated `MedicineLog` (`status: "Taken"`, `takenTime: DateTime.Now`, stock decremented by dose) | "Mark as Taken" button in `patient-dashboard.html` and `medicine-logs.html` |
| `/api/medicinelog/patient/{patientId}/history` | `GET` | All Roles | Route `patientId`<br>Query `MedicineLogFilterDto`<br>(fromDate, toDate, status, page, pageSize) | `List<MedicineLog>` | Patient History table in `caregiver-patient-details.html` and `medicine-logs.html` |

---

## 6. Adherence Reports (`AdherenceController`)

**Base Route:** `api/adherence`  
**Authentication:** Required (`ADMIN`, `PATIENT`, `CAREGIVER`)

| Endpoint | Method | Roles | Request Body / Query | Response Format | Frontend Usage |
| :--- | :---: | :---: | :--- | :--- | :--- |
| `/api/adherence/patient/{patientId}` | `GET` | All Roles | Route `patientId: number` | `AdherenceDto`<br>`{ patientId, totalDoses, takenDoses, missedDoses, adherencePercentage }` | Quick adherence pill / metric |
| `/api/adherence/patient/{patientId}/report` | `GET` | All Roles | Route `patientId`, Query `period: "Daily" \| "Weekly" \| "Monthly"` | `AdherenceReportDto`<br>`{ patientId, periodType, overallAdherence, timeline: [{ periodLabel, totalDoses, takenDoses, missedDoses, adherencePercentage }] }` | `adherence.html` and `caregiver-patient-details.html`<br>Interactive compliance timeline chart |

---

## 7. Medical Clinical Reports (`ReportController`)

**Base Route:** `api/report`  
**Authentication:** Required for JSON data, `[AllowAnonymous]` for printable view

| Endpoint | Method | Roles | Request Body / Query | Response Format | Frontend Usage |
| :--- | :---: | :---: | :--- | :--- | :--- |
| `/api/report/patient/{patientId}/data` | `GET` | All Roles | Route `patientId`, Query `days: number = 30` | `MedicalReportDto`<br>Patient demographic, adherence, active medicines, recent logs | `caregiver-patient-details.html` and `adherence.html` |
| `/api/report/patient/{patientId}/print` | `GET` | Public / Browser | Route `patientId`, Query `days: number = 30` | `text/html` (Print-optimized HTML document with window.print support) | "Download/Print Clinical PDF" button on reports and patient details |

---

## 8. Caregiver–Patient Relationships (`CaregiverPatientController`)

**Base Route:** `api/Caregiverpatient`  
**Authentication:** Required (`[Authorize]`)

| Endpoint | Method | Roles | Request Body / Query | Response Format | Frontend Usage |
| :--- | :---: | :---: | :--- | :--- | :--- |
| `/api/Caregiverpatient/link` | `POST` | `ADMIN`, `CAREGIVER` | `CaregiverPatientDto`<br>`{ caregiverId: number, patientId?: number, patientEmail?: string, relationType?: string }` | Created `CaregiverPatient` relation | "Link Patient" modal in `patients.html` and `caregivers.html` |
| `/api/Caregiverpatient/caregiver/{caregiverId}` | `GET` | `ADMIN`, `CAREGIVER` | Route `caregiverId: number` | `Patient[]` linked to caregiver | `patients.html` |
| `/api/Caregiverpatient/patient/{patientId}` | `GET` | All Roles | Route `patientId: number` | `Caregiver[]` linked to patient | `profile.html` and `caregiver-patient-details.html` |
| `/api/Caregiverpatient/unlink` | `DELETE` | `ADMIN`, `CAREGIVER` | Query `caregiverId: number`, `patientId: number` | `"Patient unlinked successfully."` | Unlink button with confirmation dialog |

---

## 9. Emergency Alerts (`EmergencyController`)

**Base Route:** `api/emergency`  
**Authentication:** Required (`PATIENT`, `CAREGIVER`, `ADMIN`)

| Endpoint | Method | Roles | Request Body / Query | Response Format | Frontend Usage |
| :--- | :---: | :---: | :--- | :--- | :--- |
| `/api/emergency` | `POST` | `PATIENT` ONLY | `EmergencyAlertDto`<br>`{ message: string }` | `{ message, emergencyId, status: "ACTIVE", caregiversNotified }` | SOS button & Modal on all patient pages |
| `/api/emergency` | `GET` | All Roles | None | `EmergencyAlert[]` (Filtered by role permissions) | `emergency.html` table & Dashboard alert banners |
| `/api/emergency/{id}/resolve` | `PUT` | All Roles (Authorized per relationship) | Route `id: number` | `{ message: "Emergency resolved successfully.", emergencyId, status: "RESOLVED", resolvedAt }` | "Resolve" action button for Caregiver and Admin |

---

## 10. Notifications Center (`NotificationController`)

**Base Route:** `api/notification`  
**Authentication:** Required (`ADMIN`, `PATIENT`, `CAREGIVER`)

| Endpoint | Method | Roles | Request Body / Query | Response Format | Frontend Usage |
| :--- | :---: | :---: | :--- | :--- | :--- |
| `/api/notification` | `GET` | All Roles | None | `Notification[]` (User's unread & read alerts) | `notifications.html` & Topbar unread badge |
| `/api/notification/{id}` | `GET` | All Roles | Route `id: number` | `Notification` | Notification item view |
| `/api/notification` | `POST` | `ADMIN` ONLY | `Notification`<br>`{ userId: number, title: string, message: string, type: string }` | Created `Notification` | Broadcast modal for Admin |
| `/api/notification/{id}/read` | `PUT` | All Roles | Route `id: number` | Updated `Notification` (`isRead: true`) | Clicking notification card |
| `/api/notification/read-all` | `PUT` | All Roles | None | `"All notifications marked as read."` | "Mark All as Read" header button |

---

## 11. Backend Limitations & Considerations

1. **User Profile Update:** The backend does not expose a `PUT /api/user/profile` endpoint. Profile information is presented in a clean, read-only format populated from the JWT token and patient/caregiver identity endpoints.
2. **Registration Roles:** Only `PATIENT` and `CAREGIVER` can register via `/api/AuthController/register`. Admins are created directly via database seeding or system provisioning.
3. **Emergency Notification Delivery:** The backend automatically attempts to send emails to linked caregivers via `IEmailService` upon `POST /api/emergency`. The frontend receives the count of notified caregivers.
