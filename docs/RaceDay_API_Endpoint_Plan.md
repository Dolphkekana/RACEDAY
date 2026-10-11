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

# 3. Authentication Endpoints

## 3.1 Register User

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/auth/register` | Creates a public Participant account. Organiser privileges cannot be requested through public registration. | None | `{ "firstName": "Dolph", "lastName": "Kekana", "email": "Dolph@example.com", "password": "SecurePassword123!", "phoneNumber": "0712345678", "dateOfBirth": "2002-05-14", "role": "Participant" }` | `201 Created` with the created user's public profile. `400 Bad Request` for invalid data or an unsupported role. `409 Conflict` if the email is already registered. |

### Registration Rules

- Email addresses must be unique.
- The password must be validated and securely hashed before storage.
- The password must never be returned in the response.
- Public registration must use the `Participant` role.
- An authenticated Organiser creates additional Organiser accounts through `POST /api/auth/provision-organiser`.

## 3.4 Provision Organiser

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/auth/provision-organiser` | Creates an Organiser account from an existing authenticated Organiser account. | Organiser | `{ "firstName": "Kabelo", "lastName": "Dlamini", "email": "kabelo@example.com", "password": "SecurePassword123!", "phoneNumber": "0723456789", "dateOfBirth": "1987-11-02" }` | `201 Created` with the public profile. `401 Unauthorized` if not logged in. `403 Forbidden` for Participants. `409 Conflict` if the email is already registered. |

---

## 3.2 Login

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/auth/login` | Authenticates a user and creates an authenticated server-side session containing the user's ID and role. | None | `{ "email": "Dolph@example.com", "password": "SecurePassword123!" }` | `200 OK` with the authenticated user's ID, name, and role. `400 Bad Request` for missing fields. `401 Unauthorized` for invalid credentials. |

---

## 3.3 Logout

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/auth/logout` | Ends the current authenticated session. | Any | None | `204 No Content`. `401 Unauthorized` if no valid authenticated session exists. |

---

# 4. User Profile Endpoints

## 4.1 Get Current User Profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/users/me` | Returns the profile information belonging to the currently authenticated user. | Any | None | `200 OK` with user profile data excluding the password hash. `401 Unauthorized` if not logged in. |

---

## 4.2 Update Current User Profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| PUT | `/api/users/me` | Updates editable personal information for the currently authenticated user. | Any | `{ "firstName": "Dolph", "lastName": "Kekana", "phoneNumber": "0712345678", "dateOfBirth": "2002-05-14" }` | `200 OK` with the updated profile. `400 Bad Request` for invalid data. `401 Unauthorized` if not logged in. |

---

# 5. Event Endpoints

## 5.1 Get Upcoming Events

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/events` | Returns upcoming RaceDay events and supports optional filtering by event type, location, date, and distance. | None | None | `200 OK` with a list of upcoming events. |

### Optional Query Parameters

```text
/api/events?eventTypeId=1
/api/events?location=Johannesburg
/api/events?eventDate=2026-09-20
/api/events?minDistance=10&maxDistance=42.2
```

---

## 5.2 Get Event by ID

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/events/{eventId}` | Returns full information for a specific event. | None | None | `200 OK` with event details and available categories. `404 Not Found` if the event does not exist. |

---

## 5.3 Create Event

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/events` | Creates a new event and associates it with the authenticated Organiser. | Organiser | `{ "eventTypeId": 1, "name": "Soweto Spring Run", "description": "A community road running event.", "eventDate": "2026-09-20", "location": "Soweto, Johannesburg", "distanceKm": 21.00 }` | `201 Created` with the new event. `400 Bad Request` for invalid data. `401 Unauthorized` if not logged in. `403 Forbidden` if the user is not an Organiser. `404 Not Found` if the selected event type does not exist. |

---

## 5.4 Update Event

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| PUT | `/api/events/{eventId}` | Updates an event owned by the authenticated Organiser. | Organiser | `{ "eventTypeId": 1, "name": "Soweto Spring Run", "description": "Updated event description.", "eventDate": "2026-09-27", "location": "Soweto, Johannesburg", "distanceKm": 21.00 }` | `200 OK` with the updated event. `400 Bad Request` for invalid data. `401 Unauthorized` if not logged in. `403 Forbidden` if the event belongs to another Organiser or the user is not an Organiser. `404 Not Found` if the event does not exist. |

---

## 5.5 Delete Event

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| DELETE | `/api/events/{eventId}` | Deletes an event owned by the authenticated Organiser when it is permitted by the system's enrolment rules. | Organiser | None | `204 No Content` when deleted. `401 Unauthorized` if not logged in. `403 Forbidden` if the event belongs to another Organiser. `404 Not Found` if the event does not exist. `409 Conflict` if the event cannot be deleted because existing enrolments prevent deletion. |

---

# 6. Event Type Endpoints

## 6.1 Get Event Types

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/event-types` | Returns the available event types used when creating and filtering events. | None | None | `200 OK` with event types such as Run, Walk, and Cycle. |

