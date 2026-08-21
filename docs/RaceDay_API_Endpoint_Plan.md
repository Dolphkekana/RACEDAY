# RaceDay API Endpoint Plan

## 1. Document Purpose

This document defines the REST API contract for the **RaceDay** event management system.

The endpoint plan is based on the Part 1 database design and will be used as the specification for the ASP.NET Core Web API developed in Part 2. The implemented API should follow these routes, responsibilities, request structures, and response expectations unless a documented technical change is required.

RaceDay supports two roles:

- **Organiser** — creates and manages events, manages categories, views enrolments, and records participant results.
- **Participant** — creates an account, browses events, views categories, enters events, views personal enrolments, and tracks personal results.

The API will use session-based authentication. Protected endpoints require an authenticated session, and role-specific endpoints must enforce the required role at the API level.

---
## 2. API Conventions

### Base Route

All API endpoints use the following base route:

`/api/`

### Authentication

Authentication is session-based.

- Public endpoints do not require an authenticated session.
- Protected endpoints require a valid authenticated session.
- The server-side session stores the authenticated user's ID and role.
- Passwords are never returned by the API and are stored as secure password hashes.
- A Participant must not be able to access Organiser-only endpoints.
- An Organiser must not be able to use Participant-only operations where the operation is restricted to Participants.

### Common Response Codes

| Status Code | Meaning |
|---|---|
| `200 OK` | Request completed successfully. |
| `201 Created` | A new resource was successfully created. |
| `204 No Content` | Request completed successfully with no response body. |
| `400 Bad Request` | Request contains invalid or incomplete data. |
| `401 Unauthorized` | Authentication is required or the session is invalid. |
| `403 Forbidden` | The authenticated user does not have permission to perform the operation. |
| `404 Not Found` | The requested resource does not exist. |
| `409 Conflict` | The request conflicts with existing data or a business rule. |

---