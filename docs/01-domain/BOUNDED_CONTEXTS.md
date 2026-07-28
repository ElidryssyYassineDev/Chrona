# Bounded Contexts

## Purpose

This document defines the major business boundaries within Chrona.

Each bounded context owns its own domain model and business rules.

---

# Contexts

## Workforce

Purpose:
Manage organizational structure and employee information.

Owns:

- Employee
- Team
- Department
- Role

---

## Work Allocation ⭐ Core Domain

Purpose:
Plan and allocate work while respecting employee capacity and availability.

Owns:

- Work Allocation
- Capacity Planning
- Workload
- Assignment

---

## Time Management

Purpose:
Capture actual work performed.

Owns:

- Time Entry
- Timesheet
- Attendance
- Overtime

---

## Leave Management

Purpose:
Manage employee availability through leave.

Owns:

- Leave Request
- Leave Balance
- Holiday Calendar

---

## Approval Workflow

Purpose:
Coordinate configurable approval processes.

Owns:

- Approval
- Approval Policy
- Approval Decision

---

## Reporting & Analytics

Purpose:
Provide operational insight.

Owns:

- Dashboards
- KPIs
- Read Models

---

## Administration

Purpose:
Manage tenants, users, permissions, and configuration.

Owns:

- Tenant
- User
- Permission
- Audit Log