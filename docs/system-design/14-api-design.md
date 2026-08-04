# 14 — API Design

**Document ID:** SYS-014
**Status:** Draft — pending review
**Version:** 1.0.0

---

## 1. Purpose

Chrona v1 exposes exactly one API: a resource-oriented REST API, communicating in JSON, called by the React Frontend and validated statelessly on every request via a JWT Bearer token (`02-system-context.md`, `09-sequence-diagrams.md`). This document defines the shape of that API — resource names, methods, status codes, and representative endpoints — without generating the controllers, DTOs, or OpenAPI specification that would implement it. Consistent endpoint naming matters here for the same reason consistent module boundaries mattered in `03-module-design.md`: an API a developer can predict is one they make fewer mistakes calling.

---

## 2. API Principles

**Resource naming.** Plural nouns, lowercase, kebab-case for multi-word resources — `/employees`, `/time-entries`. A sub-resource nests under its parent when it has no independent existence outside it, matching the composition relationships already established in `06-domain-model.md` and `10-class-diagrams.md`: `/projects/{id}/members`, `/timesheets/{id}/time-entries`. A cross-aggregate reference never nests — `/allocations` is its own top-level resource, not `/employees/{id}/allocations`, because an Allocation isn't owned by Employee (`06-domain-model.md`, Section 4).

**HTTP methods.** `GET` for every read, with no side effects — matches `03-module-design.md`'s Query classification exactly. `POST` for creating a resource, and for business actions that are more than a field update — `/submit`, `/approve`, `/reject`, `/cancel`, `/archive`, `/deactivate` — where the action itself, not just the resulting state, is the thing being recorded (`06-domain-model.md`'s invariants govern several of these transitions directly). `PUT` for a full update of a resource's mutable fields. `DELETE` only where `07-er-diagram.md` established a genuine hard delete — `Department` and Project Member. `PATCH` (partial update) is not used anywhere in this API: every update in Chrona v1 either replaces a small, complete set of fields (`PUT` suffices) or is a named action with its own business meaning (a dedicated endpoint suffices) — nothing calls for field-by-field partial patching.

**Status codes.** `200` for a successful read or update; `201` for a successful creation; `204` for a successful action with no response body. `400` for a request that's malformed or fails input validation before it ever reaches a module. `401` for a missing or invalid token. `403` for a valid token lacking the required role or authority. `404` for a resource that doesn't exist. `409` for a request that's well-formed but conflicts with the resource's current state — a business rule violation, not a shape problem: capacity exceeded, a Timesheet already submitted, a self-approval attempt. Separating `400` from `409` mirrors `03-module-design.md`'s own distinction between validating a request's shape and enforcing a business invariant — they're different kinds of failure, and the status code says which kind before the error body does.

**Error responses.** One consistent envelope for every non-2xx response, detailed in Section 6.

**Pagination.** List endpoints accept `?page=` and `?pageSize=` query parameters, defaulting to a reasonable page size. Included as a consistent convention every list endpoint follows — at the scale `01-system-overview.md` scopes this system for (20–100 employees), most lists will never fill a second page, but the convention costs nothing to apply uniformly now and avoids an inconsistent retrofit later.

