# Testing Standard

**Document ID:** ENG-009

## Purpose

Protect business knowledge through automated tests.

## Testing Priority

1. Domain
2. Application
3. Integration
4. API
5. UI

## Principles

- Test behavior, not implementation.
- One business concept per test.
- Tests must be deterministic.
- Keep tests independent.

## Naming

Given_When_Then

Example:

Employee_SubmitsApprovedTimesheet_ShouldCreatePayrollEntry

## Coverage Philosophy

Coverage is an indicator—not a goal.

Critical business rules require strong test coverage.

## Final Rule

If business behavior changes, a failing test should reveal it.