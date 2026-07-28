# Domain Model – Work Allocation

## Aggregate Root

### WorkAllocation

Represents the planning and management of work assigned to employees.

Owns:

- Assignments
- Allocation Period
- Status
- Required Skills

References:

- EmployeeId
- ProjectId

---

## Entities

### Assignment

Represents a concrete unit of assigned work.

### AllocationHistory

Maintains the history of business-significant changes.

---

## Value Objects

- AllocationPeriod
- Capacity
- Availability
- SkillRequirement
- Workload
- AllocationStatus

---

## Domain Services

- AllocationPlanner
- CapacityCalculator
- ConflictDetector
- AllocationPolicyEngine