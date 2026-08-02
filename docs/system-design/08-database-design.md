# 08 — Database Design

**Document ID:** SYS-008
**Status:** Draft — pending review
**Version:** 1.0.0

---

## 1. Purpose

`07-er-diagram.md` fixed the entities, relationships, and constraints. This document translates that into what actually gets typed into an EF Core migration: physical table and column names, PostgreSQL types, and the conventions that make nine tables look like they were designed together instead of nine separate decisions. Nothing here changes what `07-er-diagram.md` already decided — this is a naming and physical-representation pass, not a redesign.

---

## 2. Database Conventions

**Naming conventions.** snake_case for every physical identifier — tables, columns, constraints, indexes. Table names are plural (`employees`, `allocations`), with one deliberate exception: `allocation_history`, kept singular/collective, matching how an audit log reads more naturally than a count of rows. C# stays PascalCase throughout; the translation is handled by the `EFCore.NamingConventions` package, not by hand-naming every property.

**Primary key strategy (UUID).** Every primary key is a `uuid`, generated client-side (`Guid.NewGuid()`) the moment an aggregate is constructed in C#, not assigned by the database on insert. An aggregate's identity exists as soon as it exists, matching `06-domain-model.md` — not only after a round trip to the database.

**Foreign key naming.** `fk_{table}_{column}` — e.g., `fk_employees_department_id`. Naming by column rather than by referenced table avoids ambiguity anywhere a table references the same target more than once, or will in the future.

**Audit columns.** `created_at_utc` on every table; transition-specific timestamps exactly where `07-er-diagram.md` already established a need for one. No table gets a timestamp it doesn't have a stated reason for.

**Soft delete strategy.** A status column (`is_active`, `status`) substitutes for deletion on five tables; `departments` and `project_members` are the two genuinely, physically deleted in normal operation — both already decided in `07-er-diagram.md`, Section 5. No separate `is_deleted` flag anywhere: where a status already exists, a second deletion flag would just be two sources of truth for the same fact.

**Enum persistence.** Every status/outcome column is `text`, not a native PostgreSQL enum or a small integer — decided and justified in `07-er-diagram.md`, Section 5, unchanged here.

**UTC timestamps.** Every timestamp column is `timestamptz`; every value written to one is UTC by application convention (`DateTime.UtcNow`, never local time). PostgreSQL's `timestamptz` normalizes storage but doesn't enforce which timezone a value was written in — this is an application discipline, not a database constraint.

**Migration strategy.** Each module owns an independent EF Core `DbContext` and its own migration history, even though every module's tables live in the same physical PostgreSQL database (`ADR-005`). A migration for Work Allocation never touches, and is never blocked by, a pending migration for Time Management. Section 5 covers how this interacts with foreign keys that cross a module boundary.

---

## 3. Table Definitions

### departments *(Workforce)*

| Column | Type | Nullable | Key |
|---|---|---|---|
| department_id | uuid | No | PK |
| name | text | No | UK |
| created_at_utc | timestamptz | No | |

Check: `ck_departments_name_not_empty` — `length(trim(name)) > 0`.

### employees *(Workforce)*

| Column | Type | Nullable | Key |
|---|---|---|---|
| employee_id | uuid | No | PK |
| keycloak_subject_id | uuid | No | UK |
| first_name | text | No | |
| last_name | text | No | |
| department_id | uuid | No | FK → departments |
| manager_id | uuid | Yes | FK → employees (self) |
| is_active | boolean | No | |
| created_at_utc | timestamptz | No | |
| deactivated_at_utc | timestamptz | Yes | |

Check: `ck_employees_deactivated_at_consistency` — `is_active = true OR deactivated_at_utc IS NOT NULL`.

### projects *(Project Management)*

| Column | Type | Nullable | Key |
|---|---|---|---|
| project_id | uuid | No | PK |
| name | text | No | UK |
| status | text | No | |
| created_at_utc | timestamptz | No | |
| archived_at_utc | timestamptz | Yes | |

Checks: `ck_projects_name_not_empty`; `ck_projects_archived_at_consistency` — `status <> 'Archived' OR archived_at_utc IS NOT NULL`.

### project_members *(Project Management)*

| Column | Type | Nullable | Key |
|---|---|---|---|
| project_id | uuid | No | PK, FK → projects |
| employee_id | uuid | No | PK, FK → employees |
| added_at_utc | timestamptz | No | |

### allocations *(Work Allocation)*

