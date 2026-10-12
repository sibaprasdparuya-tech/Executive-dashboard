# AcxiomCRM — Enterprise CRM & Executive Dashboard

**AcxiomCRM** is an enterprise-oriented, role-based Customer Relationship Management (CRM) web application built with **ASP.NET Core 10, MVC, Web API, Entity Framework Core, ASP.NET Core Identity, and Bootstrap 5**.

The platform manages the complete sales lifecycle, from lead capture and customer management to opportunity tracking, follow-up scheduling, audit logging, and executive reporting.

## Key Features

- **Role-Based Access Control:** Admin, Manager, and Sales Executive roles.
- **Executive Dashboard:** KPI cards, interactive charts, and date filters.
- **Customer Management:** Search, pagination, filtering, and CRUD operations.
- **Lead Management:** Lead capture, qualification, and conversion workflows.
- **Opportunity Pipeline:** Stage tracking, probability, and weighted pipeline calculations.
- **Follow-Up Management:** Scheduling, rescheduling, completion, and overdue tracking.
- **Activity Timeline:** Track calls, meetings, emails, and tasks.
- **User Administration:** Manage accounts, roles, and account lockouts.
- **Audit Logging:** Record user actions and changes for accountability.
- **REST APIs:** Secured endpoints with Swagger documentation.
- **Executive Reports:** Pipeline, conversion, customer, lead, and activity reports.
- **CSV Export:** Export supported reports for further analysis.
- **Automated Testing:** xUnit tests for business rules and application workflows.

## Technology Stack

| Layer | Technologies |
|---|---|
| Backend | ASP.NET Core 10, C# |
| Web Architecture | MVC, RESTful Web API |
| ORM | Entity Framework Core |
| Authentication | ASP.NET Core Identity, JWT |
| Frontend | HTML, CSS, JavaScript, Bootstrap 5 |
| Data Visualization | Chart.js |
| API Documentation | Swagger / OpenAPI |
| Testing | xUnit |

## Demo Accounts

The application seeds sample users and CRM data during startup.

| Role | Email | Password |
|---|---|---|
| Admin | `admin@acxiomcrm.com` | `Admin@12345` |
| Manager | `manager@acxiomcrm.com` | `Manager@12345` |
| Sales Executive | `sales@acxiomcrm.com` | `Sales@12345` |

**Security note:** These are development/demo credentials. Never use them in a publicly deployed production environment.

### Password and Account Security

- Minimum password length of 8 characters.
- Requires uppercase, lowercase, numeric, and special characters.
- Account lockout after 5 failed login attempts for 15 minutes.
- Administrator-assisted account unlocking.
- Anti-forgery protection for state-changing MVC forms.

## Application Modules

### 1. Authentication & Authorization
- Login and logout.
- User registration and role assignment.
- Failed-login tracking and account lockout.
- Role-based access for Admin, Manager, and Sales Executive.

### 2. Executive Dashboard
- Total customers and leads.
- Open leads and total opportunities.
- Won, lost, and open deals.
- Gross and weighted pipeline values.
- Follow-up statistics.
- Date filters for today, this week, this month, and custom ranges.
- Interactive Chart.js visualizations for lead status, opportunity stages, and monthly won revenue.

### 3. Customer Management
- Create, view, update, and delete customers.
- Search, pagination, and status filtering.
- Duplicate email and phone validation.
- Account owner assignment.
- Related opportunities, follow-ups, and activity history.
- Confirmation dialogs and audit logging.

### 4. Lead Management
- Capture leads with source, priority, and estimated value.
- Qualify leads and manage workflow transitions.
- Convert qualified leads into customers and opportunities.
- View, edit, and delete lead records.

### 5. Opportunity Management
- Track opportunities through Qualification, Proposal, Negotiation, Won, and Lost stages.
- Validate deal amount, probability, and expected close date.
- Display pipeline progression and financial breakdowns.

**Weighted Pipeline Formula**

`Weighted Pipeline = Amount × (Probability / 100)`

### 6. Follow-Up Management
- Schedule follow-up activities.
- Prevent past dates for newly planned follow-ups.
- Mark activities as complete with outcome remarks.
- Reschedule activities with date validation.
- Identify overdue tasks with notification badges.

### 7. Activity Management
- Record calls, meetings, emails, and tasks.
- Maintain activity timelines for customers and leads.
- Track outcomes and relevant activity history.

### 8. User & Role Administration
- View users and assigned roles.
- Activate or deactivate accounts.
- Inspect account lockout status.
- Unlock locked accounts.
- Reset user passwords through administrator controls.

### 9. Audit Logging
- Track user actions and affected records.
- Record modules, actions, timestamps, and record identifiers.
- Filter logs by user, module, action, and date range.
- Support administrative review and compliance reporting.

*Audit-log integrity depends on the application's implementation and database permissions; an append-only design alone does not guarantee tamper-proof storage.*