**Filtering.** Query parameters scoped to fields a caller would realistically filter by — `GET /api/v1/allocations?employeeId=` or `GET /api/v1/timesheets?status=Submitted` (the query `04-use-cases.md`'s View Pending Approvals actually needs, and the reason `08-database-design.md` indexes `timesheets.status`). No general-purpose query language — a fixed, small set of filters per resource, matching what's actually asked for anywhere in `04-use-cases.md`.

**Validation.** Input shape and type validation — required fields present, correct types, well-formed values — happens at the API boundary, using the shared Validation component from `12-component-diagram.md`, before a request ever reaches a module's Application layer. Business-rule validation — capacity, authority, immutability — happens inside the module that owns the rule (`03-module-design.md`), and is what produces a `409`, not a `400`.

---

## 3. Authentication

Login itself is handled entirely by Keycloak, via the OIDC Authorization Code flow with PKCE, exactly as described in `02-system-context.md` and `09-sequence-diagrams.md` — not repeated here. What matters for this document is what arrives at the API afterward: every request other than the login redirect itself carries a JWT access token in the `Authorization: Bearer <token>` header. The API validates that token's signature, issuer, audience, and expiry (`02-system-context.md`), then authorizes the request against the role(s) carried in the token — Employee or Manager, per `04-use-cases.md`'s Permission Matrix. A request with no token, or an invalid one, never reaches a module at all; it's rejected by the same middleware for every endpoint in this document, not re-implemented per module.

---

## 4. Module Endpoints

### Workforce

| Method | URL | Purpose | Required Role(s) |
|---|---|---|---|
| GET | `/api/v1/employees` | List employees | Manager |
| GET | `/api/v1/employees/{id}` | View an employee | Manager |
| GET | `/api/v1/employees/me` | View my own profile | Employee, Manager |
| POST | `/api/v1/employees` | Create an employee | Manager |
| PUT | `/api/v1/employees/{id}` | Update an employee | Manager |
| POST | `/api/v1/employees/{id}/deactivate` | Deactivate an employee | Manager |
| GET | `/api/v1/departments` | List departments | Employee, Manager |
| POST | `/api/v1/departments` | Create a department | Manager |
| PUT | `/api/v1/departments/{id}` | Rename a department | Manager |
| DELETE | `/api/v1/departments/{id}` | Remove a department | Manager |

### Project Management

| Method | URL | Purpose | Required Role(s) |
|---|---|---|---|
| GET | `/api/v1/projects` | List projects | Employee, Manager |
| GET | `/api/v1/projects/{id}` | View a project | Employee, Manager |
| POST | `/api/v1/projects` | Create a project | Manager |
| PUT | `/api/v1/projects/{id}` | Edit a project | Manager |
| POST | `/api/v1/projects/{id}/archive` | Archive a project | Manager |
| GET | `/api/v1/projects/{id}/members` | List a project's members | Employee, Manager |
| POST | `/api/v1/projects/{id}/members` | Add a project member | Manager |
| DELETE | `/api/v1/projects/{id}/members/{employeeId}` | Remove a project member | Manager |

The Archive transition is defined in `11-state-diagrams.md`, Section 4.

### Work Allocation

| Method | URL | Purpose | Required Role(s) |
|---|---|---|---|
| GET | `/api/v1/allocations` | List allocations, scoped to the caller | Employee, Manager |
| GET | `/api/v1/allocations/{id}` | View an allocation | Employee, Manager |
| POST | `/api/v1/allocations` | Create an allocation | Manager |
| PUT | `/api/v1/allocations/{id}` | Modify an allocation's period or percentage | Manager |
| POST | `/api/v1/allocations/{id}/cancel` | Cancel an allocation | Manager |

The Cancel transition is defined in `11-state-diagrams.md`, Section 2.

### Time Management

| Method | URL | Purpose | Required Role(s) |
|---|---|---|---|
| POST | `/api/v1/timesheets` | Create a timesheet | Employee |
| GET | `/api/v1/timesheets/{id}` | View a timesheet — also how a Manager performs Review Timesheet | Employee, Manager |
| POST | `/api/v1/timesheets/{id}/time-entries` | Add a time entry | Employee |
| PUT | `/api/v1/timesheets/{id}/time-entries/{entryId}` | Edit a time entry | Employee |
| POST | `/api/v1/timesheets/{id}/submit` | Submit a timesheet | Employee |

The Submit transition (Draft → Submitted) is defined in `11-state-diagrams.md`, Section 3.

### Approval Workflow

| Method | URL | Purpose | Required Role(s) |
|---|---|---|---|
| POST | `/api/v1/timesheets/{id}/approve` | Approve a timesheet | Manager |
| POST | `/api/v1/timesheets/{id}/reject` | Reject a timesheet (reason required) | Manager |

Approval Workflow has no `GET` endpoint of its own — reviewing a Timesheet is the same `GET /api/v1/timesheets/{id}` Time Management already exposes; Approval Workflow adds only the two decisions, matching `03-module-design.md`'s command/query split exactly (Section 4: exactly one cross-module command in the whole system, and this is it). Both the Approve and Reject transitions (Submitted → Approved, Submitted → Draft) are defined in `11-state-diagrams.md`, Section 3.

### Dashboard

| Method | URL | Purpose | Required Role(s) |
|---|---|---|---|
| GET | `/api/v1/dashboard` | Overview: active projects, utilization, pending approvals | Manager |
| GET | `/api/v1/dashboard/utilization` | Planned vs. actual hours per employee | Manager |
| GET | `/api/v1/dashboard/pending-approvals` | Submitted timesheets awaiting a decision | Manager |

---

## 5. Request / Response Examples

**Create Employee**

`POST /api/v1/employees`
```json
{
  "firstName": "Alice",
  "lastName": "Nguyen",
  "departmentId": "3f2e1a10-1111-2222-3333-444455556666",
  "managerId": null
}
```
`201 Created`
```json
{
  "employeeId": "9c4b7e20-aaaa-bbbb-cccc-ddddeeeeffff",
  "firstName": "Alice",
  "lastName": "Nguyen",
  "departmentId": "3f2e1a10-1111-2222-3333-444455556666",
  "managerId": null,
  "isActive": true
}
```

**Create Project**

`POST /api/v1/projects`
```json
{ "name": "Apollo Migration" }
```
`201 Created`
```json
{
  "projectId": "7a1d9f30-2222-3333-4444-555566667777",
  "name": "Apollo Migration",
  "status": "Active"
}
```

**Create Allocation**

`POST /api/v1/allocations`
```json
{
  "employeeId": "9c4b7e20-aaaa-bbbb-cccc-ddddeeeeffff",
  "projectId": "7a1d9f30-2222-3333-4444-555566667777",
  "periodStart": "2026-08-03",
  "periodEnd": "2026-08-28",
  "percentage": 50
}
```
`201 Created`
```json
{
  "allocationId": "5e6f8a40-3333-4444-5555-666677778888",
  "employeeId": "9c4b7e20-aaaa-bbbb-cccc-ddddeeeeffff",
  "projectId": "7a1d9f30-2222-3333-4444-555566667777",
  "periodStart": "2026-08-03",
  "periodEnd": "2026-08-28",
  "percentage": 50,
  "status": "Active"
}
```

**Submit Timesheet**

`POST /api/v1/timesheets/{id}/submit` — empty body
`200 OK`
```json
{
  "timesheetId": "2b3c4d50-4444-5555-6666-777788889999",
  "status": "Submitted",
  "lastSubmittedAtUtc": "2026-08-03T14:22:00Z"
}
```

**Approve Timesheet**

`POST /api/v1/timesheets/{id}/approve` — empty body
`200 OK`
```json
{
  "approvalId": "8f9a0b60-5555-6666-7777-8888999900aa",
  "timesheetId": "2b3c4d50-4444-5555-6666-777788889999",
  "outcome": "Approved",
  "decidedAtUtc": "2026-08-03T15:00:00Z"
}
```

JSON field names are camelCase throughout — a third naming convention alongside C#'s PascalCase (`06-domain-model.md`, `10-class-diagrams.md`) and PostgreSQL's snake_case (`08-database-design.md`, Section 2). All three are correct in their own layer; none of them needs to match the others.

---

## 6. Error Handling

Every non-2xx response uses the same envelope:

```json
{
  "error": {
    "code": "string",
    "message": "human-readable summary",
    "details": []
  }
}
```

**Validation error** — `400`
```json
{
  "error": {
    "code": "validation_error",
    "message": "One or more fields are invalid.",
    "details": [
      { "field": "name", "issue": "must not be empty" }
    ]
  }
}
```

**Unauthorized** — `401`
```json
{
  "error": {
    "code": "unauthorized",
    "message": "A valid access token is required."
  }
}
```

**Forbidden** — `403`
```json
{
  "error": {
    "code": "forbidden",
    "message": "This action requires the Manager role."
  }
}
```

**Not Found** — `404`
```json
{
  "error": {
    "code": "not_found",
    "message": "No allocation was found with the given id."
  }
}
```

**Business rule violation** — `409`
```json
{
  "error": {
    "code": "capacity_exceeded",
    "message": "This allocation would exceed the employee's available capacity."
  }
}
```

---

## 7. API Versioning

Every endpoint is prefixed `/api/v1/...`. URL-path versioning was chosen over header- or content-negotiation-based versioning for one reason: it's visible. Anyone reading a request, a log line, or this document can see which version they're looking at without inspecting headers — the simplest scheme that still leaves room for a `/api/v2/...` later, without needing one for v1 itself to exist.

---

## 8. Design Decisions

**Why REST.** Chrona's operations map naturally onto resources and a small set of actions on them (`04-use-cases.md`'s catalog is, almost entirely, CRUD plus a handful of named transitions) — exactly what REST is suited for. Nothing in this API needs GraphQL's flexible querying or gRPC's binary performance; both would add a technology this project has no evidence it needs (`ADR` technology-adoption reasoning, applied here as much as anywhere else).

**Why JSON.** The one data format every part of this stack already speaks natively — the React Frontend, ASP.NET Core, and the JWTs themselves (`02-system-context.md`) are all JSON-based already.

**Why a stateless API.** No server-side session; every request carries everything needed to authorize and process it, in the Bearer token and the request body. This is what lets `ADR-001`'s Modular Monolith run as one process without needing session affinity or shared session storage if it's ever scaled horizontally.

**Why resource-oriented design.** It's the API-level mirror of `06-domain-model.md`'s own aggregates and entities — an `/allocations` resource exists because `Allocation` is a real aggregate root, not because a database table happened to exist. Where the domain model has a rich concept, the API has a resource; where it doesn't, the API doesn't invent one.

**Why JWT authentication.** Already decided in `ADR-003` and `02-system-context.md` — this document doesn't re-justify Keycloak, only confirms that everything the API does with the token it receives (validate, extract roles, authorize) follows directly from that earlier decision.

14-api-design.md complete.