# MediCare — Smart Medicine Reminder & Family Monitoring System

**MediCare** is a medicine management and reminder system designed to help patients take their medicines on time and allow linked caregivers to monitor medication activities. It aims to reduce missed doses through scheduled reminders and email notifications while supporting family-based care.

## 🚀 Features

- **User Authentication:** Secure login and role-based access.
- **Patient Management:** Maintain patient profiles and personal information.
- **Caregiver Management:** Link caregivers with patients for family monitoring.
- **Medicine Management:** Add, update, and manage medicine details and schedules.
- **Automated Medicine Reminders:** A background service checks medicine schedules and processes due reminders.
- **Email Notifications:** Send medicine reminder emails to patients.
- **Missed Dose Monitoring:** Track overdue medicine doses and notify linked caregivers according to the configured rules.
- **Caregiver Support:** Allow linked caregivers to access relevant patient and medicine information and assist with medicine management.
- **RESTful APIs:** Backend endpoints for application functionality and data management.
- **MySQL Database:** Store application data using Entity Framework Core.
- **Exception Handling:** Centralized error-handling middleware for improved API reliability.

## 🛠️ Technology Stack

| Component             | Technology                       |
| --------------------- | -------------------------------- |
| Backend               | ASP.NET Core Web API             |
| Programming Language  | C#                               |
| Framework             | .NET 10                          |
| Database              | MySQL                            |
| ORM                   | Entity Framework Core            |
| MySQL Provider        | Pomelo.EntityFrameworkCore.MySql |
| Authentication        | JWT Bearer Authentication        |
| API Testing           | Postman                          |
| API Documentation     | Swagger / OpenAPI                |
| Background Processing | .NET BackgroundService           |
| Version Control       | Git and GitHub                   |

## 🏗️ System Architecture

The application follows a layered architecture to separate API endpoints, business logic, data access, and background processing.

```text
Patient / Caregiver / Admin
            |
            v
      ASP.NET Core API
            |
            v
        Controllers
            |
            v
         Services
            |
            v
       Repositories
            |
            v
     Entity Framework Core
            |
            v
        MySQL Database

    Background Reminder Service
            |
            v
   Scheduled Dose Monitoring
            |
            v
    Email Notifications
```

## 💊 How It Works

1. Users log in to the system.
2. Patients manage their profiles and medicine schedules.
3. Caregivers are linked to patients through the caregiver-patient relationship.
4. The background service checks medicine schedules at configured intervals.
5. When a medicine reminder becomes due, the system processes the reminder and sends an email notification according to its configuration.
6. If a dose remains overdue, the configured missed-dose monitoring logic can notify the linked caregiver.
7. Caregivers can access relevant patient information and assist with medicine management, subject to authorization.

## 🗄️ Database

MediCare uses MySQL to store application data. Entity Framework Core manages database access and migrations.

The main entities include:

- **User:** Stores user account information and roles.
- **Patient:** Stores patient-specific details.
- **Caregiver:** Stores caregiver information.
- **CaregiverPatient:** Maintains the relationship between caregivers and patients.
- **Medicine:** Stores medicine information and scheduling details.

Additional entities may be used for dose tracking, reminders, or notifications, depending on the implemented database schema.

## 🔐 Security

- JWT-based authentication.
- Role-based access control where configured.
- Authorized access to protected API endpoints.
- Separation of application logic into controllers, services, and repositories.
- Centralized exception handling.

> Security depends on the actual endpoint authorization policies and configuration. Production deployment should also use HTTPS and securely stored secrets.

## ⚙️ Getting Started

### Prerequisites

Install the following before running the project:

- [.NET SDK](https://dotnet.microsoft.com/download)
- [MySQL Community Server](https://dev.mysql.com/downloads/mysql/)
- [Git](https://git-scm.com/downloads)
- [Postman](https://www.postman.com/downloads/)

### 1. Clone the Repository

```bash
git clone https://github.com/Gourav-93/MediCare-Smart-Medicine-Monitoring-System.git
```

### 2. Open the Project

```bash
cd MediCare-Smart-Medicine-Monitoring-System
```

### 3. Configure the Database

Update the MySQL connection string in `appsettings.json` or your development configuration.

Example:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;port=3306;database=MediCareDb;user=YOUR_USERNAME;password=YOUR_PASSWORD;"
  }
}
```

Use the actual connection-string key and database name defined in your project. Do not commit real database passwords or secrets to GitHub.

### 4. Restore Dependencies

```bash
dotnet restore
```

### 5. Apply Database Migrations

If migrations are already included in the repository:

```bash
dotnet ef database update
```

If the Entity Framework CLI is not installed:

```bash
dotnet tool install --global dotnet-ef
```

Use a compatible EF Core tool version for the project. If necessary, install the project's matching version instead.

### 6. Run the Application

```bash
dotnet run
```

Check the terminal output for the actual API address and port.

### 7. Test the APIs

Open Postman and send requests to the configured API base URL. Test authentication first, then use the returned JWT token to access protected endpoints.

Use the HTTP methods and routes defined by the project's controllers.

## 🧪 Testing Checklist

- [ ] User registration or account setup, if implemented.
- [ ] Login and JWT token generation.
- [ ] Patient profile operations.
- [ ] Caregiver profile operations.
- [ ] Linking a caregiver to a patient.
- [ ] Adding and updating medicines.
- [ ] Validating medicine schedules.
- [ ] Checking scheduled reminder execution.
- [ ] Verifying patient reminder emails.
- [ ] Verifying caregiver notifications for overdue doses.
- [ ] Checking authorization and invalid request handling.

## 🎯 Project Objective

The main objective of MediCare is to make medicine management easier and support patients who may forget their scheduled doses. By combining automated reminders with caregiver monitoring, the system provides a foundation for more organized medication routines and family support.

## 🔮 Future Enhancements

- Mobile push notifications.
- Medicine stock and refill alerts.
- Downloadable medicine history reports.
- Dashboard analytics for adherence trends.
- Multiple notification channels.
- Improved reminder preferences and timezone support.

## 👨‍💻 Developer

**Gourav Khore**

- GitHub: [Gourav-93](https://github.com/Gourav-93)
- Project: MediCare — Smart Medicine Reminder & Family Monitoring System

## 📄 License

This project is intended for learning, development, and demonstration purposes. Add a specific open-source license if you decide to distribute it under one.

---

**MediCare — Helping patients stay on schedule, with support from the people who care.**
