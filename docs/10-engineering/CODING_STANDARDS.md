# Coding Standards

**Document ID:** ENG-008

## Purpose

Ensure consistency, readability, and maintainability across the codebase.

## General

- Prefer clarity over cleverness.
- Code should explain intent.
- Small, cohesive classes and methods.
- Eliminate duplication before adding abstractions.
- Follow the ubiquitous language.

## Naming

- Names represent business concepts.
- Avoid generic names (Manager, Helper, Utils).
- Use verbs for behaviors and nouns for entities.

## Error Handling

- Fail fast.
- Never swallow exceptions.
- Surface meaningful domain errors.
- Log unexpected failures only.

## Dependency Injection

- Depend on abstractions.
- Constructor injection by default.
- Avoid Service Locator.

## Logging

- Structured logging.
- No sensitive information.
- Log business events, not every method call.

## Configuration

- Never hardcode secrets.
- Environment-specific configuration only.

## Comments

- Explain *why*, not *what*.
- Prefer self-explanatory code.

## Final Rule

Readable code is the default.