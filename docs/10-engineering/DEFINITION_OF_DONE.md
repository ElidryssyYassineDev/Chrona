# Definition of Done

**Document ID:** ENG-006  
**Status:** Approved  
**Owner:** Architecture Team  
**Version:** 1.0.0

---

# Purpose

This document defines the minimum quality standard required before any work is considered complete within Chrona.

Completion is not determined by implementation alone.

A feature is considered "Done" only when it satisfies business, architectural, engineering, documentation, testing, and operational expectations.

---

# Core Principle

Working software is necessary.

Healthy software is required.

Every completed change should improve or preserve the long-term quality of Chrona.

---

# Definition of Done Checklist

A contribution is complete only when all applicable items have been satisfied.

---

## 1. Business

✓ Requirement implemented

✓ Acceptance criteria satisfied

✓ Business rules respected

✓ Edge cases considered

✓ Ubiquitous language preserved

---

## 2. Architecture

✓ Module boundaries respected

✓ Dependency rules preserved

✓ No architectural violations introduced

✓ Existing patterns reused

✓ No unnecessary abstractions

✓ ADR created if required

---

## 3. Implementation

✓ Code is understandable

✓ Naming follows ubiquitous language

✓ No duplicated logic

✓ Errors handled appropriately

✓ Validation implemented

✓ Logging added where appropriate

---

## 4. Testing

✓ Business rules tested

✓ Integration tests updated

✓ Regression risk evaluated

✓ Existing tests continue passing

✓ Critical paths verified

---

## 5. Security

✓ Authorization verified

✓ Authentication assumptions validated

✓ Inputs validated

✓ Sensitive data protected

✓ Security implications reviewed

---

## 6. Documentation

✓ Documentation updated

✓ API documentation updated

✓ ADR referenced if applicable

✓ Architecture documents updated when necessary

---

## 7. Observability

✓ Meaningful logs

✓ Useful error messages

✓ Metrics where appropriate

✓ Tracing preserved

---

## 8. Deployment

✓ Builds successfully

✓ Configuration documented

✓ Migrations validated

✓ No deployment blockers

---

## 9. Review

✓ Code reviewed

✓ Feedback addressed

✓ Review comments resolved

✓ Quality gates passed

---

## 10. Learning

Every completed contribution should answer at least one of the following:

- What engineering concept did we learn?
- What architectural decision did we validate?
- What trade-off did we better understand?
- What documentation became clearer?

Chrona values learning as a deliverable.

---

# Maturity Levels

Every feature should be classified before implementation.

## Level 1 — Functional

The feature works correctly.

---

## Level 2 — Professional

Documentation.

Tests.

Validation.

Logging.

Readable implementation.

---

## Level 3 — Enterprise

Architecture compliance.

DDD alignment.

Security review.

Observability.

Automation.

---

## Level 4 — Production Ready

Performance validated.

Operational readiness.

Recovery strategy.

Monitoring.

Deployment verified.

---

## Level 5 — Reference Quality

The implementation demonstrates engineering practices worthy of reuse as an educational reference or enterprise exemplar.

---

# Reasons Work Is Not Done

A task is incomplete if:

- It works but violates architecture.
- It passes tests but lacks documentation.
- It introduces undocumented technical debt.
- It bypasses business rules.
- It duplicates existing functionality.
- It introduces security concerns.
- It cannot be understood by another engineer.

---

# Continuous Improvement

The Definition of Done evolves with Chrona.

As engineering maturity increases, new quality expectations may be introduced.

The standard should rise over time—not fall.

---

# Final Principle

Done is not the moment code is merged.

Done is the moment the system is healthier than it was before the change.

Every completed contribution should make future development easier rather than harder.

That is the standard Chrona strives to achieve.