# Executive-dashboard — AcxiomCRM Enterprise Solution

**AcxiomCRM** is an enterprise-grade, role-based Customer Relationship Management (CRM) web application built using **ASP.NET Core 10 (MVC + Web API)**, **Entity Framework Core**, **ASP.NET Core Identity**, and **Bootstrap 5**.

The application covers the complete customer-sales lifecycle from lead capture through follow-up and opportunity management, with enterprise authentication, role-based authorization, comprehensive auditability, multi-layered validation, secured REST APIs, and executive reporting.

---

## 1. Pre-Seeded Accounts & Credentials

The application automatically seeds roles, users, sample customers, leads, opportunities, follow-ups, and audit history upon startup.

| Role | Email / Username | Password | Access Scope |
| :--- | :--- | :--- | :--- |
| **Admin** | `admin@acxiomcrm.com` | `Admin@12345` | Full system administration, User & Role management, Lockout administration, Audit logs, CRM records, Reports. |
| **Manager** | `manager@acxiomcrm.com` | `Manager@12345` | Team CRM pipeline, Customers, Leads, Opportunities, Follow-Ups, Team & Executive Reports, Audit logs. |
| **SalesExecutive** | `sales@acxiomcrm.com` | `Sales@12345` | Scoped to own/assigned Customers, Leads, Opportunities, Follow-Ups, and individual dashboard/reports. |

> **Password Policy**: Minimum 8 characters, requiring at least one uppercase letter, one lowercase letter, one digit, and one non-alphanumeric special character.
> **Account Lockout**: Enabled by default; 5 failed login attempts trigger an automatic 15-minute account lockout, which can also be unlocked by an administrator.

---

## 2. Module Tree (Section 17.2)

```text
AcxiomCRM
│
├── Authentication & Authorization
│   ├── Login (with failed attempt counter & lockout)
│   ├── Register (role-assigned user provisioning)
│   ├── Logout (safe token termination & audit log)
│   └── Access Control (Admin, Manager, SalesExecutive)
│
├── Dashboard
│   ├── Role-based KPI Cards (Total Customers, Total Leads, Open Leads, Total Deals, Open/Won/Lost Deals, Gross Pipeline, Weighted Pipeline, Follow-Ups)
│   ├── Interactive Date Filters (Today, This Week, This Month, Custom Date Range)
│   └── Chart.js Interactive Visualizations (Lead Status, Opportunity Pipeline by Stage, Monthly Revenue Won)
│
├── Customer Management
│   ├── Master List with Pagination, Search, and Status Filtering
│   ├── Create Customer with duplicate Email and Phone validation
│   ├── Edit Customer Details & Account Owner assignment
│   ├── Customer Details with Related Opportunities, Follow-ups, and Activity history
│   └── Delete Customer with modal confirmation and audit logging
│
├── Lead Management
│   ├── Lead Capture with Source, Priority, and Estimated Value
│   ├── Lead Qualification & Workflow Transitions
│   ├── Lead-to-Customer & Opportunity Conversion Workflow
│   └── Edit, Details, and Delete
│
├── Opportunity Management
│   ├── Pipeline Tracking across Stages (Qualification, Proposal, Negotiation, Won, Lost)
│   ├── Business Validations (Amount > 0, Probability 0-100%, Expected Close Date >= Today)
│   ├── Weighted Pipeline Value (`Amount * Probability / 100`)
│   └── Stage Progression Bar & Financial Breakdown
│
├── Follow-Up Management
│   ├── Schedule Follow-Up with Date Validation (Date cannot be in past for planned activities)
│   ├── Mark Complete Action (adds outcome remarks & generates Activity log)
│   ├── Reschedule Action (checks new date >= today & logs audit trail)
│   └── Overdue Task Tracking & Notification Badges
│
├── Activity Management
│   ├── Log and Review Touchpoints: Calls, Meetings, Emails, Tasks
│   └── Timeline integration on Customer & Lead profiles
│
├── User & Role Administration (Admin Only)
│   ├── User Directory with Role and Lockout status
│   ├── User Activation / Deactivation toggle
│   ├── Account Unlock action
│   └── Administrator Password Reset
│
├── Audit Log (Admin & Manager)
│   ├── Append-only tamper-evident event log
│   ├── Filter by User, Module, Action, and Date Range
│   └── Captures User, Action, Entity, RecordId, OldValue, NewValue, Timestamp, IP Address
│
├── REST API & Swagger UI
│   ├── JWT Bearer & Cookie Authentication
│   ├── Customer REST Endpoints (`GET`, `POST`, `PUT`, `DELETE /api/customers`)
│   ├── Lead REST Endpoints (`GET`, `POST`, `PUT`, `DELETE /api/leads`, `POST /api/leads/convert`)
│   ├── Opportunity REST Endpoints (`GET`, `POST`, `PUT`, `DELETE /api/opportunities`)
│   ├── Follow-Up REST Endpoints (`GET`, `POST /api/followups`)
│   ├── Pipeline Report API (`GET /api/reports/pipeline`)
│   └── OpenAPI / Swagger Interactive Documentation at `/swagger`
│
└── Executive Reporting
    ├── Pipeline Report (Stage-wise, Owner-wise, CSV Export)
    ├── Customer Report (Status & Date filter, CSV Export)
    ├── Lead Report (Source & Status filter, CSV Export)
    ├── Follow-Up Report (Status & Date filter, CSV Export)
    ├── Opportunity Report (Stage & Status filter, CSV Export)
    ├── Sales Conversion Report (Lead conversion %, Deal win rate, Won revenue)
    ├── User Activity Report (Action counts per user, CSV Export)
    └── Audit Report (Compliance export to CSV)
```

