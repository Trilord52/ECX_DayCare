# API Reference and Endpoints

## Interactive API Documentation

The API reference is hosted in Development using Scalar at:

- Scalar UI: `http://localhost:5080/scalar/v1`
- OpenAPI v3.1 specification: `http://localhost:5080/openapi/v1.json`

## API Conventions

#### Paging Convention
List endpoints accept standard query parameters `page` (1-based integer, default `1`) and `pageSize` (integer, default `20`). Responses return a `PagedResult<T>` envelope:
```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalCount": 42,
  "totalPages": 3
}
```

#### Error Handling Convention
All endpoints return errors structured according to RFC 7807 Problem Details (`application/problem+json`):
```json
{
  "title": "Business rule violation",
  "status": 422,
  "detail": "Guardian is not authorized to pick up this child.",
  "code": "PICKUP_NOT_AUTHORIZED",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00"
}
```

## Endpoints Grouped by Controller

#### Auth (`AuthController`)
Authentication and user identity operations.

| Method | Path | Allowed Roles | Purpose |
|---|---|---|---|
| `POST` | `/api/v1/Auth/login` | `Anonymous` | Authenticate user with email and password, returning JWT bearer token |
| `GET` | `/api/v1/Auth/me` | `Authenticated` | Retrieve current authenticated user profile, roles, and linked entity IDs |
| `POST` | `/api/v1/Auth/change-password` | `Authenticated` | Change password for the current authenticated user |

#### Dashboard (`DashboardController`)
Aggregated metrics and activity summaries for dashboards.

| Method | Path | Allowed Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/Dashboard/admin` | `Admin` | Fetch facility-wide operational metrics, classroom ratios, attendance, and billing totals |
| `GET` | `/api/v1/Dashboard/staff` | `Staff, Admin` | Fetch assigned classroom rosters, check-in status, and pending daily report items |

#### Children (`ChildrenController`)
Child enrollment, profile management, classroom/fee assignments, and child-specific records.

| Method | Path | Allowed Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/Children` | `Admin, Staff, Parent` | List children with pagination and filters (scoped by role and assignment) |
| `POST` | `/api/v1/Children` | `Admin` | Enroll a new child |
| `GET` | `/api/v1/Children/{id}` | `Admin, Staff, Parent` | Get detailed profile of a child by ID |
| `PUT` | `/api/v1/Children/{id}` | `Admin` | Update child profile information |
| `DELETE` | `/api/v1/Children/{id}` | `Admin` | Soft delete a child record |
| `PUT` | `/api/v1/Children/{id}/classroom` | `Admin` | Assign child to a classroom |
| `PUT` | `/api/v1/Children/{id}/fee-plan` | `Admin` | Assign recurring fee plan to child |
| `GET` | `/api/v1/Children/{id}/guardians` | `Admin, Staff, Parent` | List linked guardians for a child |
| `POST` | `/api/v1/Children/{id}/guardians` | `Admin` | Link a guardian to a child with relationship, pickup permission, and primary contact flags |
| `PUT` | `/api/v1/Children/{id}/guardians/{guardianId}` | `Admin` | Update guardian link details (relationship, pickup, primary contact) |
| `DELETE` | `/api/v1/Children/{id}/guardians/{guardianId}` | `Admin` | Unlink guardian from child |
| `GET` | `/api/v1/Children/{id}/attendance` | `Admin, Staff, Parent` | Get attendance history for a child |
| `GET` | `/api/v1/Children/{id}/daily-reports` | `Admin, Staff, Parent` | List daily report summaries for a child |
| `GET` | `/api/v1/Children/{id}/daily-reports/{date}` | `Admin, Staff, Parent` | Get daily report detail including all activity entries for a date |
| `PUT` | `/api/v1/Children/{id}/daily-reports/{date}` | `Admin, Staff` | Upsert daily report header notes |
| `POST` | `/api/v1/Children/{id}/daily-reports/{date}/entries` | `Admin, Staff` | Add an activity entry (Meal, Nap, Diaper, Activity, Medication, Note) to daily report |
| `GET` | `/api/v1/Children/{id}/photo` | `Admin, Staff, Parent` | Retrieve child profile photo with HTTP caching support |
| `PUT` | `/api/v1/Children/{id}/photo` | `Admin` | Upload or replace child profile photo (multipart form, normalized to at most 512x512 JPEG quality 85) |
| `DELETE` | `/api/v1/Children/{id}/photo` | `Admin` | Remove child profile photo |