---

## 6.2 Get Event Type by ID

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/event-types/{eventTypeId}` | Returns a specific event type. | None | None | `200 OK` with the event type. `404 Not Found` if it does not exist. |

---

# 7. Category Endpoints

## 7.1 Get Categories for an Event

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/events/{eventId}/categories` | Returns all age and distance categories available for a specific event. | None | None | `200 OK` with a list of categories. `404 Not Found` if the event does not exist. |

---

## 7.2 Get Category by ID

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/categories/{categoryId}` | Returns details for a specific event category. | None | None | `200 OK` with category details. `404 Not Found` if the category does not exist. |

---

## 7.3 Create Category

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/events/{eventId}/categories` | Creates an age or distance category for an event owned by the authenticated Organiser. | Organiser | `{ "name": "Senior 21 km", "minimumAge": 18, "maximumAge": null, "distanceKm": 21.00 }` | `201 Created` with the new category. `400 Bad Request` for invalid data. `401 Unauthorized` if not logged in. `403 Forbidden` if the event is not owned by the Organiser. `404 Not Found` if the event does not exist. `409 Conflict` if an equivalent category already exists for the event. |

---

## 7.4 Update Category

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| PUT | `/api/categories/{categoryId}` | Updates an event category belonging to an event owned by the authenticated Organiser. | Organiser | `{ "name": "Senior 21 km", "minimumAge": 18, "maximumAge": null, "distanceKm": 21.00 }` | `200 OK` with the updated category. `400 Bad Request` for invalid data. `401 Unauthorized` if not logged in. `403 Forbidden` if the user is not the event's Organiser. `404 Not Found` if the category does not exist. |

---

## 7.5 Delete Category

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| DELETE | `/api/categories/{categoryId}` | Removes a category from an event when it is not being used by an enrolment. | Organiser | None | `204 No Content` when deleted. `401 Unauthorized` if not logged in. `403 Forbidden` if the user is not the event's Organiser. `404 Not Found` if the category does not exist. `409 Conflict` if existing enrolments use the category. |

---

# 8. Event Enrolment Endpoints

## 8.1 Create Event Enrolment

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/events/{eventId}/enrolments` | Enrols the authenticated Participant in an event using a selected category. | Participant | `{ "categoryId": 3 }` | `201 Created` with the enrolment. `400 Bad Request` for invalid data. `401 Unauthorized` if not logged in. `403 Forbidden` if the user is not a Participant. `404 Not Found` if the event or category does not exist. `409 Conflict` if the Participant is already enrolled in the event or the category does not belong to the event. |

---

## 8.2 Get My Enrolments

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/enrolments/my` | Returns all events the authenticated Participant has entered, including selected category and enrolment status. | Participant | None | `200 OK` with the Participant's enrolments. `401 Unauthorized` if not logged in. `403 Forbidden` if the user is not a Participant. |

---

## 8.3 Get Enrolment by ID

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/enrolments/{enrolmentId}` | Returns an enrolment when the authenticated user is permitted to view it. | Any | None | `200 OK` with enrolment details. `401 Unauthorized` if not logged in. `403 Forbidden` if the user does not own the enrolment or manage the related event. `404 Not Found` if the enrolment does not exist. |

---

## 8.4 Get Event Enrolments

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/events/{eventId}/enrolments` | Returns all Participants enrolled in an event managed by the authenticated Organiser. | Organiser | None | `200 OK` with participant enrolments, selected categories, and statuses. `401 Unauthorized` if not logged in. `403 Forbidden` if the user is not the event's Organiser. `404 Not Found` if the event does not exist. |

---

## 8.5 Update Enrolment Status

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| PUT | `/api/enrolments/{enrolmentId}/status` | Updates the status of an enrolment for an event managed by the authenticated Organiser. | Organiser | `{ "status": "Confirmed" }` | `200 OK` with the updated enrolment status. `400 Bad Request` for an unsupported status. `401 Unauthorized` if not logged in. `403 Forbidden` if the user is not the event's Organiser. `404 Not Found` if the enrolment does not exist. |