| Column | Type | Nullable | Key |
|---|---|---|---|
| allocation_id | uuid | No | PK |
| employee_id | uuid | No | FK → employees |
| project_id | uuid | No | FK → projects |
| period_start | date | No | |
| period_end | date | No | |
| percentage | numeric(5,2) | No | |
| status | text | No | |
| created_at_utc | timestamptz | No | |

Checks: `ck_allocations_percentage_range` — `percentage > 0 AND percentage <= 100`; `ck_allocations_period_valid` — `period_start <= period_end`.

### allocation_history *(Work Allocation)*

| Column | Type | Nullable | Key |
|---|---|---|---|
| allocation_history_id | uuid | No | PK |
| allocation_id | uuid | No | FK → allocations |
| change_type | text | No | |
| old_value | text | Yes | |
| new_value | text | Yes | |
| changed_at_utc | timestamptz | No | |

Check: `ck_allocation_history_change_type_valid` — `change_type IN ('Created', 'StatusChanged', 'PeriodChanged', 'PercentageChanged')`. This is the one status-shaped column with a value-restricting `CHECK` in this schema — `07-er-diagram.md` specifically called `change_type` a "constrained enumeration," unlike the other status/outcome columns below, which rely on application-level validation only. See Section 6.

### timesheets *(Time Management)*

| Column | Type | Nullable | Key |
|---|---|---|---|
| timesheet_id | uuid | No | PK |
| employee_id | uuid | No | FK → employees |
| period_start | date | No | |
| period_end | date | No | |
| status | text | No | |
| last_submitted_at_utc | timestamptz | Yes | |

Check: `ck_timesheets_period_valid` — `period_start <= period_end`.

### time_entries *(Time Management)*

| Column | Type | Nullable | Key |
|---|---|---|---|
| time_entry_id | uuid | No | PK |
| timesheet_id | uuid | No | FK → timesheets |
| allocation_id | uuid | No | FK → allocations |
| entry_date | date | No | |
| hours | numeric(4,2) | No | |
| created_at_utc | timestamptz | No | |

Check: `ck_time_entries_hours_range` — `hours > 0 AND hours <= 24`. The rule that `entry_date` must fall within both the Allocation's and the Timesheet's period is **not** a database constraint — it spans two other tables, which a single-table `CHECK` can't reach (`07-er-diagram.md`, Section 4). Enforced in the Application layer before save; see Section 5.

### approvals *(Approval Workflow)*

| Column | Type | Nullable | Key |
|---|---|---|---|
| approval_id | uuid | No | PK |
| timesheet_id | uuid | No | FK → timesheets |
| manager_id | uuid | No | FK → employees |
| outcome | text | No | |
| reason | text | Yes | |
| decided_at_utc | timestamptz | No | |

Check: `ck_approvals_reason_consistency` — `outcome <> 'Rejected' OR reason IS NOT NULL`.

---

## 4. Index Strategy

**Foreign key indexes.** Eleven, not twelve — `project_members`' composite primary key `(project_id, employee_id)` already indexes `project_id` as its leading column, so a separate index there would be redundant. `employee_id`, as the trailing column, does need its own:

`ix_employees_department_id`, `ix_employees_manager_id`, `ix_project_members_employee_id`, `ix_allocations_project_id`, `ix_allocation_history_allocation_id`, `ix_timesheets_employee_id`, `ix_time_entries_timesheet_id`, `ix_time_entries_allocation_id`, `ix_approvals_timesheet_id`, `ix_approvals_manager_id` — ten single-column indexes, plus `allocations.employee_id`, folded into the composite index below rather than indexed separately.

**Unique indexes.** `uk_employees_keycloak_subject_id`, `uk_departments_name`, `uk_projects_name` — PostgreSQL creates the backing index automatically with each `UNIQUE` constraint; nothing further to define.

**Business indexes.** `ix_timesheets_status`, serving `04-use-cases.md`'s View Pending Approvals query directly.

**Composite indexes.** One, and only one, meets "supported by an actual use case": `ix_allocations_employee_id_status`, on `(employee_id, status)`, replacing the plain `employee_id` index rather than adding to it — leftmost-prefix matching means a query filtering on `employee_id` alone still uses it. This serves the Capacity Validator's real, frequent query — every Create Allocation and Modify Allocation call needs "this Employee's other *active* Allocations" (`06-domain-model.md`, Section 6) — more efficiently than filtering by employee first and status second in two separate steps. No other composite index is added; nothing else in this document points to a query that needs one.

---

## 5. Entity Framework Mapping Notes