#### Classrooms (`ClassroomsController`)
Classroom capacity, age band definitions, staff assignments, and room rosters.

| Method | Path | Allowed Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/Classrooms` | `Admin, Staff` | List all classrooms with capacity, age bands, and assigned staff |
| `POST` | `/api/v1/Classrooms` | `Admin` | Create a new classroom |
| `GET` | `/api/v1/Classrooms/{id}` | `Admin, Staff` | Get classroom details by ID |
| `PUT` | `/api/v1/Classrooms/{id}` | `Admin` | Update classroom properties and capacity |
| `PUT` | `/api/v1/Classrooms/{id}/staff` | `Admin` | Assign or replace staff members assigned to classroom |
| `GET` | `/api/v1/Classrooms/{id}/roster` | `Admin, Staff` | Get active child roster and current check-in state for classroom |

#### Staff (`StaffController`)
Staff member management, classroom assignments, credentials, and profile photos.

| Method | Path | Allowed Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/Staff` | `Admin` | List all staff members with pagination and status filters |
| `POST` | `/api/v1/Staff` | `Admin` | Create a staff record and provision user login account |
| `GET` | `/api/v1/Staff/{id}` | `Admin` | Get staff member details by ID |
| `PUT` | `/api/v1/Staff/{id}` | `Admin` | Update staff details, job title, and classroom assignments |
| `DELETE` | `/api/v1/Staff/{id}` | `Admin` | Deactivate or remove staff record |
| `POST` | `/api/v1/Staff/{id}/reset-password` | `Admin` | Reset staff user account password |
| `GET` | `/api/v1/Staff/{id}/photo` | `Admin, Staff` | Retrieve staff profile photo with HTTP caching |
| `PUT` | `/api/v1/Staff/{id}/photo` | `Admin` | Upload or replace staff profile photo (multipart form, normalized to at most 512x512 JPEG quality 85) |
| `DELETE` | `/api/v1/Staff/{id}/photo` | `Admin` | Remove staff profile photo |

#### Guardians (`GuardiansController`)
Parent and guardian contact records, portal accounts, and profile photos.

| Method | Path | Allowed Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/Guardians` | `Admin` | List guardians with pagination and search |
| `POST` | `/api/v1/Guardians` | `Admin` | Create a new guardian record |
| `GET` | `/api/v1/Guardians/{id}` | `Admin` | Get guardian details and linked children |
| `PUT` | `/api/v1/Guardians/{id}` | `Admin` | Update guardian contact details and address |
| `DELETE` | `/api/v1/Guardians/{id}` | `Admin` | Remove guardian record |
| `POST` | `/api/v1/Guardians/{id}/account` | `Admin` | Provision login portal user account for guardian |
| `POST` | `/api/v1/Guardians/{id}/reset-password` | `Admin` | Reset guardian portal login password |
| `GET` | `/api/v1/Guardians/{id}/photo` | `Admin, Staff, Parent` | Retrieve guardian profile photo |
| `PUT` | `/api/v1/Guardians/{id}/photo` | `Admin` | Upload or replace guardian profile photo (multipart form, normalized to at most 512x512 JPEG quality 85) |
| `DELETE` | `/api/v1/Guardians/{id}/photo` | `Admin` | Remove guardian profile photo |

#### Attendance (`AttendanceController`)
Daily check-in and check-out tracking, absence recording, and real-time staff-to-child ratios.

| Method | Path | Allowed Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/Attendance` | `Admin, Staff` | Query attendance records by date, classroom, or child |
| `POST` | `/api/v1/Attendance/check-in` | `Admin, Staff` | Check in a child with timestamp, guardian verification, and recorded by |
| `POST` | `/api/v1/Attendance/{id}/check-out` | `Admin, Staff` | Check out an active attendance record with pickup guardian authorization |
| `POST` | `/api/v1/Attendance/absences` | `Admin, Staff` | Record child absence with optional reason |
| `GET` | `/api/v1/Attendance/ratios` | `Admin, Staff` | Get current staff-to-child ratio compliance per classroom |

