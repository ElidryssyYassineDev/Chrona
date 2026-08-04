# 06 — Domain Model

**Document ID:** SYS-006
**Status:** Draft — pending review
**Version:** 1.0.0

---

## 1. Purpose

A domain model names the business concepts a system must represent and the rules that govern them, independent of how they end up stored. `03-module-design.md` already decided which module owns which concept; `04-use-cases.md` and `05-business-processes.md` already showed those concepts in action. This document is the missing piece: it says precisely what an Allocation or a Timesheet *is* — its identity, its lifecycle, what it owns versus merely references — before a single Entity Framework class or database table gets written.

`08-database-design.md` will translate this into tables and columns; this document is deliberately silent on that translation, because a table structure is an implementation detail that should follow from the domain model, not shape it. If a later document's schema doesn't match what's decided here, the schema is wrong, not this one.

---

## 2. Core Domain

`03-module-design.md` already established why Work Allocation is the core domain at the module level — it's the one decision everything else exists to enable, validate, or report on. This document asks the same question from a different angle: at the level of individual business concepts, which one carries the most invariants, the richest lifecycle, and the most business rules that must never be violated? That's Work Allocation's `Allocation` aggregate — the only aggregate in this system with a genuine capacity constraint to protect, a multi-step lifecycle, and a rule (an allocation may not exceed capacity) with no equivalent complexity anywhere else in the domain.

The remaining modules' domain concepts support this in one of two ways: they supply something an Allocation needs to exist (an `Employee` from Workforce, a `Project` from Project Management), or they consume the fact that an Allocation exists (a `TimeEntry` validates against an active Allocation; a decision is really a decision about a `Timesheet`, which only exists because time was recorded against Allocations in the first place).

Authentication & Authorization and Dashboard do not appear in Sections 3 or 4 at all. As established in `03-module-design.md`, neither owns business data, so neither has a domain model of its own.

---

## 3. Aggregates

### Employee

- **Purpose:** represent a person who works at the organization, identifiable and referenceable by every other module.
- **Aggregate Root:** `Employee`
- **Invariants:** an Employee must resolve to exactly one Keycloak subject ID; an Employee must reference exactly one Department, and that Department must exist — Department is mandatory, not optional, resolving what an earlier draft of this section left ambiguous; an Employee may not be their own Manager; a deactivated Employee cannot be the target of a new Allocation (enforced by Work Allocation querying Workforce at the moment of allocation — not by Employee itself, which only guarantees what it alone can).
- **Owned Entities:** none.
- **Owned Value Objects:** none in v1 — name and the Department/Manager references are simple attributes, not rich enough on their own to justify a dedicated type.

### Department

- **Purpose:** name and group a set of Employees.
- **Aggregate Root:** `Department`
- **Invariants:** a Department's name must be unique; a Department cannot be removed while Employees are still assigned to it (`04-use-cases.md`, Manage Departments).
- **Owned Entities:** none.
- **Owned Value Objects:** none in v1.

### Project

- **Purpose:** represent a piece of work that Employees can be associated with and, later, allocated against.
- **Aggregate Root:** `Project`
- **Invariants:** a Project's name must be non-empty and unique (`05-business-processes.md`, Project Creation); an archived Project accepts no new Project Members and no new Allocations, but existing ones are unaffected (`05-business-processes.md`); archiving is permanent in v1 — reactivation is not designed (`04-use-cases.md`, Open Questions).
- **Owned Entities:** `ProjectMember`.
- **Owned Value Objects:** `ProjectStatus` (Active, Archived).

### Allocation *(Core Domain)*