### 10. REST API & Swagger
- JWT and cookie-based authentication support.
- Customer, lead, opportunity, and follow-up endpoints.
- Lead conversion workflow.
- Pipeline reporting endpoint.
- Interactive API documentation through Swagger.

### 11. Executive Reporting
- Pipeline report by stage and owner.
- Customer report with status and date filters.
- Lead report by source and status.
- Follow-up and opportunity reports.
- Sales conversion and deal win-rate reports.
- User activity and audit reports.
- CSV export for supported reports.

## Business Validation Rules

The application is designed to validate business rules on both the client and server sides.

| Validation | Rule |
|---|---|
| Opportunity Amount | Must be greater than zero |
| Opportunity Probability | Must be between 0 and 100 |
| Expected Close Date | Must not be in the past for active opportunities |
| Follow-Up Date | New planned activities cannot use past dates |
| Customer Email | Must be unique |
| Customer Phone | Must be unique |
| Lead Expected Value | Cannot be negative |
| MVC Form Security | State-changing forms require anti-forgery validation |

Server-side validation is essential because client-side validation can be bypassed.

## Project Structure

The application is organized around the following functional areas:

```text
AcxiomCRM/
├── Controllers/
├── Models/
├── Views/
├── Data/
├── Services/
├── DTOs/
├── Middleware/
├── wwwroot/
├── Tests/
├── appsettings.json
└── Program.cs
```

*The directory tree above is illustrative. Adjust it to match the actual repository structure.*

## Getting Started

### Prerequisites

- .NET 10 SDK
- A supported database configured for the application
- Git
- Visual Studio or Visual Studio Code

### 1. Clone the Repository

```bash
git clone <YOUR_REPOSITORY_URL>
cd AcxiomCRM
```

Replace `<YOUR_REPOSITORY_URL>` with the actual GitHub repository URL.

### 2. Configure the Database

Update the database connection string using local user secrets or environment variables. Ensure the database provider and Entity Framework Core configuration match the project.

Do not commit production credentials, JWT signing keys, or sensitive connection strings.

### 3. Apply Database Migrations

If the project uses EF Core migrations, run:

```bash
dotnet ef database update
```

Run this command from the directory containing the relevant project, or specify the project paths as required.

### 4. Restore Dependencies

```bash
dotnet restore
```

### 5. Build the Application

```bash
dotnet build
```

### 6. Run the Application

```bash
dotnet run
```

Use the actual local URL printed in the terminal. If the project is configured to use port `5007`, the application may be available at:

- Web application: `http://localhost:5007`
- Swagger UI: `http://localhost:5007/swagger`

The configured URL may differ depending on the launch profile and HTTPS settings.

## REST API Reference

| Method | Endpoint | Purpose |
|---|---|---|
| POST | `/api/auth/login` | Authenticate and obtain a JWT |
| POST | `/api/auth/logout` | End the authenticated session or token workflow |
| GET | `/api/customers` | Retrieve accessible customers |
| POST | `/api/customers` | Create a customer |
| GET | `/api/customers/{id}` | Retrieve customer details |
| PUT | `/api/customers/{id}` | Update a customer |
| DELETE | `/api/customers/{id}` | Delete a customer |
| GET | `/api/leads` | Retrieve leads |
| POST | `/api/leads` | Create a lead |
| POST | `/api/leads/convert` | Convert a lead |
| GET | `/api/opportunities` | Retrieve opportunities |
| POST | `/api/opportunities` | Create an opportunity |
| GET | `/api/followups` | Retrieve follow-ups |
| POST | `/api/followups` | Schedule a follow-up |
| GET | `/api/reports/pipeline` | Retrieve pipeline summaries |

Consult the generated Swagger documentation for the exact request schemas, authorization requirements, and available operations.

## Automated Testing

Run the test suite using:

```bash
dotnet test
```

The project specification targets 15 automated xUnit tests covering acceptance criteria, validation rules, duplicate constraints, workflow transitions, and API controllers.

**Note:** The actual number of passing tests should be confirmed by running the command locally.

## Security Considerations

- Use environment variables or user secrets for sensitive configuration.
- Change all seeded passwords before public deployment.
- Configure JWT expiration, signing keys, and validation correctly.
- Enforce authorization and record-level access restrictions on the server.
- Validate all incoming data on the server.
- Use HTTPS in deployed environments.
- Review audit-log access and retention policies.
- Disable public access to development-only features where appropriate.

## Future Enhancements

Potential improvements include:

- Email and notification integrations.
- Advanced sales analytics and forecasting.
- Automated follow-up reminders.
- Additional integration and end-to-end tests.
- Containerized deployment and CI/CD automation.

## Author

**Sibaprasad Paruya**

*AcxiomCRM — Enterprise Customer Relationship Management Solution*