---

## 3. Mandatory Business Validation Rules (Section 5 & 17)

All business rules are enforced on both the **client-side** (immediate UX feedback) and **server-side** (rejecting manipulated or bypassed HTTP requests):

1. **Opportunity Amount**: Must be greater than 0 (`Amount > 0`). Zero or negative amounts are rejected with `"Opportunity Amount must be greater than 0."`.
2. **Opportunity Probability**: Must be between 0 and 100 inclusive (`0 <= Probability <= 100`). Values outside this range are rejected with `"Probability must be between 0 and 100."`.
3. **Expected Close Date**: Cannot be in the past for active/open opportunities (`ExpectedCloseDate >= DateTime.Today`). Rejected with `"Expected Close Date cannot be in the past."`.
4. **Follow-Up Date**: Cannot be earlier than today for a new/planned follow-up (`FollowUpDate.Date >= DateTime.Today`). Rejected with `"Follow-up date cannot be earlier than today."`.
5. **Customer Email Uniqueness**: Enforces unique customer emails across the system. Duplicates are rejected with `"Email is already registered for another customer."`.
6. **Customer Phone Uniqueness**: Enforces unique customer phone numbers across the system. Duplicates are rejected with `"Phone number is already registered for another customer."`.
7. **Lead Expected Value**: Cannot be negative (`ExpectedValue >= 0`).
8. **Anti-Forgery Protection**: All state-changing MVC POST forms are secured with `[ValidateAntiForgeryToken]`.

---

## 4. REST API Specification

Interactive Swagger UI is accessible at:
```text
http://localhost:5007/swagger
```

### Key Endpoints:
- `POST /api/auth/login`: Authenticates user and returns JWT token + user details.
- `POST /api/auth/logout`: Safely terminates token / session.
- `GET /api/customers`: Retrieves customers scoped by user role.
- `POST /api/customers`: Creates a customer with duplicate validation (201 Created or 409 Conflict).
- `GET /api/customers/{id}`: Retrieves customer by ID.
- `PUT /api/customers/{id}`: Updates customer by ID.
- `DELETE /api/customers/{id}`: Deletes customer by ID.
- `GET /api/leads`: Retrieves leads.
- `POST /api/leads`: Creates a new lead.
- `POST /api/leads/convert`: Converts lead to Customer and Opportunity.
- `GET /api/opportunities`: Retrieves opportunities.
- `POST /api/opportunities`: Creates opportunity with Amount, Probability, and Date validation.
- `GET /api/followups`: Retrieves scheduled follow-ups.
- `POST /api/followups`: Schedules follow-up with Date validation.
- `GET /api/reports/pipeline`: Retrieves stage-wise and owner-wise pipeline summary.

---

## 5. Running the Application & Automated Tests

### Run Application:
```bash
dotnet run
```
Navigate to:
- Web Application: `http://localhost:5007`
- Swagger API Docs: `http://localhost:5007/swagger`

### Run Test Suite:
```bash
dotnet test
```
The test suite executes 15 automated xUnit tests validating all acceptance criteria, business rules, duplicate constraints, workflow transitions, and REST API controllers.
