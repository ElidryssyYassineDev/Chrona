# Code Review Standard

**Document ID:** ENG-005  
**Status:** Approved  
**Owner:** Architecture Team  
**Version:** 1.0.0

---

# Purpose

This document defines the mandatory review process for all code contributions to Chrona.

Code review is not a search for mistakes.

It is a collaborative engineering activity that protects the architecture, improves maintainability, preserves business correctness, and transfers knowledge between contributors.

Every merged change becomes part of Chrona's long-term maintenance burden.

The purpose of review is to ensure that this burden remains manageable.

---

# Review Philosophy

A successful review answers one question:

> Does this change leave Chrona in a healthier state than before?

Working software alone is not sufficient.

A contribution is considered successful only when it also improves or preserves:

- architectural integrity
- readability
- maintainability
- correctness
- security
- documentation
- testability

---

# Review Priorities

Reviews must evaluate concerns in the following order.

## 1. Business Correctness

Questions:

- Does the implementation satisfy the documented requirement?
- Are business rules preserved?
- Are edge cases handled?
- Does the implementation respect the ubiquitous language?

---

## 2. Architecture

Questions:

- Does the implementation respect module boundaries?
- Are dependency rules preserved?
- Is business logic located in the correct layer?
- Does the implementation align with existing architectural decisions?
- Is the solution unnecessarily coupled?

---

## 3. Security

Questions:

- Is authorization enforced?
- Is authentication assumed safely?
- Are inputs validated?
- Could this introduce security vulnerabilities?
- Are secrets handled correctly?
- Is sensitive data exposed?

---

## 4. Maintainability

Questions:

- Can another engineer understand this in six months?
- Are names meaningful?
- Is complexity justified?
- Is duplication introduced?
- Is technical debt documented?

---

## 5. Testing

Questions:

- Are business rules protected?
- Are critical paths tested?
- Are tests understandable?
- Do tests verify behavior instead of implementation?

---

## 6. Performance

Questions:

- Are obvious bottlenecks introduced?
- Are expensive operations justified?
- Is optimization evidence-based?
- Does performance impact business requirements?

---

## 7. Style

Formatting, naming conventions, and minor consistency issues should generally be enforced through automated tooling.

Reviewers should avoid spending significant effort on issues that automation can detect.

---

# Review Severity Levels

## Critical

Must be fixed before merge.

Examples:

- Incorrect business behavior
- Security vulnerabilities
- Architectural violations
- Data corruption risks
- Missing authorization
- Broken tests

---

## Major

Should normally be resolved before merge.

Examples:

- High complexity
- Poor abstraction
- Significant duplication
- Missing validation
- Inadequate documentation
- Missing tests for important business rules

---

## Minor

May be addressed before or after merge.

Examples:

- Naming improvements
- Small refactorings
- Readability suggestions
- Documentation wording

---

## Suggestion

Optional improvements.

Examples:

- Alternative implementation ideas
- Future refactoring opportunities
- Performance observations
- API ergonomics

Suggestions should never block delivery.

---

# Pull Request Expectations

Every Pull Request should clearly explain:

## Business Problem

Why is this change necessary?

---

## Solution

How does this implementation solve the problem?

---

## Architectural Impact

Does this affect:

- modules
- dependencies
- APIs
- persistence
- infrastructure
- security

If yes, reference the relevant documentation or ADR.

---

## Testing

Explain how the change was verified.

---

## Documentation

List every document updated.

If none were updated, explain why.

---

# AI-Generated Code

AI-generated code is reviewed using exactly the same standards as human-written code.

Additional review questions include:

- Does the implementation match Chrona's documented architecture?
- Did AI introduce unnecessary abstractions?
- Did AI duplicate existing functionality?
- Does the generated code follow established patterns?
- Were architectural assumptions invented?

Generated code receives no special treatment.

Correctness and maintainability determine acceptance.

---

# Reviewer Responsibilities

Reviewers should:

- Assume positive intent.
- Explain reasoning.
- Reference documentation whenever possible.
- Challenge ideas, not people.
- Protect long-term maintainability.
- Encourage knowledge sharing.

Reviews should educate rather than merely approve or reject.

---

# Author Responsibilities

Authors should:

- Keep Pull Requests focused.
- Provide sufficient context.
- Respond constructively to feedback.
- Update documentation when required.
- Avoid defensive discussions.
- Seek clarification when feedback is unclear.

The objective is improving the product, not defending an implementation.

---

# Merge Criteria

A Pull Request may be merged only when:

- All critical issues are resolved.
- Major concerns are addressed or explicitly accepted.
- Required documentation is updated.
- Relevant tests pass.
- Architecture remains compliant.
- Business requirements are satisfied.

Approval is a statement of engineering confidence—not a guarantee of perfection.

---

# Anti-Patterns

Reviewers should avoid:

- Reviewing formatting instead of architecture.
- Requesting personal style preferences.
- Introducing unnecessary complexity.
- Blocking progress without justification.
- Approving code without understanding it.
- Ignoring documentation inconsistencies.

---

# Continuous Improvement

The review process itself should evolve.

If recurring review comments appear across multiple Pull Requests, the underlying guidance should be improved through:

- documentation updates
- coding standards
- automated tooling
- architecture refinements

The best review comment is the one that never needs to be written again because the process has improved.

---

# Final Principle

Code review is not a gate.

It is a feedback loop.

Its purpose is to protect Chrona's architecture, improve engineering quality, and help every contributor become a better software engineer.

Every review should leave both the codebase and the engineer stronger than before.