#### DailyReportEntries (`DailyReportEntriesController`)
Direct modification and deletion of individual daily report activity entries.

| Method | Path | Allowed Roles | Purpose |
|---|---|---|---|
| `PUT` | `/api/v1/daily-report-entries/{entryId}` | `Admin, Staff` | Update an existing daily report entry |
| `DELETE` | `/api/v1/daily-report-entries/{entryId}` | `Admin, Staff` | Delete a daily report entry |

#### Incidents (`IncidentsController`)
Injury and behavioral incident reporting, parent notifications, and digital sign-off.

| Method | Path | Allowed Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/Incidents` | `Admin, Staff, Parent` | List incident reports with date, classroom, and severity filters |
| `POST` | `/api/v1/Incidents` | `Admin, Staff` | Log a new incident with type, severity, description, and actions taken |
| `GET` | `/api/v1/Incidents/{id}` | `Admin, Staff, Parent` | Get full incident report detail |
| `PUT` | `/api/v1/Incidents/{id}` | `Admin, Staff` | Update incident details or corrective actions |
| `POST` | `/api/v1/Incidents/{id}/notify-parent` | `Admin, Staff` | Mark incident as parent notified |
| `POST` | `/api/v1/Incidents/{id}/acknowledge` | `Parent` | Acknowledge incident report with parent signature notes |

#### Announcements (`AnnouncementsController`)
Facility-wide, staff-only, or classroom-specific broadcast notices.

| Method | Path | Allowed Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/Announcements` | `Admin, Staff, Parent` | List announcements visible to current user audience |
| `POST` | `/api/v1/Announcements` | `Admin` | Publish a new announcement (Everyone, Parents, Staff, or Classroom) |
| `PUT` | `/api/v1/Announcements/{id}` | `Admin` | Update announcement content, audience, or expiration |
| `DELETE` | `/api/v1/Announcements/{id}` | `Admin` | Delete an announcement |

#### FeePlans (`FeePlansController`)
Tuition templates with monthly charge amounts.

| Method | Path | Allowed Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/fee-plans` | `Admin` | List all active and inactive fee plans |
| `POST` | `/api/v1/fee-plans` | `Admin` | Create a new recurring fee plan template |
| `PUT` | `/api/v1/fee-plans/{id}` | `Admin` | Update fee plan name, description, amount, or active status |

#### Invoices (`InvoicesController`)
Billing generation, fee adjustments, parent statements, and payment recording.

| Method | Path | Allowed Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/Invoices` | `Admin, Parent` | List invoices (scoped to guardian for Parent role) |
| `POST` | `/api/v1/Invoices/generate` | `Admin` | Batch generate monthly invoices for all active children |
| `GET` | `/api/v1/Invoices/{id}` | `Admin, Parent` | Get invoice statement with line items, payments, and balance |
| `POST` | `/api/v1/Invoices/{id}/adjustments` | `Admin` | Add a credit or debit line adjustment to an invoice |
| `POST` | `/api/v1/Invoices/{id}/payments` | `Admin` | Record offline payment (Cash, BankTransfer, Telebirr, Other) against invoice |
| `POST` | `/api/v1/Invoices/{id}/online-payments` | `Parent` | Process parent online simulated payment against invoice |
| `POST` | `/api/v1/Invoices/{id}/void` | `Admin` | Void an unpaid invoice |

#### Payments (`PaymentsController`)
Ledger payment reversal and voiding.

| Method | Path | Allowed Roles | Purpose |
|---|---|---|---|
| `POST` | `/api/v1/Payments/{id}/void` | `Admin` | Void a previously recorded payment and restore invoice balance |

#### Reports (`ReportsController`)
Management intelligence and auditing summaries.

| Method | Path | Allowed Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/Reports/attendance` | `Admin` | Generate aggregated attendance report by date range and classroom |
| `GET` | `/api/v1/Reports/billing` | `Admin` | Generate aggregated revenue, collection, and aging balance report |

#### Health (`HealthController`)
Service health and database connectivity diagnostics.

| Method | Path | Allowed Roles | Purpose |
|---|---|---|---|
| `GET` | `/health` | `Anonymous` | Root health check verifying API and PostgreSQL connectivity |
| `GET` | `/api/v1/health` | `Anonymous` | Versioned health check verifying API and PostgreSQL connectivity |



---
