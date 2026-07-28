# Chrona Engineering Guide

## Mission

Build Chrona as a production-grade enterprise application.

Do not optimize for speed.

Optimize for maintainability, correctness, and long-term architectural quality.

---

## Architecture

- Domain-Driven Design
- Modular Monolith
- Clean Architecture
- Vertical Slice Architecture
- CQRS where appropriate

---

## Core Domain

Work Allocation

This is the strategic core of the system.

Protect its model.

Avoid unnecessary coupling.

---

## Source of Truth

Always consult the documentation before implementing.

Priority:

1. docs/10-engineering
2. docs/00-product
3. docs/01-domain

---

## Engineering Rules

- Follow the Ubiquitous Language.
- Preserve bounded contexts.
- Never bypass the domain model.
- Never introduce cross-module coupling.
- Prefer business methods over anemic entities.
- Keep business rules inside the domain.

---

## Development Workflow

For every milestone:

1. Understand the domain.
2. Explain the implementation plan.
3. Implement incrementally.
4. Review the result.
5. Update documentation if necessary.

Never make architectural changes without justification.