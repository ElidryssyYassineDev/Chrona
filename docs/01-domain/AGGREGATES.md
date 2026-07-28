# Aggregates

## Aggregate Root

### WorkAllocation

Purpose:

Maintain consistency while planning and executing employee work allocations.

---

## Responsibilities

- Allocate work
- Validate capacity
- Track lifecycle
- Publish domain events
- Enforce allocation policies

---

## Child Entities

- Assignment
- AllocationHistory

---

## Value Objects

- AllocationPeriod
- Capacity
- Availability
- SkillRequirement
- Workload
- AllocationStatus

---

## Invariants

- Capacity cannot be exceeded.
- Allocation periods must be valid.
- Assignments belong to one allocation.
- Invalid state transitions are forbidden.
- Every important transition is audited.
- Cross-tenant operations are forbidden.

---

## Domain Events

- WorkAllocationCreated
- EmployeeAssigned
- AssignmentAccepted
- AssignmentRejected
- AssignmentForced
- WorkAllocationActivated
- WorkAllocationCompleted
- WorkAllocationCancelled
- WorkAllocationArchived