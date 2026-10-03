# MediCare – Smart Medicine Reminder & Family Monitoring System

MediCare is a real-world healthcare management system designed to help patients manage their medicines and allow caregivers/family members to monitor their health-related activities.

The project is built using **ASP.NET Core Web API** with **MySQL** and follows a layered architecture.

## Features

* User authentication with JWT
* Role-based access for Admin, Patient and Caregiver
* Patient profile management
* Caregiver and patient relationship management
* Medicine management
* Medicine schedule and reminder system
* Patient medicine tracking
* Family/Caregiver monitoring
* MySQL database integration
* Entity Framework Core migrations
* Background service for reminder processing
* RESTful APIs

## User Roles

### Admin

* Manage users
* Manage patients and caregivers
* Manage system data

### Patient

* Manage personal profile
* Add and manage medicines
* Set medicine schedules
* Track medicine reminders

### Caregiver

* Monitor assigned patients
* View patient medicine schedules
* Monitor medicine-related activities

## Technology Stack

### Backend

* C#
* ASP.NET Core Web API
* .NET 10
* Entity Framework Core
* REST API
* JWT Authentication

### Database

* MySQL
* Entity Framework Core Migrations
* Pomelo.EntityFrameworkCore.MySql

### Development Tools

* Visual Studio Code
* Postman
* Git & GitHub

## Project Architecture

```text
Client / Postman
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
```

A background service is also used for processing medicine reminders.

```text
Medicine Schedule
       |
       v
Background Service
       |
       v
Check Reminder Time
       |
       v
Generate Reminder / Notification
```

## Main Modules

```text
MediCare
│
├── Controllers
│   ├── AuthController
│   ├── PatientController
│   ├── CaregiverController
│   └── MedicineController
│
├── Models
│   ├── User
│   ├── Patient
│   ├── Caregiver
│   ├── CaregiverPatient
│   └── Medicine
│
├── Data
│   └── AppDbContext
│
├── Services
│
├── Repositories
│
├── DTOs
│
├── Migrations
│
└── Program.cs
```

## Authentication

MediCare uses **JWT (JSON Web Token)** authentication.

After successful login, the API generates a JWT token containing information such as:

* User ID
* User role
* Token expiry time

The token is then used to access protected APIs.

Example:

```text
Login
  ↓
Username / Email + Password
  ↓
Authentication
  ↓
JWT Token
  ↓
Authorization
  ↓
Protected API
```

## Database Design

The application uses MySQL as its database.

Main entities include:

* User
* Patient
* Caregiver
* CaregiverPatient
* Medicine
* Medicine Schedule
* Reminder

Relationships are managed using **Entity Framework Core**.

Database changes are handled through migrations instead of manually creating database tables.

## Entity Framework Core Migration

Example commands:

```powershell
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Migrations allow the database structure to be updated whenever the application's models change.

## Getting Started

### 1. Clone the Repository

```powershell
git clone https://github.com/Gourav-93/MediCare.git
```

### 2. Open the Project

```powershell
cd MediCare
```

### 3. Restore Dependencies

```powershell
dotnet restore
```

### 4. Configure MySQL

Update the connection string in:

```text
appsettings.json
```

Example:

```json
"ConnectionStrings": {
  "DefaultConnection": "server=localhost;database=MediCareDb;user=root;password=YOUR_PASSWORD;"
}
```

### 5. Apply Database Migration

```powershell
dotnet ef database update
```

### 6. Run the Project

```powershell
dotnet run
```

The API can then be tested using **Postman** or another API client.

## API Testing

The APIs can be tested using Postman.

Typical flow:

```text
Register
   ↓
Login
   ↓
Get JWT Token
   ↓
Add Token in Authorization Header
   ↓
Access Protected APIs
```

Authorization header:

```text
Authorization: Bearer <JWT_TOKEN>
```

## Security

The project uses:

* JWT Authentication
* Role-based Authorization
* Password hashing
* Protected API endpoints
* User-specific data access

## Future Improvements

Possible future enhancements:

* Push notifications
* SMS/Email medicine reminders
* Mobile application
* Doctor integration
* Medicine history and reports
* Emergency alerts
* Advanced caregiver dashboard
* Medicine stock tracking
* Cloud deployment

## Project Goal

The main goal of MediCare is to solve the real-world problem of **missed medicines and lack of family monitoring**.

The system connects patients with their caregivers and provides a centralized platform for managing medicine schedules and monitoring important medicine-related activities.

## Developer

**Gourav Khore**

**Role:** Software / Backend Developer

**Technologies:** C#, ASP.NET Core, .NET, Entity Framework Core, MySQL, REST API, JWT

---

### Status

🚧 **Currently under development**

The core backend structure, authentication, patient/caregiver modules and database relationships are being developed step by step.
