# Git Workflow

**Document ID:** ENG-011

## Branches

main

Production-ready.

develop

Integration branch.

feature/<name>

New functionality.

fix/<name>

Bug fixes.

refactor/<name>

Internal improvements.

docs/<name>

Documentation only.

---

## Commits

Use Conventional Commits.

Examples:

feat(timesheet): submit weekly timesheet

fix(auth): prevent expired token reuse

docs(ddd): update work allocation aggregate

refactor(api): simplify employee endpoint

test(payroll): add overtime calculation tests

---

## Pull Requests

Every PR must include:

- Business objective
- Summary
- Testing performed
- Documentation updated
- ADR reference (if applicable)

---

## Rules

- Keep PRs small.
- Rebase before merge.
- Never commit secrets.
- Every commit should leave the project buildable.

## Final Rule

Git history is engineering documentation.