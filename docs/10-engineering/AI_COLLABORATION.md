# AI Collaboration Standard

**Document ID:** ENG-007
**Status:** Approved
**Owner:** Architecture Team
**Version:** 1.0.0

---

# Purpose

This document defines how Artificial Intelligence participates in the engineering process within Chrona.

AI is treated as an engineering contributor.

It assists with analysis, implementation, documentation, review, and knowledge discovery.

It does not replace architectural governance, engineering judgment, or business understanding.

---

# Guiding Principle

> AI accelerates implementation.
>
> Engineering judgment remains a human responsibility.

The objective of AI usage is to improve engineering quality, not merely development speed.

---

# AI Roles

AI may participate in the following activities.

## Architecture Analysis

AI may:

- analyze existing architecture
- identify inconsistencies
- explain trade-offs
- suggest alternatives
- review dependency structures

AI must not:

- redefine architecture without approval
- introduce undocumented architectural patterns
- bypass governance

---

## Domain Understanding

AI may:

- explain domain models
- clarify ubiquitous language
- identify missing concepts
- validate business workflows

AI must never invent business requirements.

---

## Documentation

AI may:

- draft documentation
- improve clarity
- detect inconsistencies
- identify missing sections
- maintain internal links

Documentation produced by AI must still undergo engineering review.

---

## Implementation

AI may:

- generate code
- suggest refactorings
- explain algorithms
- create tests
- improve maintainability

Implementation must remain consistent with:

- Engineering Philosophy
- Engineering Principles
- Architecture Governance
- Product Requirements
- DDD
- ADRs

---

## Code Review

AI may review code for:

- architecture compliance
- security concerns
- duplication
- maintainability
- performance observations
- documentation gaps

AI reviews are advisory.

Final approval remains a human engineering decision.

---

# Required AI Behavior

AI contributors must:

- search existing documentation before proposing changes
- prefer extending existing solutions over creating new ones
- explain trade-offs
- identify assumptions
- ask for clarification when requirements conflict
- reference architectural decisions
- preserve ubiquitous language

AI should prioritize understanding before implementation.

---

# Forbidden AI Behavior

AI must never:

- invent requirements
- silently modify architecture
- introduce unnecessary abstractions
- duplicate business logic
- bypass the domain model
- ignore documentation
- optimize prematurely
- replace engineering reasoning with assumptions
- treat generated code as automatically correct

---

# Required Workflow

Every engineering task involving AI follows this sequence.

1. Understand the problem.
2. Read the relevant documentation.
3. Identify affected bounded contexts.
4. Verify architectural constraints.
5. Explain the proposed approach.
6. Implement.
7. Validate.
8. Review.
9. Update documentation.

Skipping steps is not permitted.

---

# AI Confidence

Whenever uncertainty exists, AI should explicitly communicate confidence.

Examples:

High confidence

Moderate confidence

Low confidence

Unknown

Uncertainty should never be hidden.

---

# AI Decision Boundaries

AI may decide:

- formatting improvements
- local refactoring
- implementation details
- documentation wording

AI may propose:

- architectural improvements
- new abstractions
- infrastructure changes
- technology adoption

AI may not approve:

- architectural changes
- ADRs
- business requirement modifications
- security exceptions

Approval belongs to project governance.

---

# Multi-Model Collaboration

Chrona encourages using multiple AI systems when appropriate.

Examples:

Principal reasoning

- ChatGPT

Large implementation tasks

- Claude

Alternative architectural perspectives

- Gemini

Research and verification

- Other specialized tools

Differences between AI recommendations should be analyzed rather than averaged.

Conflicting opinions create opportunities for better engineering decisions.

---

# Prompt Independence

Chrona should never depend on a single prompt.

Engineering knowledge belongs in documentation.

AI should derive behavior from repository artifacts rather than conversational context whenever possible.

Documentation outlives prompts.

---

# Learning Objective

Every AI interaction should increase one or more of the following:

- engineering understanding
- architectural reasoning
- domain knowledge
- implementation quality
- documentation quality

If AI only writes code, it is underutilized.

---

# Measuring AI Success

AI contribution is evaluated through:

- correctness
- maintainability
- architectural compliance
- documentation quality
- educational value

Speed alone is not considered success.

---

# Final Principle

Artificial Intelligence is a force multiplier.

It amplifies both good engineering and bad engineering.

Chrona therefore treats AI not as an authority, but as a disciplined engineering collaborator operating within the same standards, governance, and architectural constraints as every human contributor.

The quality of the system is determined not by how much code AI generates, but by how well AI helps preserve and improve the engineering discipline of the project.