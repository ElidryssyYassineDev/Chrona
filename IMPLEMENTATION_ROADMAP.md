# Chrona v1 — Implementation Roadmap

A 50-day, single-developer implementation plan for Chrona v1, sequenced from the completed system-design package (`docs/system-design/01` through `15`, plus `ADR-001` through `ADR-007`). This file makes no new architectural decision — every choice below is already made in the referenced documents; this roadmap only sequences the work.

---

## At a Glance

| # | Milestone | Duration | Cumulative Day |
|---|---|---|---|
| 1 | Foundation & Walking Skeleton | 5 days | Day 5 |
| 2 | Workforce | 6 days | Day 11 |
| 3 | Project Management | 5 days | Day 16 |
| 4 | Work Allocation *(Core Domain)* | 9 days | Day 25 |
| 5 | Time Management | 7 days | Day 32 |
| 6 | Approval Workflow | 6 days | Day 38 |
| 7 | Dashboard | 4 days | Day 42 |
| 8 | Hardening & Polish | 6 days | Day 48 |

48 of 50 days allocated — a 2-day buffer, not a rounding error. Use it where a milestone runs long; Milestone 4 is the most likely candidate.

---

## Sequencing Philosophy

Eight milestones, derived directly from the module dependency graph in `03-module-design.md` — each one builds only on modules already complete, never ahead of what its dependencies provide. Two deliberate choices shape the order beyond that graph:

**A walking skeleton comes first, not last.** Milestone 1 doesn't build any business module in depth — it proves the entire stack (Docker Compose, Keycloak, the ASP.NET Core Host, PostgreSQL, and the React SPA) can boot and complete one real, authenticated round trip before any richer feature exists. This matters more for a 50-day solo internship than it would for a larger team: it surfaces integration problems — Keycloak configuration, JWT validation, the Docker network — in week one, while there's still time to absorb the learning curve, not in week seven when a deadline is close.

**Backend and frontend are paired within each milestone, not separated into phases.** The original enterprise-scope `ROADMAP.md` treated frontend as its own phase after every module was built; this roadmap doesn't, because `01-system-overview.md` already commits to Vertical Slice Architecture as the implementation style — a milestone that only ever produces backend code isn't really a vertical slice, and deferring all frontend work to the end is exactly the kind of late-stage crunch that would undercut both the working-software goal and the learning goal this roadmap is asked to prioritize.

---

## Milestone 1 — Foundation & Walking Skeleton

**Goal:** Prove the entire stack works end-to-end — Docker Compose boots all four containers, a person can log in through Keycloak, and one authenticated request round-trips through the API to PostgreSQL and back — before any module is built in depth. This is also where the Shared Kernel (base `Entity`/`AggregateRoot`/`ValueObject`/`Result` types) and the per-module solution structure get established, since every later milestone depends on both existing correctly.

**Features:** Login, Logout, and a minimal `GET /api/v1/employees/me` resolving to one seeded Employee (`04-use-cases.md`).

