# Event Storming

## Purpose

Capture Chrona's business workflow independently of implementation.

---

# Actors

- Employee
- Team Manager
- HR Administrator
- Tenant Administrator
- System
- Identity Provider
- Notification Service

---

# Core Business Flow

Manager
    │
    ▼
Create Project
    │
    ▼
Project Created
    │
    ▼
Allocate Employee
    │
    ▼
Employee Assigned
    │
    ▼
Accept Assignment
    │
    ▼
Assignment Accepted
    │
    ▼
Record Time
    │
    ▼
Time Entry Recorded
    │
    ▼
Submit Timesheet
    │
    ▼
Timesheet Submitted
    │
    ▼
Approve Timesheet
    │
    ▼
Timesheet Approved
    │
    ▼
Generate Reports

---

# Commands

- Create Project
- Allocate Employee
- Accept Assignment
- Record Time
- Submit Timesheet
- Approve Timesheet
- Reject Timesheet
- Request Leave
- Approve Leave

---

# Domain Events

- Project Created
- Employee Assigned
- Assignment Accepted
- Time Entry Recorded
- Timesheet Submitted
- Timesheet Approved
- Timesheet Rejected
- Leave Requested
- Leave Approved

---

# Policies

When Employee Assigned
→ Recalculate Capacity

When Timesheet Submitted
→ Notify Manager

When Timesheet Approved
→ Update Reporting

When Leave Approved
→ Update Availability

When Employee Deactivated
→ Cancel Future Allocations