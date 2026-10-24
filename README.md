# MediCare — Smart Medicine Reminder & Family Monitoring System

A smart medicine management and family-monitoring application designed to help patients follow their medicine schedules and help linked caregivers stay informed when support may be needed.

**Repository:** [Gourav-93/MediCare-Smart-Medicine-Monitoring-System](https://github.com/Gourav-93/MediCare-Smart-Medicine-Monitoring-System)

**Project status:** Completed project  
**Backend:** ASP.NET Core Web API  
**Language:** C#  
**Framework:** .NET 10  
**Database:** MySQL  
**ORM:** Entity Framework Core  
**Database provider:** Pomelo.EntityFrameworkCore.MySql  
**Authentication:** JWT Bearer Authentication  
**API style:** REST  
**API testing:** Postman  
**Architecture:** Layered architecture with controllers, services, repositories, data access, and background processing

> Important: This README describes the project and its intended application workflows. Configure local connection strings, email credentials, JWT settings, and any environment-specific values before running it on another machine.

# Table of Contents

1. [Project Overview](#project-overview)
2. [Problem Statement](#problem-statement)
3. [Project Objectives](#project-objectives)
4. [Key Features](#key-features)
5. [User Roles](#user-roles)
6. [System Architecture](#system-architecture)
7. [Technology Stack](#technology-stack)
8. [Core Modules](#core-modules)
9. [Medicine Reminder Workflow](#medicine-reminder-workflow)
10. [Missed Dose and Caregiver Alerts](#missed-dose-and-caregiver-alerts)
11. [Caregiver–Patient Management](#caregiverpatient-management)
12. [Database Design](#database-design)
13. [Authentication and Authorization](#authentication-and-authorization)
14. [Project Structure](#project-structure)
15. [Prerequisites](#prerequisites)
16. [Installation and Setup](#installation-and-setup)
17. [Configuration](#configuration)
18. [Database Migrations](#database-migrations)
19. [Running the Application](#running-the-application)
20. [Testing with Postman](#testing-with-postman)
21. [Example User Journeys](#example-user-journeys)
22. [Validation and Error Handling](#validation-and-error-handling)
23. [Security Considerations](#security-considerations)
24. [Logging and Troubleshooting](#logging-and-troubleshooting)
25. [Testing Checklist](#testing-checklist)
26. [Limitations and Future Enhancements](#limitations-and-future-enhancements)
27. [Deployment Notes](#deployment-notes)
28. [Viva and Interview Preparation](#viva-and-interview-preparation)
29. [Contribution Guidelines](#contribution-guidelines)
30. [License](#license)

# Project Overview

MediCare is a Smart Medicine Reminder and Family Monitoring System. It focuses on two connected needs: helping a patient remember prescribed medicines and allowing an authorized caregiver to support the patient when a dose is missed or attention is required.

The backend is implemented as an ASP.NET Core Web API using C#. It exposes REST endpoints to the client application, applies authentication and authorization rules, processes business logic through services, accesses data through repositories and Entity Framework Core, and stores application records in MySQL.

The reminder workflow includes scheduled background processing. Rather than depending only on a patient opening a page, a hosted background service can periodically inspect medicine schedules and process reminders according to the configured rules.

The system also supports relationships between patients and caregivers. A caregiver must be linked to a patient before accessing that patient's caregiver-specific information. This relationship is represented in the database through a linking entity.

MediCare is intended as a supportive medicine-management tool. It is not a substitute for a doctor's advice, a pharmacist's instructions, or emergency medical services.

# Problem Statement

People may forget to take medicine at the intended time, particularly when they follow multiple schedules or need assistance from family members. A missed dose can also leave a caregiver unaware that the patient may need a reminder or check-in.

A simple alarm can notify a person, but it does not necessarily provide a shared view of medicine schedules, dose history, stock information, and caregiver relationships. MediCare brings these functions into one application workflow.

The project addresses the following problems:

- Patients may forget a scheduled dose.
- Patients may need a central place to maintain medicine details and schedules.
- Family members may need a way to monitor linked patients with permission.
- Caregivers may need to know when a dose remains unconfirmed after its scheduled time.
- Medicine stock may need to be tracked to help prevent running out unexpectedly.
- Application users need protected access to personal records.
- Reminder processing should continue on the server while the application is running.
- A record of dose-related events can help users review adherence history.

# Project Objectives

- Build a backend using ASP.NET Core Web API and C#.
- Store application data in MySQL.
- Use Entity Framework Core for object-relational mapping and database migrations.
- Separate API endpoints, business rules, and database access into maintainable layers.
- Provide JWT-based authentication for protected API requests.
- Support role-aware access for Admin, Patient, and Caregiver users.
- Represent patients and caregivers as related but distinct application entities.
- Allow medicines and schedules to be associated with the correct patient.
- Process scheduled reminders through a hosted background service.
- Track medicine-related events and dose outcomes where supported by the application workflow.
- Notify users about reminders and relevant missed-dose situations using the configured notification channel.
- Support caregiver monitoring only for linked patients.
- Make the backend testable through REST requests and Postman.
- Keep the codebase understandable and extensible for future improvements.

# Key Features

### User accounts and roles
The application distinguishes Admin, Patient, and Caregiver roles. Authenticated requests can include a JWT Bearer token, allowing the backend to identify the caller and apply authorization rules.

### Patient management
Patient profile data is stored separately from the general user account. The patient profile can contain patient-specific information such as date of birth and emergency contact details.
heiqfhiehfiehfi
### Caregiver management
Caregiver profile data is associated with a user account. A caregiver can be connected to one or more patients through the caregiver-patient relationship.

### Caregiver–patient linking
The relationship table records which caregiver is connected to which patient. This association is important for limiting caregiver access to authorized patient records.

### Medicine management
Medicine records represent medicines maintained by a patient. Medicine information can be used by schedules, reminders, history records, and stock-related workflows.

### Medicine schedules
Schedules represent the planned timing or frequency for taking a medicine. The reminder processor uses the schedule data to decide when a reminder should be considered due.

### Background reminder processing
A hosted background service checks relevant schedules periodically. It uses a dependency-injection scope when it needs scoped services such as the Entity Framework Core database context.

### Dose status and history
The workflow can distinguish between a dose being taken, remaining within a grace period, and being treated as missed according to configured rules. Dose-related events can be stored for history and monitoring.

### Email notifications
Email notification functionality is part of the project workflow. The actual delivery depends on valid email configuration and a reachable mail provider.

### Caregiver notifications
A linked caregiver can be notified when a patient's dose remains missed beyond the configured threshold. The project workflow discussed includes a 30-minute missed-dose escalation rule; confirm the current configuration in the source before changing this interval.

### Medicine stock monitoring
Medicine stock-related information can be used to help identify low-stock situations and notify the appropriate user where that workflow is enabled.

### Emergency-related records
The project data model includes emergency-alert functionality. Emergency information should be used carefully and must not be represented as a replacement for contacting local emergency services.

### REST API
The backend exposes endpoints for client applications to send requests and receive structured responses.

### Database migrations
Entity Framework Core migrations provide a repeatable way to create or update the schema from the code model.

### Postman testing
Postman can be used to verify authentication, protected endpoints, CRUD workflows, request validation, and reminder-related behavior.

# User Roles

### Admin
The Admin role is intended for administrative operations supported by the API. Administrative permissions should be explicitly enforced on the server rather than relying on the user interface to hide controls.

Typical responsibilities may include:
- Managing permitted application records.
- Reviewing system-level data where an authorized endpoint exists.
- Supporting user and role administration where implemented.
- Diagnosing configuration or operational issues.

### Patient
The Patient role represents the person who manages their own medicine routine.

Typical responsibilities include:
- Viewing and maintaining their patient profile.
- Maintaining their medicine list.
- Creating or updating medicine schedules where permitted.
- Reviewing dose history and reminders.
- Responding to reminders and recording a dose outcome.
- Reviewing stock-related information where available.
- Managing caregiver relationships through the supported workflow.

### Caregiver
The Caregiver role represents a trusted person connected to a patient.

Typical responsibilities include:
- Viewing patients linked to that caregiver.
- Reviewing permitted medicine and history information for linked patients.
- Helping manage medicines or schedules when the application grants that permission.
- Receiving relevant missed-dose or stock-related alerts.
- Reviewing information needed to support the patient.

### Authorization principle
A role alone should not be treated as proof that a caregiver may access every patient. The backend should check both the user's role and the specific caregiver-patient relationship for patient-specific operations.

# System Architecture

The project follows a layered backend design so that HTTP handling, business rules, and persistence concerns do not all live in one place.

```text
Client / Frontend / Postman
            |
            v
ASP.NET Core Web API
            |
            v
Controllers
            |
            v
Services and Business Rules
            |
            v
Repository Interfaces and Implementations
            |
            v
Entity Framework Core / AppDbContext
            |
            v
MySQL Database

Hosted Background Service
            |
            v
Creates a scope and uses application services / database access
            |
            v
Checks schedules and processes due reminders
            |
            v
Reminder records and configured notifications
```

### Client layer
The client sends HTTP requests to the API. It may be a web frontend, a mobile client, or a testing tool such as Postman.

### Controller layer
Controllers receive HTTP requests, bind request data, call the appropriate services, and return HTTP responses. Controllers should not contain all business rules or large database queries.

### Service layer
Services implement application rules, coordinate operations, and provide a place for reusable business logic.

### Repository layer
Repositories encapsulate data-access operations and help separate persistence details from higher-level business rules.

### Data layer
The data layer contains the Entity Framework Core database context and entity configuration used to map C# models to MySQL tables.

### Background service
The reminder background service runs as part of the ASP.NET Core host. It periodically checks relevant data and processes due reminder logic without requiring a browser request to trigger every check.

### Database
MySQL stores user accounts, profiles, caregiver relationships, medicines, schedules, notifications, and other supported records.

# Technology Stack

| Technology | Purpose |
|---|---|
| C# | Main backend programming language |
| .NET 10 | Runtime and development platform used by the current project |
| ASP.NET Core Web API | REST API and hosting framework |
| Entity Framework Core | Object-relational mapping and data access |
| Pomelo.EntityFrameworkCore.MySql | MySQL provider for EF Core |
| MySQL | Relational database |
| JWT Bearer Authentication | Authentication token handling for protected API requests |
| Dependency Injection | Supplies services and dependencies to application components |
| Hosted BackgroundService | Periodic server-side reminder processing |
| REST / HTTP | Communication between client and backend |
| JSON | Typical request and response data format |
| Postman | Manual API testing |
| Git and GitHub | Version control and source-code hosting |

Package versions should be checked against the actual project file. EF Core, the MySQL provider, and the target framework should use compatible versions. If a local build reports package compatibility warnings, resolve those warnings before treating the setup as production-ready.

# Core Modules

### Authentication module
Handles login-related workflows and the creation or validation of authentication tokens according to the implementation.

### User module
Stores common account information such as a user's name, email, password hash, and role.

### Patient module
Stores patient-specific profile information and connects it to the corresponding user account.

### Caregiver module
Stores caregiver-specific profile information and connects it to the corresponding user account.

### Caregiver–patient module
Represents the many-to-many relationship between caregivers and patients. The linking record may also store the relationship type.

### Medicine module
Stores medicine information associated with a patient.

### Schedule module
Stores the timing information used by the reminder workflow.

### Reminder module
Represents reminder-related data and status information generated by the schedule workflow.

### Medicine log / history module
Stores dose-related events so the application can show a history where supported.

### Notification module
Stores notification information for users and can be used alongside configured email delivery.

### Emergency alert module
Stores emergency-related records for the relevant patient workflow.

### Background processing module
Periodically evaluates schedules and initiates reminder-related operations.

### Shared helper and middleware code
Helpers can provide reusable functions, while middleware can handle cross-cutting HTTP concerns such as exception handling where configured.

# Medicine Reminder Workflow

The reminder workflow is one of the central functions of MediCare.

1. A patient or an authorized caregiver maintains a medicine record.
2. The medicine is associated with a patient.
3. A schedule records when the medicine should be taken.
4. The hosted background service runs periodically while the application is running.
5. The service creates a scope for scoped dependencies.
6. The reminder logic reads the schedule and related records.
7. When a schedule is due, the application processes the reminder according to its configured rules.
8. A reminder or notification record may be created or updated.
9. The patient can review the reminder and record the dose outcome through the supported API.
10. The resulting status can be used in dose history and caregiver monitoring.
11. If a dose remains unconfirmed past the configured threshold, the missed-dose workflow can run.
12. The service logs useful operational details and errors through the configured logging system.

### Background processing considerations
- The application host must remain running for in-process background processing to execute.
- A hosted service is not a separate cloud scheduler; it runs with the ASP.NET Core application.
- The service should use a fresh dependency-injection scope for scoped database services.
- Reminder processing should be designed to avoid generating duplicate reminders on every polling cycle.
- Time-zone assumptions should be consistent between stored schedules and server-side comparisons.
- Restarting the application should not silently lose important reminder state; persistent records should be used for state that must survive restarts.
- Production deployments with multiple application instances need a strategy to prevent duplicate processing.

# Missed Dose and Caregiver Alerts

MediCare is designed to help a patient respond to a scheduled dose and to help a linked caregiver become aware of a missed dose when escalation is configured.

### Typical sequence
1. A medicine schedule reaches its planned time.
2. The system processes the reminder.
3. The patient is given an opportunity to take the medicine and record the outcome.
4. The application evaluates the dose status according to its configured timing rules.
5. A dose may remain within a grace period before being classified as missed.
6. When the configured missed-dose condition is met, the system can record the missed status.
7. The system identifies caregivers linked to that patient.
8. The system sends or queues the relevant caregiver notification using the configured notification implementation.
9. The event can be retained for history and review.

### Thirty-minute escalation rule
The project's recent workflow discussion specified that if a patient has still not taken the medicine 30 minutes after the missed-dose threshold, a message should be sent to the caregiver linked to that patient. Treat this as a configurable project rule and verify the actual code and settings before relying on the exact interval.

### Important reliability points
- A caregiver notification must be associated with the correct patient.
- Only caregivers linked to that patient should be considered recipients.
- The same missed dose should not repeatedly generate duplicate escalation emails.
- A dose marked as taken should not continue to trigger a stale missed-dose escalation.
- Failed email delivery should be logged and, if required, retried safely.
- Email delivery depends on valid SMTP or mail-provider credentials and network access.
- Notification wording should not expose unnecessary sensitive medical information.

# Caregiver–Patient Management

Caregiver–patient management allows the application to represent a trusted support relationship.

### Relationship model
A caregiver and a patient are separate profile types. The linking entity joins them. This allows one caregiver to be linked to multiple patients and a patient to have multiple caregivers, depending on the application's validation rules.

### Expected workflow
1. An authenticated user opens the caregiver or patient relationship workflow.
2. The backend validates the caller and the requested relationship.
3. The backend confirms that the referenced caregiver and patient exist.
4. The backend checks that the relationship is allowed and does not already exist.
5. The relationship record is created.
6. Later caregiver requests verify that the relationship exists before returning patient-specific data.
7. Removing a relationship should stop future caregiver access through that relationship.
8. The system should preserve other valid relationships when one link is removed.

### Authorization checks
The backend should never rely only on a patient ID supplied by the client. For caregiver requests, it should confirm that the authenticated caregiver owns the caregiver profile and is linked to the requested patient.

### Caregiver access
Where the project allows caregivers to view medicine information or manage medicines for linked patients, the API should enforce the intended permissions on every relevant endpoint.

### Privacy
Caregiver access should be limited to information needed for the support relationship. Sensitive information should not be exposed to unrelated users or caregivers.

# Database Design

The application uses MySQL for persistent storage and Entity Framework Core for mapping C# entities to database tables.

The model names below reflect the project structure discussed during development. Exact column names, indexes, nullability, and foreign-key names should be confirmed in the current entity classes and migrations.

### User
Represents a common login account.

Common fields may include:
- Id
- Name
- Email
- PasswordHash
- Role

A user account may be associated with a patient profile or a caregiver profile.

### Patient
Represents patient-specific information.

Fields discussed in the project include:
- Id
- UserId
- DateOfBirth
- EmergencyContact

The UserId relationship connects the patient profile to its account.

### Caregiver
Represents caregiver-specific information.

Fields discussed in the project include:
- Id
- UserId
- Relationship

The caregiver profile is connected to a user account.

### CaregiverPatient
Represents the link between a caregiver and a patient.

Fields may include:
- Id
- CaregiverId
- PatientId
- RelationType

This entity supports the many-to-many relationship between caregivers and patients.

### Medicine
Represents a medicine associated with a patient. The exact fields should be taken from the current Medicine model and migrations.

### Medicine schedule
Represents planned medicine timing and related schedule configuration.

### Medicine log
Represents dose-related events, such as taking a dose or recording a missed dose, where implemented.

### Reminder
Represents reminder records generated for scheduled doses.

### Notification
Represents user-facing notification information.

### Emergency alert
Represents emergency-related records for a patient workflow.

### Relationships overview
- User to Patient: one-to-one profile relationship where configured.
- User to Caregiver: one-to-one profile relationship where configured.
- Caregiver to Patient: many-to-many through CaregiverPatient.
- Patient to Medicine: one-to-many.
- Medicine to Schedule: one-to-many where configured.
- Medicine to MedicineLog: one-to-many where configured.
- User to Notification: one-to-many where configured.
- Patient to EmergencyAlert: one-to-many where configured.

Entity Framework Core migrations are the source of truth for the exact database schema created by the current code.



# Authentication and Authorization

MediCare uses JWT Bearer Authentication for protected API requests.

### Authentication concept
Authentication answers: “Who is making this request?” After successful login, the API can issue a signed token that the client sends with later requests.

### Authorization concept
Authorization answers: “Is this authenticated user allowed to perform this action?” A valid token does not automatically grant access to every patient, medicine, or caregiver record.

### Typical client request
A client includes a header similar to the following when calling a protected endpoint:

```http
Authorization: Bearer YOUR_ACCESS_TOKEN
```

Replace `YOUR_ACCESS_TOKEN` with a real token returned by the application's authentication flow.

### JWT contents
The project's token service is intended to include identity and role information needed by the API, with an expiry. The actual claims must be checked in the token-generation implementation.

### Role-aware access
- Admin access should be limited to endpoints explicitly authorized for administrators.
- Patient access should be restricted to the authenticated patient's own records where applicable.
- Caregiver access should be restricted to linked patients and permitted actions.
- Anonymous access should be limited to endpoints intentionally made public, such as a login or registration endpoint if configured.

### Token safety
- Never commit real JWT signing keys to a public repository.
- Use a long, random secret stored in environment variables or a secret manager.
- Set a suitable token expiry.
- Validate issuer and audience if the application uses those settings.
- Use HTTPS in deployed environments.
- Do not store plaintext passwords.
- Avoid placing access tokens in screenshots, logs, or public issue reports.

# Project Structure

The exact file list can change as the project evolves. The following is a guide to the main folders used by the backend.

```text
MediCare/
├── Controllers/
│   └── API controllers that receive HTTP requests
├── Models/
│   └── Entity classes representing application data
├── DTOs/
│   └── Request and response models used by API endpoints
├── Data/
│   └── AppDbContext and database-related configuration
├── Repositories/
│   ├── Data-access implementations
│   └── Interfaces/
│       └── Repository contracts
├── Services/
│   ├── Business logic implementations
│   └── Interfaces/
│       └── Service contracts
├── BackgroundServices/
│   └── Hosted background reminder processing
├── Helpers/
│   └── Shared utility code
├── Middleware/
│   └── Cross-cutting HTTP pipeline behavior, if present
├── Migrations/
│   └── EF Core database migration files
├── Program.cs
│   └── Application startup and dependency registration
├── appsettings.json
│   └── Non-secret default application configuration
└── MediCare.csproj
    └── Target framework and package references
```

### Controllers
Keep controllers focused on request handling and HTTP responses.

### Models
Models describe the data and relationships stored by the application.

### DTOs
DTOs allow the API to accept and return the intended fields without exposing database entities directly in every response.

### Data
The database context coordinates Entity Framework Core operations and maps entities to database tables.

### Repositories
Repository classes contain reusable database operations.

### Services
Service classes coordinate workflows and implement business rules.

### BackgroundServices
Hosted services handle periodic work such as checking medicine schedules.

### Helpers
Helpers contain small reusable operations that do not belong to a specific controller.

### Program.cs
Program.cs configures the web host, dependency injection, database provider, middleware, authentication, authorization, and endpoint mapping.

### Migrations
Migrations describe changes to the database schema over time.

# Prerequisites

Before running the project on a new machine, install the following tools:

- .NET SDK compatible with the project's target framework.
- MySQL Server.
- A MySQL client such as MySQL Workbench or the MySQL command-line client.
- Git, if cloning the repository.
- Postman for manual API testing.
- An editor such as Visual Studio or Visual Studio Code.

### Verify .NET
Run:

```bash
dotnet --info
dotnet --list-sdks
```

Confirm that an SDK compatible with the project's target framework is installed.

### Verify MySQL
Confirm that the MySQL service is running and that you know the local database username and password.

### Verify the repository
After cloning, open the project folder and check the `.csproj` file to confirm the actual target framework and package versions.

### Version compatibility
The EF Core runtime, EF Core design package, and MySQL provider must be compatible with one another. If `dotnet restore` reports package downgrade or compatibility warnings, investigate them rather than ignoring them.

# Installation and Setup

If the solution or API project is in a nested folder, change to the directory containing `MediCare.csproj`.

### 3. Restore dependencies

```bash
dotnet restore
```

This downloads the NuGet packages referenced by the project.

### 4. Review the project file

Open `MediCare.csproj` and confirm:
- The target framework.
- Entity Framework Core package versions.
- The MySQL provider package.
- The JWT Bearer package.
- Any email or OpenAPI packages used by the project.

### 5. Configure the database

Set the MySQL connection string using local configuration or environment variables. Do not commit real passwords.

### 6. Configure JWT settings

Provide the signing key and any issuer, audience, or expiry settings expected by the token service.

### 7. Configure email settings

If email reminders are enabled, provide the mail host, port, sender address, authentication details, and TLS settings required by the configured provider.

### 8. Apply database migrations

Run the EF Core database update command from the directory containing the project file, or specify the project path if necessary.

```bash
dotnet ef database update
```

If the EF CLI tool is not installed, install or invoke a compatible local tool according to your development setup.

### 9. Start the API

```bash
dotnet run
```

Read the terminal output to identify the local HTTP or HTTPS address.

### 10. Test the API

Use Postman to call the available authentication endpoint first, then send the returned token with protected requests.

# Configuration

Configuration names differ between projects, so compare these examples with the settings read by the current source code.

### Connection string example

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;port=3306;database=MediCareDb;user=YOUR_DB_USER;password=YOUR_DB_PASSWORD;"
  }
}
```

The database name and connection-string key above are examples. Use the exact key and database name expected by the current `Program.cs` and `AppDbContext` setup.

### JWT configuration example

```json
{
  "Jwt": {
    "Key": "REPLACE_WITH_A_LONG_RANDOM_SECRET",
    "Issuer": "MediCare",
    "Audience": "MediCareClient",
    "ExpiryMinutes": 60
  }
}
```

These key names are illustrative. The `JwtService` must read the same keys for the settings to take effect.

### Email configuration example

```json
{
  "EmailSettings": {
    "Host": "YOUR_SMTP_HOST",
    "Port": 587,
    "SenderEmail": "YOUR_SENDER_EMAIL",
    "SenderName": "MediCare",
    "Username": "YOUR_SMTP_USERNAME",
    "Password": "YOUR_SMTP_PASSWORD",
    "EnableSsl": true
  }
}
```

Use the property names expected by the project's email service. Do not paste live credentials into README examples or commit them to Git.

### Environment variables
For local or production environments, prefer environment variables or a secret manager for secrets. If the app uses ASP.NET Core's standard configuration provider, environment variables can override many settings using double underscores between nested key names.

### Configuration checklist
- MySQL host is reachable.
- Database credentials are valid.
- Database name matches the intended environment.
- JWT signing secret is present.
- Token validation settings match token-generation settings.
- Email settings are correct if email is enabled.
- The application is configured for the intended environment.
- No production secret is stored in source control.

# Database Migrations

Entity Framework Core migrations keep the database schema aligned with the model classes.

### Apply existing migrations

```bash
dotnet ef database update
```

### List migrations

```bash
dotnet ef migrations list
```

### Add a migration
Only add a migration after intentionally changing the model and reviewing the resulting schema difference.

```bash
dotnet ef migrations add DescribeYourChange
```

### Update the database after a model change

```bash
dotnet ef database update
```

### Migration safety
- Review generated migration code before applying it to important data.
- Back up a database before a production schema change.
- Do not manually delete migration files to fix a schema mismatch.
- Keep migration files under version control.
- Make sure the startup project and target project are correct when the solution contains multiple projects.
- Verify that the connection string points to the intended database.
- Do not run destructive migration commands against production without a recovery plan.

# Running the Application

### Development mode
Run the API from the project directory:

```bash
dotnet run
```

The console output displays the URLs on which the host is listening.

### Watch mode
During development, the following command can restart the app when source files change:

```bash
dotnet watch run
```

### Swagger / OpenAPI
If OpenAPI or Swagger is configured and enabled for the current environment, open the URL shown by the project or its startup output. If the Swagger page is unavailable, the API can still be tested with Postman as long as the server starts and the routes are known.

### Background service
The reminder service runs as part of the application host when registered and enabled. Closing the terminal or stopping the process stops in-process reminder polling.

### Production hosting
For production, host the API using a suitable process manager or hosting platform, configure HTTPS, set secrets outside source control, and monitor application logs and email delivery.

# Testing with Postman

Postman is useful for verifying the API without relying on the frontend.

### Recommended testing sequence
1. Start MySQL.
2. Start the MediCare API.
3. Confirm the server starts without an unhandled exception.
4. Call the configured registration endpoint if registration is available.
5. Call the login endpoint with a valid account.
6. Copy the access token from the response.
7. Add the token to Postman's Authorization tab using Bearer Token.
8. Call the patient profile endpoints.
9. Call the medicine endpoints.
10. Create or inspect medicine schedules.
11. Verify the caregiver-patient linking workflow.
12. Test that a caregiver can access a linked patient only.
13. Test reminder-related endpoints, if exposed.
14. Test a valid dose outcome.
15. Test a missed-dose scenario using safe test data and the configured timing rules.
16. Check application logs for background-service activity.
17. Confirm notification records or email delivery where configured.
18. Test invalid input and unauthorized requests.
19. Verify that one user's data is not returned to another unrelated user.
20. Repeat tests after restarting the application to check persisted state.

### Bearer token setup
In Postman:
- Select the request.
- Open the Authorization tab.
- Choose Bearer Token.
- Paste the token into the token field.
- Send the request.

### Useful test cases
- A valid login succeeds.
- An invalid password is rejected.
- A request without a token cannot access protected data.
- An expired or invalid token is rejected.
- A patient cannot read another patient's private data.
- An unlinked caregiver cannot read a patient's private data.
- A linked caregiver can read only the information allowed by the API.
- Invalid medicine data is rejected.
- A valid medicine schedule can be saved.
- A dose outcome updates the expected record.
- A reminder does not create duplicate records on every background-service poll.
- Email failures are visible in logs.

# Example User Journeys

### Patient adds a medicine
1. The patient signs in.
2. The client sends the access token with protected requests.
3. The patient opens medicine management.
4. The client submits the medicine details.
5. The API validates the request.
6. The service verifies the patient context.
7. The repository saves the record.
8. The API returns an appropriate response.
9. The patient can see the saved medicine in later requests.

### Patient sets a schedule
1. The patient selects a medicine.
2. The client submits the schedule information.
3. The backend verifies that the medicine belongs to the patient or that the caller has permission to manage it.
4. The service validates required schedule data.
5. The repository saves the schedule.
6. The background service can evaluate the schedule during later polling cycles.

### Caregiver links to a patient
1. The relationship workflow receives caregiver and patient identifiers.
2. The backend validates the caller.
3. The backend verifies that both records exist.
4. The backend checks that the link is allowed and not duplicated.
5. The link is stored in the relationship table.
6. Later caregiver requests can use this relationship for authorization.

### Patient records a dose
1. The patient opens a due reminder.
2. The patient records the dose outcome.
3. The API validates the request.
4. The service updates the appropriate reminder or history record.
5. The updated status is returned to the client.
6. The missed-dose workflow should respect the updated status.

### Caregiver receives a missed-dose alert
1. A scheduled dose remains unresolved.
2. Background processing evaluates the configured grace and missed-dose rules.
3. The system identifies linked caregivers.
4. The configured notification workflow creates or sends the alert.
5. The event can be logged and retained for review.
6. If email fails, logs should help diagnose the failure.

# Validation and Error Handling

Input validation helps keep data consistent and makes API behavior predictable.

### Recommended validation
- Required fields should not be empty.
- Email addresses should follow the application's expected format.
- User roles should be restricted to recognized values.
- Referenced users, patients, caregivers, medicines, and schedules should exist.
- A medicine schedule should contain valid timing information.
- Dates and times should be interpreted consistently.
- A caregiver relationship should not be duplicated.
- Dose outcome requests should reference valid records.
- A client should not be allowed to overwrite protected identity fields without permission.
- Sensitive properties such as password hashes should not be returned in API responses.

### HTTP response expectations
The exact status codes depend on the controllers. Common REST conventions include:
- `200 OK` for a successful read or update with a response body.
- `201 Created` when a new resource is created.
- `204 No Content` for a successful operation with no response body.
- `400 Bad Request` for invalid input.
- `401 Unauthorized` when authentication is missing or invalid.
- `403 Forbidden` when the caller is authenticated but lacks permission.
- `404 Not Found` when a requested resource does not exist or is intentionally concealed.
- `409 Conflict` when a request conflicts with current data, such as a duplicate relationship.
- `500 Internal Server Error` for unexpected server failures.

### Exception handling
Where global exception middleware is configured, it can provide consistent error responses and log unexpected exceptions. Do not return stack traces or secrets to clients in production.

# Security Considerations

Medicine and patient data should be treated as sensitive information.

- Store password hashes rather than plaintext passwords.
- Keep JWT signing keys out of source control.
- Keep database credentials out of source control.
- Use HTTPS outside local development.
- Validate all incoming request data.
- Enforce authorization on the server for every protected operation.
- Check ownership before allowing a patient to read or change their records.
- Check caregiver-patient links before caregiver access.
- Avoid returning password hashes or internal security data.
- Do not log access tokens, passwords, SMTP passwords, or unnecessary medical details.
- Apply least-privilege database permissions.
- Restrict CORS to trusted client origins in deployed environments.
- Rotate compromised secrets.
- Keep dependencies updated and review security advisories.
- Use safe error messages for clients and detailed, protected logs for operators.
- Do not treat a successful frontend check as a replacement for backend authorization.
- Back up important data and protect backup files.
- Define retention and deletion policies appropriate to the deployment.
- Obtain appropriate consent for caregiver monitoring and notifications.
- Make it clear to users what information caregivers can see.
- Do not describe reminder messages as medical diagnosis or clinical advice.

# Logging and Troubleshooting

### API does not start
- Read the first error in the terminal.
- Confirm the installed SDK supports the target framework.
- Run `dotnet restore`.
- Check for compilation errors.
- Confirm the configured port is not already in use.
- Verify that configuration files are valid JSON.

### Database connection fails
- Confirm the MySQL service is running.
- Check host, port, database name, username, and password.
- Confirm the database user has the necessary permissions.
- Confirm the application reads the same connection-string key that you configured.
- Apply migrations to the intended database.
- Check that the MySQL provider version is compatible with EF Core.

### Migration command fails
- Run the command from the project directory or specify the project.
- Confirm the EF Core tools are available.
- Confirm the startup project can load its configuration.
- Read the full exception rather than only the final line.
- Do not create a duplicate migration name to work around a failed update.

### JWT authentication fails
- Confirm the login endpoint returns a token.
- Confirm the client sends `Authorization: Bearer <token>`.
- Confirm the token has not expired.
- Confirm signing and validation settings match.
- Confirm the API's authentication middleware is configured in the request pipeline.
- Check whether the endpoint requires a specific role.

### Email is not received
- Confirm the email workflow is triggered.
- Check sender, recipient, SMTP host, port, and authentication settings.
- Check spam or junk folders.
- Check whether the provider requires an app password or other authentication method.
- Check application logs for connection or authentication errors.
- Confirm the caregiver is linked to the patient.
- Confirm the missed-dose threshold was actually reached.
- Confirm the same alert was not already processed by duplicate-prevention logic.

### Reminder is not generated
- Confirm the background service is registered.
- Keep the API process running during the test.
- Confirm a valid schedule exists.
- Confirm the schedule time and time zone.
- Check whether the schedule is active.
- Check logs for database errors.
- Confirm the background service's polling interval and due-time comparison.
- Verify that the reminder is not already marked as processed.

### Data does not appear
- Confirm the request is sent to the correct API URL.
- Confirm the token belongs to the expected user.
- Confirm the data was saved to the expected database.
- Check filters, pagination, and ownership conditions.
- Check whether the frontend is expecting different JSON property names.
- Inspect the response body and server logs.

# Testing Checklist

Use this checklist when validating a local build or preparing a demonstration.

### Environment
- [ ] .NET SDK is installed.
- [ ] MySQL is running.
- [ ] Connection string is configured locally.
- [ ] JWT settings are configured.
- [ ] Email settings are configured if email notifications are being demonstrated.
- [ ] NuGet restore completes.
- [ ] Database migrations apply successfully.
- [ ] API starts without a fatal exception.

### Authentication
- [ ] Valid login returns the expected response.
- [ ] Invalid credentials are rejected.
- [ ] Protected endpoints reject requests without a token.
- [ ] Invalid or expired tokens are rejected.
- [ ] Role restrictions are enforced by the API.

### Patient
- [ ] Patient profile can be retrieved by an authorized user.
- [ ] Patient profile updates validate input.
- [ ] A patient cannot access another patient's private data.

### Caregiver
- [ ] Caregiver profile can be retrieved by an authorized user.
- [ ] A caregiver can see linked patients within allowed permissions.
- [ ] An unlinked caregiver cannot access a patient's private data.
- [ ] Duplicate relationship creation is handled safely.
- [ ] Removing a relationship prevents future access through that link.

### Medicine and schedules
- [ ] Medicine records can be created with valid data.
- [ ] Medicine records can be read by an authorized user.
- [ ] Update and delete operations enforce ownership or role permissions.
- [ ] Schedule data is validated.
- [ ] Schedules remain available after application restart.

### Reminders and notifications
- [ ] Background service starts with the host.
- [ ] A due schedule is detected under the configured rules.
- [ ] Reminder state is persisted as intended.
- [ ] A dose outcome is recorded correctly.
- [ ] Missed-dose processing respects the configured grace period.
- [ ] Caregiver recipients are limited to linked caregivers.
- [ ] Duplicate alerts are prevented or handled safely.
- [ ] Email errors are logged.
- [ ] Low-stock and emergency-related workflows are tested if enabled.

### Quality
- [ ] Invalid requests return useful errors.
- [ ] Logs do not expose secrets.
- [ ] README configuration examples contain placeholders only.
- [ ] Repository contains no local passwords or tokens.

# Limitations and Future Enhancements

The current project demonstrates the core architecture and medicine-reminder workflow. A production deployment would benefit from further validation, operational controls, and user testing.

Possible future enhancements include:

- Push notifications for mobile devices.
- SMS notifications through a verified provider.
- A configurable grace period per medicine.
- A user-facing notification preferences page.
- A caregiver invitation and consent workflow.
- A complete audit trail for caregiver actions.
- A calendar view for medicine schedules.
- Refill reminders based on daily dosage and current stock.
- A report showing adherence trends over time.
- Downloadable history reports for authorized users.
- Accessibility improvements and support for larger text.
- Multi-language interface support.
- Better time-zone handling for users in different regions.
- Retry policies and delivery status for email messages.
- A persistent job queue for more reliable scheduling at scale.
- Idempotency keys or unique constraints to prevent duplicate reminders.
- Health checks for database and mail-provider connectivity.
- Automated unit and integration tests.
- CI pipeline for build and test validation.
- Containerized development using Docker.
- Centralized production logging and monitoring.
- Rate limiting on sensitive endpoints.
- Refresh-token support if required by the security design.
- Account recovery and email verification.
- Data export and account deletion workflows.
- Formal security review before handling real patient data.
- Backup and restore procedures.
- A deployment guide for the selected hosting platform.

These items are suggestions, not claims that every item is already implemented.

# Deployment Notes

Before deploying MediCare beyond a local development environment:

1. Select a supported hosting environment for the ASP.NET Core API.
2. Provision a MySQL database with restricted credentials.
3. Configure all secrets through the hosting platform's secret-management facility.
4. Configure HTTPS and trusted client origins.
5. Apply database migrations through a controlled release process.
6. Confirm the background service runs continuously.
7. Decide how reminders will be processed if multiple API instances run.
8. Configure email delivery and test messages with approved recipients.
9. Enable logs and monitor background-service exceptions.
10. Set up database backups and test restoring them.
11. Use a health-check endpoint if the project provides one.
12. Review access control for patient and caregiver data.
13. Confirm that logs and analytics do not leak private information.
14. Document how to rotate database, JWT, and email secrets.
15. Test the application with non-production data before launch.

### Multiple-instance warning
An in-process `BackgroundService` may run once per application instance. If the same application is deployed to multiple instances, each instance could inspect the same schedules. Use a distributed lock, a persistent job queue, or a database-based claim/idempotency strategy before scaling reminder processing horizontally.

### Data protection
Do not use real patient information in development screenshots, sample seed data, or public bug reports. Use synthetic data for demos.

# Viva and Interview Preparation

### What is MediCare?
MediCare is a Smart Medicine Reminder and Family Monitoring System. It helps patients manage medicine schedules and supports caregiver monitoring for linked patients.

### Why did you build it?
The project addresses the common problem of forgetting scheduled medicines and the need for a trusted caregiver to be notified when a patient may need support.

### Why ASP.NET Core Web API?
ASP.NET Core provides a structured way to build REST endpoints, apply middleware, use dependency injection, and implement authentication and authorization.

### Why MySQL?
MySQL is a relational database suitable for structured data and relationships between users, patient profiles, caregivers, medicines, schedules, and logs.

### What is Entity Framework Core?
Entity Framework Core is an object-relational mapper. It lets the application work with C# entity objects and translates supported operations into database queries.

### What is a migration?
A migration is a versioned description of a database schema change generated from the model changes. It helps update the database in a repeatable way.

### What is JWT authentication?
JWT authentication uses a signed token to carry claims about an authenticated user. The client sends the token with later requests, and the API validates it.

### What is authorization?
Authorization checks whether the authenticated caller has permission to perform an action or access a resource.

### Why use roles?
Roles help separate the permissions of Admin, Patient, and Caregiver users. Resource-level checks are still needed for patient-specific data.

### What is a repository?
A repository groups data-access operations behind a clear interface. It separates persistence code from business logic.

### What is a service?
A service contains application logic and coordinates operations between controllers and data-access components.

### What is a background service?
A background service runs server-side work independently of a specific HTTP request. MediCare uses a hosted background service to inspect medicine schedules and process reminder logic.

### Why create a dependency-injection scope in a background service?
Database contexts and many repositories are scoped services. A background service is long-lived, so it should create a scope when resolving scoped dependencies rather than keeping a scoped database context for the entire service lifetime.

### What is the purpose of CaregiverPatient?
It records the relationship between a caregiver and a patient. The API can use this relationship to check whether a caregiver is allowed to access a particular patient's information.

### How do you prevent unauthorized patient access?
The backend should identify the caller from the validated token and verify patient ownership or the caregiver-patient link before returning or modifying data.

### How do reminders work?
The background service periodically checks schedules, determines which are due according to configured timing rules, and processes the corresponding reminder state and notifications.

### How does missed-dose escalation work?
When a dose remains unresolved beyond the configured grace or missed-dose threshold, the system can mark it missed and notify linked caregivers according to the configured rule.

### What is dependency injection?
Dependency injection provides a class with the services it needs instead of having it create every dependency itself. ASP.NET Core includes a built-in dependency-injection container.

### What is middleware?
Middleware is software in the HTTP request pipeline. It can handle cross-cutting concerns such as authentication, authorization, exception handling, and logging.

### What is REST?
REST is an architectural style commonly used to design HTTP APIs around resources and standard HTTP methods.

### How did you test the API?
Postman can send HTTP requests, include Bearer tokens, inspect JSON responses, and verify success and failure cases.

### What would you improve next?
Possible improvements include automated tests, stronger notification delivery guarantees, push notifications, audit logs, production monitoring, and a robust multi-instance scheduling strategy.

# Contribution Guidelines

This repository can be extended by following the existing project structure.

1. Create a feature branch for a focused change.
2. Keep controllers focused on HTTP request handling.
3. Put reusable business rules in services.
4. Put persistence operations in repositories or the established data-access layer.
5. Add validation for new request fields.
6. Update entity relationships carefully.
7. Add a migration when a model change alters the database schema.
8. Check authorization for every new endpoint.
9. Add or update tests where available.
10. Test both successful and unsuccessful requests.
11. Update this README when setup or behavior changes.
12. Never commit credentials, tokens, personal medical data, or local database backups.
13. Use clear commit messages describing the change.
14. Review the complete diff before pushing.
15. Keep package versions compatible with the project's target framework.

For bug reports, include a short description, reproduction steps, expected behavior, actual behavior, and relevant sanitized logs. Remove tokens, passwords, email credentials, and private medical information before sharing diagnostic output.

# License

Add a license file to the repository if you intend to publish the project for reuse. Until a license is included, do not assume that the repository grants general permission to copy, modify, or redistribute the code.

For an academic or portfolio project, you can include a short project description and repository link in your resume, portfolio, or demonstration materials.

# Project Summary

MediCare brings together medicine schedules, reminder processing, patient profiles, caregiver relationships, authentication, and notification workflows in an ASP.NET Core Web API backed by MySQL.

The project demonstrates practical backend development concepts:
- REST API design.
- C# and ASP.NET Core.
- Entity Framework Core and migrations.
- Relational database modeling.
- Layered architecture.
- Repository and service patterns.
- Dependency injection.
- JWT authentication.
- Role-based and resource-level authorization.
- Background processing.
- Email notification integration.
- Error handling and logging.
- API testing with Postman.
- Version control with Git and GitHub.

The most important design principle is that patient data must be protected by backend authorization. A caregiver should only receive information for patients with whom they have an authorized relationship. Reminder and email behavior should be verified using the configured schedules, thresholds, and notification settings.

**Repository:** https://github.com/Gourav-93/MediCare-Smart-Medicine-Monitoring-System

**Project:** MediCare — Smart Medicine Reminder & Family Monitoring System

# Practical Reference Appendix

This appendix is a quick reference for development, demonstration, and maintenance. Confirm exact route names and configuration keys in the current source code before using them.

## Local Development Commands

### `dotnet --info`
Displays installed .NET information.

### `dotnet --list-sdks`
Lists installed SDK versions.

### `dotnet restore`
Restores NuGet dependencies.

### `dotnet build`
Compiles the project.

### `dotnet run`
Starts the API.

### `dotnet watch run`
Runs the API with development-time file watching.

### `dotnet ef migrations list`
Lists known EF Core migrations.

### `dotnet ef database update`
Applies migrations to the configured database.

### `git status`
Shows changed files.

### `git diff`
Shows unstaged source changes.

### `git log --oneline -5`
Shows recent commits.

## HTTP and API Testing Notes

- Use GET to retrieve resources when the endpoint follows standard REST conventions.

- Use POST to submit a new resource or initiate an action when supported by the API.

- Use PUT for a full resource replacement when that convention is implemented.

- Use PATCH for partial updates only when the API supports it.

- Use DELETE to remove a resource only when the endpoint permits deletion.

- Send JSON with the correct Content-Type header for JSON request bodies.

- Attach the Bearer token to protected requests.

- Check both the HTTP status code and the response body.

- Test missing required fields.

- Test invalid identifiers.

- Test an identifier that belongs to another user.

- Test a caregiver without a patient relationship.

- Test duplicate caregiver-patient relationships.

- Test invalid role values.

- Test expired tokens.

- Test unexpected database failures in a controlled development environment.

- Do not test destructive actions against production data.

## Reminder Verification Notes

- Keep the API process running while verifying the hosted background service.

- Use a test schedule with a known time and a test patient.

- Check the server's time zone and the schedule's time zone assumptions.

- Confirm the medicine is associated with the intended patient.

- Confirm the schedule is active and has valid timing fields.

- Observe application logs for background-service cycles.

- Inspect reminder records after a due time is processed.

- Record a dose outcome and confirm that the status changes as expected.

- Use a controlled test to verify grace-period behavior.

- Use a controlled test to verify the missed-dose threshold.

- Confirm only linked caregivers are considered for notifications.

- Check notification records as well as email delivery.

- Check the spam folder when a test email is not visible in the inbox.

- Avoid repeatedly running the same missed-dose test without resetting test data.

- Verify that one schedule does not generate duplicate alerts on every polling cycle.

- Verify that a restart does not cause already-processed alerts to be sent again.

- If multiple application instances are running, verify the duplicate-processing strategy.

## Database and Data Integrity Notes

- Keep entity relationships explicit and consistent with migrations.

- Use foreign keys where appropriate.

- Use unique constraints for fields that must be unique.

- Validate identifiers before performing updates.

- Avoid deleting parent records without understanding cascade behavior.

- Use transactions when a workflow requires several writes to succeed together.

- Inspect generated migration code before applying schema changes.

- Keep a backup before a high-risk migration.

- Use synthetic data in demos.

- Do not publish real patient details.

- Do not store passwords in plain text.

- Avoid returning internal entity fields that clients do not need.

- Keep database access credentials separate by environment.

- Use least-privilege permissions for the application database user.

## Security Review Notes

- Authentication verifies the identity represented by the token.

- Authorization verifies permission for a specific operation.

- Role checks do not replace patient ownership checks.

- Caregiver access requires an authorized caregiver-patient relationship.

- Token validation settings must match token-generation settings.

- Use a strong signing key.

- Keep secrets outside source control.

- Use HTTPS in deployed environments.

- Limit CORS to trusted origins for production.

- Log failures without logging passwords or access tokens.

- Return safe error messages to API clients.

- Review all endpoints that accept patient or medicine identifiers.

- Review all endpoints that can change caregiver links.

- Review who can create, update, and delete medicine schedules.

- Review whether a caregiver can manage medicines and under what conditions.

- Review email content for unnecessary sensitive information.

- Define data retention and deletion expectations.

## Release Checklist

- [ ] Confirm the target framework and package versions.

- [ ] Restore dependencies from a clean checkout.

- [ ] Build the project without compilation errors.

- [ ] Review and resolve relevant package compatibility warnings.

- [ ] Apply migrations to a clean development database.

- [ ] Run the authentication test cases.

- [ ] Run the patient ownership test cases.

- [ ] Run caregiver relationship test cases.

- [ ] Run medicine CRUD test cases.

- [ ] Run schedule validation test cases.

- [ ] Run a reminder-processing test.

- [ ] Run a missed-dose escalation test.

- [ ] Run an email delivery test if configured.

- [ ] Confirm secrets are not tracked by Git.

- [ ] Confirm README examples use placeholders.

- [ ] Review CORS and HTTPS configuration.

- [ ] Review exception handling and logging.

- [ ] Document deployment-specific environment variables.

- [ ] Back up the database before production schema changes.

- [ ] Record any known limitations for the release.

## Glossary

### API
An interface that lets client applications communicate with the backend.

### ASP.NET Core
A framework for building web applications and HTTP APIs with .NET.

### Background service
A hosted component that performs work outside a single HTTP request.

### Caregiver
A trusted user linked to a patient for permitted monitoring or support.

### CaregiverPatient
A linking entity that records a caregiver-patient relationship.

### CORS
A browser security mechanism that controls cross-origin requests.

### DTO
A Data Transfer Object used to define request or response data.

### Dependency injection
A pattern for providing a class with its required dependencies.

### EF Core
Entity Framework Core, an object-relational mapper for .NET.

### Entity
A C# class mapped to stored application data.

### JWT
JSON Web Token, a compact token format commonly used for claims.

### Migration
A versioned change to a database schema.

### Middleware
A component in the HTTP pipeline that can inspect or handle requests.

### MySQL
A relational database management system.

### Repository
A component that groups data-access operations.

### REST
A common architectural style for HTTP-based resource APIs.

### Role
A named permission grouping such as Admin, Patient, or Caregiver.

### Schedule
Data describing when a medicine is planned to be taken.

### Service
A component that implements application rules or coordinates operations.

### SMTP
A protocol commonly used to send email.

### Token claim
A value in a token describing identity or an attribute of the caller.

### Validation
Checking that input satisfies expected rules before processing.

## Responsible Use Reminder

- MediCare is a software support tool, not a medical professional.

- Patients should follow the instructions provided by their qualified healthcare professional.

- Users should confirm the medicine and dose information they enter.

- Caregiver monitoring should be transparent and based on appropriate consent.

- Email delivery should not be assumed to be instantaneous or guaranteed.

- A notification should not be treated as proof that a patient has or has not taken a medicine.

- Users should contact local emergency services for urgent emergencies.

- Test reminder logic with synthetic accounts before demonstrating it.

- Keep private medical information out of public repositories and issue trackers.

- Review privacy and security requirements before a real-world deployment.