**Documents to reference:** `ADR-001` through `ADR-004`; `12-component-diagram.md` (module and layer structure); `13-deployment-diagram.md` (containers, network, configuration); `09-sequence-diagrams.md`, Section 2; `06-domain-model.md`, Sections 3–5 (for the Shared Kernel's base types); `14-api-design.md`, Sections 1–3.

**Backend tasks:**
- Scaffold the solution: one project per module (`12-component-diagram.md`, Section 4), the `Chrona.Api` host, and a Shared Kernel project.
- `docker-compose.yml`: `frontend`, `backend`, `postgres`, `keycloak` (`13-deployment-diagram.md`, Section 6).
- Configure a Keycloak realm, client, and the Employee and Manager roles.
- Wire JWT-validation middleware in `Chrona.Api` (`02-system-context.md`; `15-security-model.md`, Section 3).
- Build just enough of Workforce to seed one `Employee` row linked to a Keycloak subject ID, and implement `GET /api/v1/employees/me`.

**Frontend tasks:**
- Scaffold the React SPA (Vite, TypeScript, Tailwind, shadcn/ui — `01-system-overview.md`).
- Implement the OIDC Authorization Code + PKCE flow against Keycloak (`09-sequence-diagrams.md`, Section 2).
- One authenticated screen that calls `GET /api/v1/employees/me` and displays the result.

**Estimated duration:** 5 days.

**Deliverables:** `docker-compose up` brings up a working system; a real login completes; the profile screen shows real data read from PostgreSQL.

**Definition of Done:** all four containers start cleanly from a fresh clone; a login round-trip succeeds against a real Keycloak realm, not a mock; the solution builds with zero warnings-as-errors violations; the manual test for this milestone is simply using the app as a person would.

---

## Milestone 2 — Workforce

**Goal:** Complete the Workforce module — the foundation every later module depends on for Employee and Department data, and the module-level pattern (layers, validation, EF Core mapping) every subsequent module will repeat.

**Features:** Manage Employees, Manage Departments, View Profile (`04-use-cases.md`).

**Documents to reference:** `03-module-design.md`, Workforce section; `06-domain-model.md`, `Employee`/`Department` aggregates; `07-er-diagram.md`, `08-database-design.md` (`employees`, `departments` tables); `10-class-diagrams.md`, Workforce diagram; `14-api-design.md`, Workforce endpoints.

**Backend tasks:**
- `Employee` and `Department` aggregates, with the invariants from `06-domain-model.md`, Section 3 (mandatory Department, no self-management, unique Department name).
- EF Core configuration and the first real migration, including the `CHECK` constraints from `08-database-design.md`.
- The `EmployeeDeactivated` domain event — publish it now; nothing consumes it until Milestone 4, and that's expected.
- Every Workforce endpoint in `14-api-design.md`.

**Frontend tasks:**
- Employee list, detail, create, and edit screens; a deactivate action.
- Department list, create, rename, and delete screens.
- The profile screen from Milestone 1, now showing real Department and Manager fields.

**Estimated duration:** 6 days.

**Deliverables:** a Manager can fully administer Employees and Departments through the UI; every Workforce endpoint is implemented and manually verified against `14-api-design.md`'s tables.

**Definition of Done:** every invariant in `06-domain-model.md`'s Employee and Department sections has a passing test; `08-database-design.md`'s `CHECK` constraints are present in the actual migration, not just documented; the solution still builds and Milestone 1's login flow still works unmodified.

---

## Milestone 3 — Project Management

**Goal:** Complete Project Management — the second dependency Work Allocation needs before the core domain can be built.

**Features:** Create/Edit/Archive Project, Manage Project Members (`04-use-cases.md`).

**Documents to reference:** `03-module-design.md`, Project Management section; `06-domain-model.md`, `Project`/`ProjectMember`; `ADR-006` (Project Management as its own bounded context); `11-state-diagrams.md`, Section 4; `14-api-design.md`, Project Management endpoints.

**Backend tasks:**
- `Project` aggregate with `ProjectMember` as a true child entity (`06-domain-model.md`, Section 3) — composition, not a separate aggregate, per `10-class-diagrams.md`.
- The call to Workforce that validates an Employee before adding a Project Member (`03-module-design.md`, Section 2).
- Every Project Management endpoint in `14-api-design.md`.

**Frontend tasks:**
- Project list, detail, create, and edit screens; an archive action.
- Project member management within the project detail screen.

**Estimated duration:** 5 days.

**Deliverables:** Projects and their membership are fully manageable through the UI.

**Definition of Done:** archiving a Project correctly blocks new membership and matches `11-state-diagrams.md`'s Section 4 transitions exactly; the cross-module call to Workforce is a real Application-layer call, not a duplicated validation.

---

## Milestone 4 — Work Allocation *(Core Domain)*

**Goal:** Build the core domain — the one module the entire product exists to support (`06-domain-model.md`, Section 2). This is the largest milestone in the roadmap on purpose; it's also where the project's hardest, most instructive engineering happens.

**Features:** Create/Modify/Cancel Allocation, View Allocation (`04-use-cases.md`).

**Documents to reference:** `06-domain-model.md`, `Allocation`/`AllocationHistory`, Sections 3, 6, 7, 8; `07-er-diagram.md`, `08-database-design.md` (`allocations`, `allocation_history`, the composite index); `11-state-diagrams.md`, Section 2; `09-sequence-diagrams.md`, Section 3; `14-api-design.md`, Work Allocation endpoints.

**Backend tasks:**
- `Allocation` aggregate with the Capacity Validator domain service (`06-domain-model.md`, Section 6) — the hardest single piece of logic in this project.
- `AllocationHistory`, written on creation and on every subsequent change (`06-domain-model.md`, Section 3, as revised).
- The Employee and Project existence/activity checks via Workforce and Project Management (`03-module-design.md`).
- The `EmployeeDeactivated` handler from Milestone 2, now with a real consumer: cancel the deactivated Employee's active Allocations.
- Every Work Allocation endpoint in `14-api-design.md`.

**Frontend tasks:**
- Allocation creation form with live capacity feedback.
- Allocation list, scoped by role — an Employee's own, a Manager's full view (`04-use-cases.md`, View Allocation).
- Modify and cancel actions.

**Estimated duration:** 9 days — the largest single milestone; budget real time for the capacity-validation logic specifically, since it's the one place a bug would be both easy to introduce and easy to miss.

**Deliverables:** a Manager can allocate an Employee against a Project without ever exceeding capacity, and every Allocation carries a complete history.

**Definition of Done:** the capacity invariant has tests for the boundary cases specifically — exactly 100%, one unit over, overlapping periods, adjacent non-overlapping periods; the `EmployeeDeactivated` handler is verified with an integration test, not only a unit test, since it crosses a module boundary.

---

## Milestone 5 — Time Management

**Goal:** Capture actual work against planned Allocations — the module that makes Work Allocation's plans verifiable.

**Features:** Create Timesheet, Add/Edit Time Entry, Submit Timesheet (`04-use-cases.md`).

**Documents to reference:** `06-domain-model.md`, `Timesheet`/`TimeEntry`, Section 3 (including the dual-period check added during `07-er-diagram.md`'s review); `11-state-diagrams.md`, Section 3; `09-sequence-diagrams.md`, Section 4; `14-api-design.md`, Time Management endpoints.

**Backend tasks:**
- `Timesheet` aggregate with `TimeEntry` as a child entity.
- The Work Allocation call validating an Allocation is active and covers the entry's date, plus the Timesheet's own period check — both required, independently (`06-domain-model.md`, Section 3, as revised).
- The Draft/Submitted/Approved state machine, minus the two transitions Approval Workflow owns (Milestone 6).
- Every Time Management endpoint in `14-api-design.md`.

**Frontend tasks:**
- Timesheet view for the current period, with an Add Time Entry form.
- Edit Time Entry, constrained to Draft status.
- Submit action, disabled while the Timesheet has no entries.

**Estimated duration:** 7 days.

**Deliverables:** an Employee can create a Timesheet, log time against real Allocations, and submit it.

**Definition of Done:** attempting to log time outside an Allocation's period, or outside the Timesheet's own period, is rejected in both cases, tested independently — `07-er-diagram.md`'s review specifically added the second check after it was missed the first time.

---

## Milestone 6 — Approval Workflow

**Goal:** Close the loop Time Management opens — a Manager's decision on submitted work, including the one deliberate backward transition in the entire domain model.

**Features:** Review/Approve/Reject Timesheet (`04-use-cases.md`).

**Documents to reference:** `06-domain-model.md`, `Approval`, Section 3, Section 6 (Manager Authority Resolver — a direct-manager comparison, not a hierarchy traversal, per the finalized consistency pass); `11-state-diagrams.md`, Section 3; `09-sequence-diagrams.md`, Sections 5–6; `14-api-design.md`, Approval Workflow endpoints.

**Backend tasks:**
- `Approval` aggregate — immutable once created, never referenced by Timesheet (`06-domain-model.md`, Section 4).
- The direct-manager authority check against Workforce (`03-module-design.md`) — a straightforward comparison, not a hierarchy walk.
- The self-approval check, and the mandatory-reason check on rejection.
- The one cross-module command in the whole system: `MarkTimesheetApproved` / `MarkTimesheetRejected` (`03-module-design.md`, Section 4).

**Frontend tasks:**
- A pending-approvals list for the Manager (a preview of what Milestone 7 builds out fully).
- Review screen showing the Timesheet's Time Entries.
- Approve and Reject actions, with a required reason field on rejection.

**Estimated duration:** 6 days.

**Deliverables:** a Manager can review, approve, or reject an Employee's Timesheet, and a rejected Timesheet correctly reopens to Draft.

**Definition of Done:** a Manager cannot approve or reject their own Timesheet, and this is tested, not only implemented; rejection without a reason is rejected by the API itself, not only by frontend validation.

---

## Milestone 7 — Dashboard

**Goal:** Make everything built so far visible in one place, without Dashboard owning any of it.

**Features:** View Dashboard, View Utilization, View Pending Approvals (`04-use-cases.md`).

**Documents to reference:** `03-module-design.md`, Section 5 (Dashboard queries live, no caching); `06-domain-model.md`, Section 2; `05-business-processes.md`, Dashboard Refresh; `14-api-design.md`, Dashboard endpoints.

**Backend tasks:**
- The three Dashboard endpoints, each a read-only fan-out to the modules that own the underlying data — no new tables, no duplicated data.

**Frontend tasks:**
- The full dashboard overview, utilization view, and pending-approvals view, replacing Milestone 6's preview list.

**Estimated duration:** 4 days — the shortest milestone, since it adds no new business rules, only queries against rules already built.

**Deliverables:** a Manager has one screen showing active projects, utilization, and pending approvals, all current.

**Definition of Done:** every number on the dashboard is verifiably a live query — confirmed by changing underlying data and watching the dashboard reflect it immediately, with no caching layer to invalidate.

---

## Milestone 8 — Hardening & Polish

**Goal:** Turn a working application into one that would survive a real person using it — consistent error handling, validated input everywhere, and a deployment a stranger could actually run.

**Features:** none new — this milestone closes gaps in what already exists.

**Documents to reference:** `14-api-design.md`, Sections 2 and 6 (error envelope, status codes); `15-security-model.md`, Section 7 (best-practices checklist); `13-deployment-diagram.md`, Section 7 (configuration); `12-component-diagram.md` (dependency rules, checked against what was actually built).

**Backend tasks:**
- Consistent error responses across every endpoint, matching `14-api-design.md`'s envelope exactly.
- A pass through `15-security-model.md`'s Protected Resources table, confirming every endpoint actually enforces the role it's supposed to.
- Confirm every foreign key from `08-database-design.md` has its index, and the composite index on `allocations` is actually in place.

**Frontend tasks:**
- Consistent error display for every failure category in `14-api-design.md`, Section 6.
- A pass for basic responsiveness and empty/loading states across every screen built in Milestones 2–7.

**Estimated duration:** 6 days.

**Deliverables:** a `README` a new developer could follow to run the whole system from a fresh clone; every use case in `04-use-cases.md` walked through manually, end to end, at least once.

**Definition of Done:** every item in `15-security-model.md`, Section 7 has been checked against the actual running system, not only the design document; the full 23-use-case catalog from `04-use-cases.md` has been exercised by hand at least once without a single unhandled error.