- **Purpose:** reserve an Employee's capacity against a Project for a period, and protect that reservation from ever exceeding what the Employee actually has to give.
- **Aggregate Root:** `Allocation`
- **Invariants:**
  - The sum of an Employee's active Allocations' percentages, for any overlapping period, may not exceed 100%. v1 assumes a fixed 100% capacity per Employee — a configurable, per-employee capacity is not modeled.
  - `EmployeeId` and `ProjectId` are set at creation and never change afterward — changing either is a new Allocation, not a modification of an existing one.
  - Only Period and Percentage may be modified after creation.
  - Valid transitions only: Active → Cancelled (terminal). There is no "Completed" status: an Allocation whose period has simply elapsed is still Active in status terms. "Currently valid for recording time" means Status == Active **and** the entry's date falls within the Allocation's period, evaluated at the moment a Time Entry is added — not tracked as a separate lifecycle stage.
  - Every status transition and every change to Period or Percentage is recorded in `AllocationHistory` — including creation itself, which writes the first entry; an Allocation is never without at least one history record.
- **Owned Entities:** `AllocationHistory`.
- **Owned Value Objects:** `AllocationPeriod`, `AllocationPercentage`, `AllocationStatus`.

### Timesheet

- **Purpose:** collect one Employee's Time Entries for a reporting period, and carry that collection through submission and decision.
- **Aggregate Root:** `Timesheet`
- **Invariants:**
  - A Time Entry may be added or edited only while the Timesheet is in Draft status.
  - Each Time Entry's Allocation must be active and its period must cover the entry's date at the moment the entry is added (validated against Work Allocation, per `03-module-design.md`); the entry's date must also fall within the Timesheet's own reporting period — a date outside the Timesheet's period is rejected even if the Allocation itself would otherwise allow it.
  - A Timesheet cannot be submitted with zero Time Entries.
  - Valid transitions only: Draft → Submitted; Submitted → Approved (terminal, immutable from that point on); Submitted → Draft — this is what "rejected" means for the Timesheet itself. The rejection reason and the decision's history live in Approval Workflow's own `Approval` record, not on Timesheet (`03-module-design.md`).
  - The Timesheet records when it was most recently transitioned to Submitted, so a currently-pending decision can be told apart from a stale one after a resubmission (see `Approval`'s invariants below).
- **Owned Entities:** `TimeEntry`.
- **Owned Value Objects:** `TimesheetStatus` (Draft, Submitted, Approved), `TimesheetPeriod`.

### Approval

- **Purpose:** record a Manager's decision about one specific submission of a Timesheet, without owning the Timesheet itself.
- **Aggregate Root:** `Approval`
- **Invariants:**
  - The deciding Manager must have authority over the Timesheet's owning Employee, checked against Workforce at the moment of decision.
  - A Manager may not decide on their own Timesheet.
  - A rejection requires a reason; an approval does not.
  - A decision, once recorded, is never edited. If a Timesheet is rejected, corrected, and resubmitted, the eventual new decision is a new `Approval` record, not a change to the old one — "approval does not modify history."
  - A Timesheet's current submission is *pending* when the Timesheet is in Submitted status and no `Approval` record for it has a decision time later than the Timesheet's most recent submission time. This is how "pending" is derived without Timesheet needing any notion of its own decisions.
- **Owned Entities:** none.
- **Owned Value Objects:** `ApprovalOutcome` (Approved, Rejected).

---

## 4. Entities

This section covers every entity in the model — aggregate roots and child entities alike — from a different angle than Section 3: not what an aggregate owns and enforces as a whole, but each entity's own identity, lifecycle, and relationships to others. Some restatement of Section 3 is expected; the emphasis here is on lifecycle detail and relationships specifically.

### Employee

- **Responsibilities:** represent one person's identity, organizational placement, and reporting line within Chrona.
- **Identity:** a system-generated `EmployeeId`, distinct from the Keycloak subject ID it's linked to.
- **Lifecycle:** created (Onboarding) → active → deactivated (terminal in v1 — no reactivation use case exists).
- **Relationships:** references exactly one `Department`; may reference another `Employee` as its Manager (self-referencing, optional).

### Department

- **Responsibilities:** name and group a set of Employees.
- **Identity:** a system-generated `DepartmentId`.
- **Lifecycle:** created → renamed (any number of times) → removed (only once no Employee references it).
- **Relationships:** referenced by zero or more Employees; owns no other entity.

### Project

- **Responsibilities:** represent a piece of work Employees can be associated with and allocated against.
- **Identity:** a system-generated `ProjectId`.
- **Lifecycle:** created (Active) → archived (terminal in v1).
- **Relationships:** owns zero or more `ProjectMember` entities; referenced by zero or more Allocations by `ProjectId` only — Work Allocation never holds a reference to the Project entity itself (`03-module-design.md`).

### ProjectMember

- **Responsibilities:** record that a specific Employee is associated with a specific Project.
- **Identity:** exists only within its parent Project — identified by the pair (`ProjectId`, `EmployeeId`), not by a standalone ID of its own.
- **Lifecycle:** added → removed. No intermediate states.
- **Relationships:** exists only as part of a Project; references an `EmployeeId` validated against Workforce at the moment it's added, but does not hold Employee data itself.

### Allocation

- **Responsibilities:** hold the actual reservation of one Employee's capacity against one Project for one period, and enforce that the reservation never exceeds what's available.
- **Identity:** a system-generated `AllocationId`.
- **Lifecycle:** created (Active) → modified (any number of times, still Active) → cancelled (terminal). No "completed" state — see Section 3.
- **Relationships:** references one `EmployeeId` and one `ProjectId`, both immutable after creation; owns its own `AllocationHistory` entries; referenced by zero or more `TimeEntry` records by `AllocationId` only.

### AllocationHistory

- **Responsibilities:** record what changed about an Allocation and when, for that Allocation's lifetime.
- **Identity:** exists only within its parent Allocation — an ordered sequence of entries, not independently addressable.
- **Lifecycle:** append-only, and never empty — creating the parent Allocation writes the first entry. A further entry is written whenever the Allocation's status, period, or percentage changes thereafter; existing entries are never edited or removed.
- **Relationships:** exists only as part of an Allocation.

### Timesheet

- **Responsibilities:** collect one Employee's Time Entries for a reporting period, and track that collection through submission.
- **Identity:** a system-generated `TimesheetId`.
- **Lifecycle:** created (Draft) → submitted → approved (terminal) or reopened to Draft on rejection (any number of times).
- **Relationships:** references one `EmployeeId`; owns zero or more `TimeEntry` entities; the subject of zero or more `Approval` records over its lifetime — one per submission that reached a decision.

### TimeEntry

- **Responsibilities:** record hours worked on a specific date against a specific Allocation.
- **Identity:** exists only within its parent Timesheet.
- **Lifecycle:** created (while the Timesheet is Draft) → edited any number of times (while still Draft) → frozen once the Timesheet is Submitted.
- **Relationships:** exists only as part of a Timesheet; references an `AllocationId`, validated against Work Allocation at the moment it's added, but does not hold Allocation data itself.

### Approval

- **Responsibilities:** record one Manager's decision about one submission of one Timesheet.
- **Identity:** a system-generated `ApprovalId`.
- **Lifecycle:** created once, at the moment a decision is made. Immutable from that point on.
- **Relationships:** references one `TimesheetId` and the deciding Manager's `EmployeeId`; does not hold Timesheet or Employee data itself, and is never referenced *by* Timesheet — Approval Workflow queries Time Management, not the other way around, matching `03-module-design.md`'s dependency direction exactly.

---

## 5. Value Objects

A value object is defined entirely by its data, not by an identity — two `AllocationPercentage`s with the same number are the same value object, in a way two Employees with the same name are still two different Employees. None of the value objects below are ever looked up or referenced by ID; each exists only as data owned by exactly one entity or aggregate, and is replaced wholesale rather than mutated in place when it changes. Every value object named in Section 3 is detailed here, grouped by the aggregate that owns it.

### Project

**ProjectStatus**
- **Represents:** whether a Project currently accepts new Project Members and new Allocations.
- **Fields:** an enumeration — Active, Archived.
- **Validation Rules:** must be one of the two values. The *transition* rule (Active → Archived, permanent in v1) belongs to Project's own invariants (Section 3), not to this value object — a value object validates its own shape, not the rules governing how an aggregate changes it over time.
- **Notes:** —

### Allocation

**AllocationPeriod**
- **Represents:** the date range an Allocation covers.
- **Fields:** `StartDate`, `EndDate`.
- **Validation Rules:** `StartDate` must be on or before `EndDate`; both are required.
- **Notes:** two `AllocationPeriod`s are equal only if both dates match. This is the value Allocation's capacity check (Section 3) compares across an Employee's Allocations to determine overlap.

**AllocationPercentage**
- **Represents:** how much of an Employee's assumed 100% capacity a single Allocation reserves.
- **Fields:** a single numeric value, `0 < value ≤ 100`.
- **Validation Rules:** must be greater than zero — an Allocation reserving 0% isn't meaningfully an allocation — and no greater than 100, since a single Allocation cannot alone claim more than an Employee's entire capacity. The cross-Allocation *sum* check is Allocation's own invariant (Section 3); this value object only bounds one number.
- **Notes:** expressed as a percentage rather than absolute hours deliberately — v1 doesn't model a configurable per-employee weekly-hours capacity (Section 3), so a percentage is the only unit that stays meaningful without one.

**AllocationStatus**
- **Represents:** whether an Allocation is currently in force.
- **Fields:** an enumeration — Active, Cancelled.
- **Validation Rules:** must be one of the two values; the transition rule (Active → Cancelled only) is Allocation's own invariant, not this value object's concern.
- **Notes:** —

### Timesheet

**TimesheetStatus**
- **Represents:** where a Timesheet currently sits in its submission and decision lifecycle.
- **Fields:** an enumeration — Draft, Submitted, Approved.
- **Validation Rules:** must be one of the three values. Transition rules belong to Timesheet's own invariants (Section 3) — including the point already made there: there is no stored "Rejected" value, since a rejection is represented by returning to Draft, not by a fourth status.
- **Notes:** —

**TimesheetPeriod**
- **Represents:** the reporting period a Timesheet covers.
- **Fields:** `StartDate`, `EndDate`.
- **Validation Rules:** `StartDate` must be on or before `EndDate`.
- **Notes:** v1 does not enforce a specific length (e.g., exactly one week) at the value-object level. The Ubiquitous Language describes weekly as the *usual* case, not a hard rule, and nothing so far has required making it one — worth confirming explicitly if a Design Decisions pass is added to this document.

### Approval

**ApprovalOutcome**
- **Represents:** the result of a Manager's decision on a Timesheet submission.
- **Fields:** an enumeration — Approved, Rejected.
- **Validation Rules:** must be one of the two values. When Rejected, `Approval`'s own invariant (Section 3) requires a reason to accompany it — that pairing rule belongs to the aggregate, not to this value object, which only validates that the outcome itself is a known value.
- **Notes:** —

---

## 6. Domain Services

A domain service holds business logic that doesn't belong to any single aggregate instance, because it needs to look across multiple instances of the same aggregate type to do its job. Both services below stay within one module — Capacity Validator inside Work Allocation, Manager Authority Resolver inside Workforce — deliberately: logic that spans a *module* boundary, like determining whether a Timesheet's current submission is still pending a decision, is an application-layer orchestration concern (`03-module-design.md`, Section 5), not a domain service in this document's sense. A domain service that reached across modules would quietly reintroduce exactly the coupling `03-module-design.md`'s Dependency Rules exist to prevent.

### Capacity Validator

- **Belongs to:** Work Allocation.
- **Responsibility:** given a proposed Allocation — new, or an existing one being modified — its Employee, period, and percentage, determine whether accepting it would push that Employee's overlapping active Allocations over 100%.
- **Why it's a service, not a method on Allocation:** a single `Allocation` instance only knows its own period and percentage. Answering "would this exceed capacity" requires the Employee's *other* active Allocations too — data living in different instances of the same aggregate, which is exactly what a domain service is for.
- **Used by:** Create Allocation and Modify Allocation (`04-use-cases.md`), at the point described in Employee Allocation's main flow (`05-business-processes.md`).

### Manager Authority Resolver

- **Belongs to:** Workforce.
- **Responsibility:** given two Employees, determine whether the first has management authority over the second.
- **Why it's a service, not a method on Employee:** authority isn't a property of one Employee record in isolation — it's a relationship between two separate Employee records, which is exactly what a domain service is for. Chrona v1 uses a single direct-manager hierarchy — every Employee has at most one Manager, and authority is a direct comparison, not a traversal.
- **Used by:** `IsManagerOf`, the capability Workforce exposes to Approval Workflow (`03-module-design.md`), and Review Timesheet / Approve Timesheet / Reject Timesheet (`04-use-cases.md`).

---

## 7. Domain Events

A domain event records something significant that already happened, published specifically so code elsewhere can react to it without the aggregate that raised it needing to know who's listening, or whether anyone is. This is different from the history entries introduced in Section 4 (`AllocationHistory`): a history entry is private bookkeeping an aggregate keeps about itself, never dispatched anywhere; a domain event is dispatched precisely because something *outside* the aggregate needs to know.

v1's domain model has exactly one domain event with an established consumer:

### EmployeeDeactivated

- **Raised by:** `Employee` (Workforce), when an existing Employee is deactivated (`04-use-cases.md`, Manage Employees).
- **Payload:** `EmployeeId`, `DeactivatedAtUtc`.
- **Handled by:** Work Allocation, which cancels every currently-Active Allocation for that Employee — regardless of whether the Allocation's period has already elapsed, since a deactivated Employee cannot be doing any work at all, active or otherwise (`03-module-design.md`, Section 5).
- **Why an event and not a direct call:** Workforce doesn't need Work Allocation's response and shouldn't need to know it exists to do its own job — deactivating an Employee must succeed on its own terms. This is the same reasoning `03-module-design.md` used to justify this exact event as the one clear case for publish/subscribe over a direct call.

No other state change in this model is published as a domain event in v1. Every other significant fact — a Project being archived, an Allocation being created or cancelled, a Timesheet being submitted — is either captured privately as history (`AllocationHistory`) or queried on demand by whichever module needs to know, following the pull pattern established throughout `03-module-design.md`, `04-use-cases.md`, and `05-business-processes.md`. Publishing an event for a state with no current listener would be exactly the kind of premature complexity the project's engineering principles ask to be justified by real evidence, not built in advance of needing it. If Project archiving is later decided to retroactively affect open Allocations (flagged as an open question in `04-use-cases.md` and `05-business-processes.md`), `ProjectArchived` becoming a real domain event is the natural way to implement that — not before the decision is made.

---

## 8. Domain Invariants

Section 3 stated each aggregate's invariants individually. This section pulls them together by *kind*, because the same handful of patterns repeat across the model — worth seeing once, together, rather than re-derived six separate times.

**1. Existence-and-activity validation, at the moment of reference, never re-checked afterward**
Every time one aggregate references another by ID, that reference is validated as existing and currently active at the moment of the reference — not on any schedule afterward:
- Project Member Assignment validates the Employee (Workforce).
- Allocation creation validates both the Employee (Workforce) and the Project (Project Management).
- Time Entry creation validates the Allocation (Work Allocation) is active and its period covers the entry's date.
- Approval decisions validate the deciding Manager's authority over the Employee (Workforce).

None of these are re-verified after the fact — an Allocation created against an active Project remains valid even if that Project is later archived; archiving only blocks *new* references (Section 3, Project). One consistent rule, applied four times, not four separate rules.

**2. Capacity cannot be exceeded**
The one quantitative constraint in the model: an Employee's overlapping active Allocations may never sum past 100% (Section 3, Allocation). Enforced by the Capacity Validator (Section 6) at the moment of creation or modification — never re-verified on a schedule, since nothing in this model changes what an Allocation reserves except a create or modify, which is exactly when the check already runs.

**3. Immutability after a terminal state**
Several aggregates have a genuinely terminal state, after which they stop changing:
- `Approval`, from the moment it's created.
- `Timesheet`, once Approved.
- `Project` and `Allocation`, once Archived or Cancelled respectively — terminal for that specific concern only. They remain fully readable and their history stays intact; nothing here is deleted, only closed to further change.

**4. Rejection is the only backward transition in the model**
`Timesheet` moving from Submitted back to Draft, on rejection, is the sole transition anywhere in this model that goes backward. A Cancelled Allocation stays Cancelled; an Archived Project stays Archived; a decided Approval is never revisited. This asymmetry is deliberate — rejection exists specifically so an Employee can correct a mistake, and nothing else in the model has an equivalent correct-and-retry need.

**5. Authority is decided in exactly one place**
Every authorization-flavored invariant is enforced by whichever aggregate or service owns the underlying relationship, never duplicated — the Manager Authority Resolver (Section 6, Workforce) is the only place "does X manage Y" is decided; no other aggregate keeps its own copy of that answer. This mirrors `03-module-design.md`'s Dependency Rule 7 (Workforce is the sole owner of the manager/employee relationship) at the domain-model level, not just the module level.

---

## 9. Design Decisions

### Decisions made

- `EmployeeDeactivated` is the only domain event with a real consumer in v1; every other significant state change is either private aggregate history or resolved by an on-demand query, not a published event.
- Employee capacity is a fixed, assumed 100% in v1 — not individually configurable — which is why an Allocation's reservation is expressed as a percentage rather than absolute hours.
- Project Membership and Employee Allocation are modeled as independent: Allocation's invariants (Section 3) do not require the Employee to already be a Project Member. An Employee can be allocated to a Project without formal membership, and can be a Member without ever being allocated.
- A Timesheet's "pending" state is derived by comparing timestamps — most recent submission versus most recent decision — rather than stored as its own status value, avoiding the need for a version number or a fourth `TimesheetStatus`.
- Rejection is modeled as a transition back to Draft, not as a distinct `TimesheetStatus` value — the rejection fact and reason live entirely in `Approval`, consistent with `03-module-design.md`'s module boundaries.
- Chrona v1 uses a single direct-manager hierarchy — every Employee has at most one Manager, and `Employee.ManagerId` alone is sufficient for Manager Authority Resolver; no chain traversal is needed (`03-module-design.md`).

### Open Questions

- Should Employee Allocation actually require prior Project Membership as a precondition? The model as designed treats them as independent (see Decisions above) — worth explicit confirmation, since it's easy to assume the opposite.
- Should `TimesheetPeriod` enforce a specific length (e.g., exactly one week), or stay open-ended as currently modeled? (Carried forward from Section 5.)
- Should `Timesheet` gain its own structured history entity, symmetrical to `AllocationHistory`, or does the combination of its `TimeEntry` records and status remain sufficient in v1?

### Deferred to v2

- `ProjectArchived` becoming a real, published domain event — relevant only if Project archiving is later decided to retroactively affect open Allocations, an open question in `04-use-cases.md` and `05-business-processes.md` that hasn't been resolved either way.
- Configurable, per-employee capacity, replacing the fixed 100% assumption, if a real need for it appears.
- Any richer audit/history mechanism beyond `AllocationHistory` — not needed until a concrete requirement, such as a compliance need, makes it necessary.