Supported statuses:

- `Pending`
- `Confirmed`
- `Cancelled`

---

# 9. Results Endpoints

## 9.1 Record Participant Result

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/enrolments/{enrolmentId}/result` | Records the finish time and finishing position for a Participant after an event has been completed. | Organiser | `{ "finishTime": "01:42:35", "finishingPosition": 47 }` | `201 Created` with the recorded result. `400 Bad Request` for invalid time or position. `401 Unauthorized` if not logged in. `403 Forbidden` if the user is not the event's Organiser. `404 Not Found` if the enrolment does not exist. `409 Conflict` if a result already exists for the enrolment. |

---

## 9.2 Update Participant Result

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| PUT | `/api/results/{resultId}` | Corrects or updates a recorded result for an event managed by the authenticated Organiser. | Organiser | `{ "finishTime": "01:41:58", "finishingPosition": 46 }` | `200 OK` with the updated result. `400 Bad Request` for invalid data. `401 Unauthorized` if not logged in. `403 Forbidden` if the user is not the event's Organiser. `404 Not Found` if the result does not exist. |

---

## 9.3 Get My Results

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/results/my` | Returns the authenticated Participant's personal race history, including event, date, category, finish time, and finishing position. | Participant | None | `200 OK` with the Participant's results. `401 Unauthorized` if not logged in. `403 Forbidden` if the user is not a Participant. |

---

# 10. Endpoint Summary

The planned API contains the following endpoint groups:

| Resource | Planned Operations |
|---|---|
| Authentication | Register, Login, Logout |
| User Profile | Get Profile, Update Profile |
| Events | List, Get, Create, Update, Delete |
| Event Types | List, Get |
| Categories | List by Event, Get, Create, Update, Delete |
| Enrolments | Create, Get Mine, Get by ID, Get Event Enrolments, Update Status |
| Results | Create, Update, Get Mine |

**Total planned endpoints: 26**

---

# 11. Role Access Summary

| Operation | Public | Any Authenticated User | Organiser | Participant |
|---|---:|---:|---:|---:|
| Register | Yes | No | No | No |
| Login | Yes | No | No | No |
| Logout | No | Yes | Yes | Yes |
| View Events | Yes | Yes | Yes | Yes |
| View Event Details | Yes | Yes | Yes | Yes |
| View Event Types | Yes | Yes | Yes | Yes |
| View Categories | Yes | Yes | Yes | Yes |
| View Own Profile | No | Yes | Yes | Yes |
| Update Own Profile | No | Yes | Yes | Yes |
| Create Event | No | No | Yes | No |
| Update Own Event | No | No | Yes | No |
| Delete Own Event | No | No | Yes | No |
| Create Category | No | No | Yes | No |
| Update Category | No | No | Yes | No |
| Delete Category | No | No | Yes | No |
| Enrol in Event | No | No | No | Yes |
| View Own Enrolments | No | No | No | Yes |
| View Event Enrolments | No | No | Yes | No |
| Update Enrolment Status | No | No | Yes | No |
| Record Result | No | No | Yes | No |
| Update Result | No | No | Yes | No |
| View Own Results | No | No | No | Yes |

---

# 12. Relationship to the Part 1 ERD

The API is designed around the RaceDay database entities:

- `Users`
- `Events`
- `EventTypes`
- `Categories`
- `Enrolments`
- `Results`

The planned routes preserve the relationships defined in the ERD:

- A User acting as an Organiser can manage Events.
- Each Event belongs to an EventType.
- Each Event can have multiple Categories.
- A Participant enters an Event through an Enrolment.
- An Enrolment records the selected Category.
- An Enrolment may have zero or one Result.
- A Result belongs to one Enrolment.

The API should not introduce a separate database model that conflicts with the Part 1 ERD.

---

# 13. Part 2 Implementation Rule

This endpoint plan is the API specification for Part 2.

When implementation begins:

1. Routes should follow this plan.
2. HTTP methods should follow this plan.
3. Role restrictions should follow this plan.
4. Request models should represent the documented request bodies.
5. Response status codes should follow the documented outcomes.
6. Authentication must use server-side session management.
7. Protected endpoints must enforce authorization at the API level.
8. The API must use Entity Framework Core to access the SQL Server database.
9. Swagger must expose and describe the implemented endpoints.
10. Any necessary deviation from this plan must be documented and justified.

The goal is for the Part 2 implementation to closely match this approved Part 1 API specification.

