# Business Capabilities

Document ID: PROD-003
Status: Approved
Version: 1.0

---

# Purpose

This document identifies the core business capabilities Chrona provides.

A business capability represents something the organization must be able to perform regardless of technology or implementation.

Capabilities become the foundation for:

- Event Storming
- Domain-Driven Design
- Bounded Contexts
- Architecture
- Use Cases

---

# Capability Map

Chrona consists of seven major business capabilities.

```

```
Chrona

├── Workforce Management
├── Work Allocation ⭐ Core Domain
├── Time Management
├── Leave Management
├── Approval Management
├── Reporting & Analytics
└── Administration
```

---

# 1. Workforce Management

## Purpose

Maintain the organizational structure and employee information.

### Responsibilities

- Manage employees
- Manage teams
- Manage departments
- Manage roles
- Manage organizational hierarchy

### Inputs

- Employee information
- Organizational changes

### Outputs

- Employee directory
- Team structure
- Department structure

---

# 2. Work Allocation ⭐ Core Domain

## Purpose

Assign the right work to the right people while respecting availability and capacity.

### Responsibilities

- Allocate employees to projects
- Estimate workload
- Track capacity
- Balance workloads
- Reallocate work
- Monitor allocation history

### Inputs

- Projects
- Employees
- Capacity
- Skills
- Availability

### Outputs

- Work assignments
- Capacity utilization
- Allocation history

### Why this is the Core Domain

This capability creates the business value that differentiates Chrona.

Every other capability either supports or consumes work allocation.

---

# 3. Time Management

## Purpose

Capture how employees spend their working time.

### Responsibilities

- Record time
- Submit timesheets
- Track attendance
- Record overtime
- Track breaks

### Inputs

- Work assignments
- Employee activity

### Outputs

- Timesheets
- Attendance records
- Time reports

---

# 4. Leave Management

## Purpose

Manage employee availability.

### Responsibilities

- Request leave
- Approve leave
- Calculate balances
- Maintain holiday calendars

### Outputs

- Leave schedules
- Updated availability

---

# 5. Approval Management

## Purpose

Validate business operations before they become official.

### Responsibilities

- Review timesheets
- Review leave
- Multi-level approvals
- Delegation
- Escalation

### Outputs

- Approved records
- Rejected records
- Approval history

---

# 6. Reporting & Analytics

## Purpose

Provide operational visibility.

### Responsibilities

- Workforce utilization
- Capacity reports
- Attendance reports
- Productivity dashboards
- Approval metrics

### Outputs

- Dashboards
- KPIs
- Operational insights

---

# 7. Administration

## Purpose

Configure and secure the platform.

### Responsibilities

- Tenant management
- User management
- Authentication
- Authorization
- Audit logs
- System configuration

---

# Capability Relationships

```
Administration
        │
        ▼
Workforce Management
        │
        ▼
Work Allocation
      /     \
     ▼       ▼
Time      Leave
     \       /
      ▼     ▼
 Approval Management
          │
          ▼
Reporting & Analytics
```

---

# Core Domain

The strategic core of Chrona is **Work Allocation**.

Supporting capabilities exist to enable, validate, or analyze work allocation.

This decision influences future domain modeling, bounded contexts, aggregates, and architectural priorities.

---

# Next Step

These capabilities will be decomposed during Event Storming into:

- Actors
- Commands
- Business Events
- Policies
- Aggregates
- Read Models
- External Systems