**Owned Value Objects.** `AllocationPeriod`, `AllocationPercentage`, and `TimesheetPeriod` map as EF Core complex types (or `OwnsOne`, depending on EF Core version), flattened onto their owner's table — no separate table, no separate key, matching `07-er-diagram.md`, Section 5 exactly.

**Aggregate mapping.** Each aggregate root (`Employee`, `Department`, `Project`, `Allocation`, `Timesheet`, `Approval`) gets its own `DbSet`. Child entities (`ProjectMember`, `AllocationHistory`, `TimeEntry`) do not — they're reachable only through their parent's navigation property. This puts the aggregate boundary `06-domain-model.md` and `07-er-diagram.md` both describe into the ORM configuration itself, not only into documentation someone could bypass.

**Delete behaviors.** Configured via `.OnDelete(DeleteBehavior.X)` in Fluent API, one line per relationship, taken directly from `07-er-diagram.md`, Section 4 — Cascade for the three same-aggregate child relationships, Restrict for the eight cross-aggregate ones, SetNull for `Employee.ManagerId`.

**Concurrency considerations.** PostgreSQL's built-in `xmin` system column is used for optimistic concurrency, via EF Core's `IsRowVersion()` mapped to `xmin` — no schema change, no dedicated `RowVersion` column. Given v1's scale, this is the lighter of two reasonable choices; a dedicated column is deferred to Section 6 rather than added speculatively.

**Cross-module references stay data, never navigation.** `Allocation.EmployeeId` and `Allocation.ProjectId` are plain `Guid` properties, with a real foreign key constraint in the migration — but no EF Core navigation property to `Employee` or `Project`, because those types belong to a different module's `DbContext` entirely. The same applies to every other cross-module reference in Section 3. This is the one place the module boundary from `03-module-design.md` would be easiest to quietly violate — a navigation property is one line to add and looks harmless — so it's worth stating as a rule here, not just an outcome of how the DbContexts happen to be split.

**Future migrations.** Each module's `DbContext` maps only to the tables listed under it in Section 3: Workforce owns `departments` and `employees`; Project Management owns `projects` and `project_members`; Work Allocation owns `allocations` and `allocation_history`; Time Management owns `timesheets` and `time_entries`; Approval Workflow owns `approvals`. Cross-module foreign keys are still real database constraints — PostgreSQL enforces them regardless of which `DbContext` created which table — they're just never modeled as EF Core relationships within a single context.

---

## 6. Design Decisions

### Decisions made

- snake_case physical naming throughout, via `EFCore.NamingConventions`, while C# stays PascalCase.
- Client-side UUID generation (`Guid.NewGuid()`), not server-side database defaults.
- Each module owns an independent `DbContext` and migration history against the one shared database — resolving how `ADR-001` (module isolation) and `ADR-005` (shared database) combine at the EF Core level, which hadn't been made explicit before this document.
- Cross-module foreign keys are plain `uuid` columns with a real database constraint, never an EF Core navigation property to another module's entity type — the module boundary holds at the ORM layer, not only in documentation.
- PostgreSQL's native `xmin` column is used for optimistic concurrency rather than a dedicated `RowVersion` column — no schema change required, and sufficient for v1's scale.
- `allocation_history.change_type` gets a value-restricting `CHECK`; the other status/outcome text columns do not — only `change_type` was described as a "constrained enumeration" in `07-er-diagram.md`; the others rely on application-level validation, unchanged from what's already frozen.
- One composite index, `allocations(employee_id, status)`, added beyond `07-er-diagram.md`'s baseline, justified by the Capacity Validator's actual query shape and replacing rather than duplicating the plain `employee_id` index.

### Open Questions

- Should the other status/outcome columns (`projects.status`, `allocations.status`, `timesheets.status`, `approvals.outcome`) eventually get their own value-restricting `CHECK` constraints, for consistency with `change_type`? Deliberately not added here — that would be reopening a frozen document's scope without a new decision to do so, not a database-design-layer call to make unilaterally.
- Carried forward from `07-er-diagram.md`: should `TimeEntry` support removal, not just editing, while its Timesheet is in Draft?

### Deferred to v2

- Server-side (database-generated) UUIDs, if a reason to prefer them over client-side generation ever appears.
- A dedicated `RowVersion` column, if `xmin`-based concurrency proves insufficient against a concrete, observed conflict — not a hypothetical one.
- Any additional composite index beyond `allocations(employee_id, status)` — none is justified by a named query today.