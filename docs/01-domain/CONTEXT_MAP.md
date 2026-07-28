# Context Map

## Purpose

This document defines how bounded contexts interact.

Each bounded context owns its own business model.

Communication occurs through well-defined contracts and domain events.

---

# Ownership

| Context | Owns |
|----------|------|
| Workforce | Employees, Teams, Departments |
| Project Management | Projects |
| Work Allocation | Allocations, Capacity, Workload |
| Time Management | Time Entries, Timesheets |
| Leave Management | Leave Requests |
| Approval Workflow | Approval Requests |
| Reporting | Read Models |
| Administration | Tenants, Users, Permissions |

---

# Relationships

## Workforce → Work Allocation

Provides:

- Employee
- Team

Consumes:

Nothing.

---

## Project Management → Work Allocation

Provides:

- Project

Consumes:

Nothing.

---

## Leave Management → Work Allocation

Publishes:

Availability Changed

Work Allocation updates planning.

---

## Work Allocation → Time Management

Provides:

Active Allocations

Time Management records work against them.

---

## Time Management → Approval Workflow

Publishes:

Timesheet Submitted

Approval Workflow creates approval requests.

---

## Approval Workflow → Reporting

Publishes:

Timesheet Approved

Reporting updates projections.

---

## Administration

Supports every context.

Owns authentication, authorization, tenant configuration, and audit logging.

---

# Integration Rules

- Contexts never modify another context's data.
- Communication occurs through events or application services.
- Business ownership remains explicit.
- Cross-context dependencies should remain minimal.