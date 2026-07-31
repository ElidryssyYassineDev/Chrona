# 01 — System Overview

**Document ID:** SYS-001
**Status:** Draft — pending review
**Version:** 1.0.0

---

## 1. Purpose

This document describes what Chrona v1 is, who it serves, what it does, and what it deliberately does not do. It is the entry point for every other document in `docs/system-design/`.

---

## 2. Scope Decision

Chrona was originally scoped as a multi-tenant, self-hosted enterprise workforce management platform. That scope has been deliberately reduced for v1: a single-developer, ~50-day internship implementation, evaluated against the time available and the learning objectives of the project.

v1 keeps the same architectural ambition — Modular Monolith, Clean Architecture, Domain-Driven Design, Vertical Slice Architecture — while cutting functional scope to what one developer can build to a production-quality standard in the time available. It is a smaller set of capabilities, each still done properly, not a sloppier version of the original idea.

v1 is a single internal deployment, not a multi-tenant product. Multi-tenancy, if it returns, is a v2 concern.

---

## 3. What Chrona v1 Is

Chrona is an internal Workforce Management Platform. Its core business capability is **Work Allocation**: managers allocate employees to projects, employees record the hours they actually work, managers approve submitted timesheets, and a dashboard gives visibility into utilization and project progress.

Everything else in the system exists to support that capability.

---

## 4. What Chrona v1 Is Not

Chrona does not provide, and will not grow into, any of the following in v1:

- Payroll, salary calculation, or compensation
- Accounting, invoicing, or financial reporting
- Recruitment or candidate tracking
- CRM, sales, or lead management
- Inventory, asset, or warehouse management
- Multi-tenancy — v1 serves one organization, deployed once

---

## 5. Users

| Actor | Role in the system |
|---|---|
| Employee | Views their own allocations, records time against them, submits timesheets. |
| Manager | Allocates employees to projects, reviews and approves or rejects submitted timesheets, monitors utilization on the dashboard. |
| Administrator | Manages authentication and authorization through Keycloak — user accounts, roles, and permissions. |

---

## 6. Business Objectives

v1 must let a manager answer, at any time:

- Who is allocated to what, and for how long?
- Is any employee allocated beyond their capacity?
- Which submitted timesheets are waiting for approval?
- How does planned allocation compare to actual recorded hours?

If v1 answers these reliably, it has succeeded. Everything else is secondary.

---

## 7. System Modules

| Module | Responsibility |
|---|---|
| Authentication & Authorization | Login, roles, and permissions, via Keycloak. |
| Workforce | Employees and departments. |
| Project Management | Projects and project membership. |
| Work Allocation *(Core Domain)* | Allocating employees to projects, validating capacity, managing an allocation's lifecycle. |
| Time Management | Timesheets and the time entries within them. |
| Approval Workflow | Submitting, approving, and rejecting timesheets. |
| Dashboard | Read-only visibility: utilization, pending approvals, planned vs. actual hours, active projects. |

No module beyond this list is in scope for v1. New business modules are not introduced without an explicit decision to do so.

---

## 8. Technology Stack

**Backend:** ASP.NET Core (.NET 10), Entity Framework Core, PostgreSQL
**Frontend:** React, TypeScript, Tailwind CSS, shadcn/ui
**Authentication:** Keycloak
**Infrastructure:** Docker Compose
**Architecture:** Modular Monolith, Clean Architecture, Domain-Driven Design, Vertical Slice Architecture

---

## 9. Document Map

This document is the first of a set describing Chrona v1 end to end, generated and reviewed one at a time:

01. System Overview *(this document)*
02. System Context
03. Module Design
04. Use Cases
05. Business Processes
06. Domain Model
07. ER Diagram
08. Database Design
09. Sequence Diagrams
10. Class Diagrams
11. State Diagrams
12. Component Diagram
13. Deployment Diagram
14. API Design
15. Security Model
16. Business Rules