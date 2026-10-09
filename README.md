# MediCare - Smart Medicine Monitoring System

MediCare is a comprehensive healthcare application designed to manage medicines, scheduling, and adherence for patients. It features multi-role access (Admin, Patient, Caregiver) to ensure patients receive proper care and timely reminders for their medication, while caregivers can monitor their assigned patients' adherence.

## 🚀 Key Features

*   **Multi-Role Access Control:** Separate dashboards and functionalities for Admin, Patients, and Caregivers.
*   **Medicine Management:** Easily add, update, and track medicines.
*   **Intelligent Scheduling:** Set up flexible schedules for medicine intake.
*   **Automated Reminders:** Background services automatically send timely reminders to patients (via Email/Notifications).
*   **Adherence Tracking:** Patients can log their medication intake, allowing the system to track and calculate adherence rates.
*   **Caregiver Portal:** Caregivers can monitor their patients' medication logs, schedules, and overall adherence.
*   **Secure Authentication:** JWT-based authentication for secure access and data privacy.
*   **Emergency Services:** Quick access to emergency contacts and actions.

## 🛠️ Tech Stack

**Backend (API):**
*   C# / .NET (ASP.NET Core Web API)
*   Entity Framework Core (EF Core)
*   MySQL Database
*   JWT (JSON Web Tokens) for Authentication
*   Hosted Background Services (for automated reminders)

**Frontend:**
*   HTML5, CSS3, Vanilla JavaScript
*   Chart.js (for adherence charts/visualizations)
*   Responsive UI Design

## 📁 Project Structure

*   `Controllers/`: API Endpoints for various modules (Medicines, Auth, Logs, etc.).
*   `Models/`: Entity classes representing the database tables.
*   `DTOs/`: Data Transfer Objects for secure and optimized API communication.
*   `Services/`: Business logic layer (MedicineService, EmailService, AdherenceService, etc.).
*   `Repositories/`: Data access layer for database operations.
*   `BackgroundServices/`: Contains `MedicineReminderBackgroundService` for continuous reminder checks.
*   `MediCare-Frontend/`: The web application UI consisting of HTML, CSS, and JS files.

## ⚙️ Setup & Installation

### Prerequisites
*   [.NET SDK](https://dotnet.microsoft.com/download) (v8.0 or appropriate version)
*   [MySQL Server](https://dev.mysql.com/downloads/)
*   Live Server (VS Code Extension) or any basic HTTP server for the frontend.

### 1. Database Configuration
1.  Open `appsettings.json`.
2.  Update the `DefaultConnection` string with your MySQL server credentials:
    ```json
    "ConnectionStrings": {
      "DefaultConnection": "Server=localhost;Database=MediCareDb;User=root;Password=your_password;"
    }
    ```
3.  Ensure your `Jwt` settings (Key, Issuer, Audience) are properly configured.

### 2. Run Database Migrations
Open your terminal in the project root and run:
```bash
dotnet ef database update
```
*(Ensure you have EF Core tools installed: `dotnet tool install --global dotnet-ef`)*

### 3. Run the Backend API
```bash
dotnet run
```
The API will start running (typically on `https://localhost:7000` or `http://localhost:5000`).

### 4. Run the Frontend
1. Navigate to the `MediCare-Frontend` folder.
2. If you are using VS Code, you can right-click `index.html` and select **"Open with Live Server"**.
3. Ensure the API base URL in your frontend JavaScript files (e.g., `js/charts.js`, `js/caregiver-dashboard.js`) points to your local running API.

## 🧑‍🤝‍🧑 User Roles

*   **Patient:** Can view their schedules, log medicine intake, and view personal adherence charts.
*   **Caregiver:** Assigned to specific patients to monitor their schedules and adherence logs.
*   **Admin:** Has overarching control over the system, managing users, medicines, and system settings.

## 📄 License
This project is for educational and portfolio purposes.
