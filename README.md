# RaceDay

RaceDay is an ASP.NET Core Web API for managing community sports events. The system allows organisers to create and manage events, categories, participant enrolments and race results, while participants can register, browse events, enrol in events and view their results.

The project is being developed as part of the RaceDay API assignment and follows a planning-first approach. The database design and API contract are defined before the main API implementation.

---

## Project Overview

RaceDay supports two main user roles.

### Organiser

An Organiser can:

- Create and manage events.
- Manage event categories.
- View participants enrolled in their events.
- Update enrolment statuses.
- Record participant results.
- Correct recorded results.



### Participant

A Participant can:

- Create an account.
- Log in and log out.
- View and update their profile.
- Browse upcoming events.
- View event details and categories.
- Enrol in events.
- View their own enrolments.
- View their personal race results.

---



## Technology Stack


| Technology            | Purpose                       |
| --------------------- | ----------------------------- |
| ASP.NET Core Web API  | REST API                      |
| C#                    | Application development       |
| Entity Framework Core | Database access and ORM       |
| SQL Server            | Relational database           |
| ASP.NET Core Session  | Session-based authentication  |
| Swagger / OpenAPI     | API documentation and testing |
| xUnit                 | Automated testing             |
| Git / GitHub          | Version control               |
| GitHub Actions        | Continuous Integration        |


---
## Database Design

<img src="docs/RaceDayERD.png" alt="RaceDay ERD" width="600" />

# RaceDay



## Core Tables


| Table      | Purpose                                                                      |
| ---------- | ---------------------------------------------------------------------------- |
| Users      | Stores Organiser and Participant accounts.                                   |
| EventTypes | Stores the types of events supported by RaceDay.                             |
| Events     | Stores event information and links events to Organisers and EventTypes.      |
| Categories | Stores the age and distance categories available for each event.             |
| Enrolments | Links Participants to Events and records their selected category and status. |
| Results    | Stores finishing information for an enrolment.                               |


The database uses:

- Primary keys.
- Foreign keys.
- Unique constraints.
- Check constraints.
- NOT NULL constraints.
- Indexes on frequently queried fields and relationships.
- Referential integrity between related entities.

The complete database script is located at:

```
/docs/RaceDay-Database.sql
```
## API Design

RaceDay follows a REST-style API structure.

### Authentication

```
POST /api/auth/register
POST /api/auth/login
POST /api/auth/logout
```



### User Profile

```
GET  /api/users/me
PUT  /api/users/me
```



### Events

```
GET    /api/events
GET    /api/events/{eventId}
POST   /api/events
PUT    /api/events/{eventId}
DELETE /api/events/{eventId}
```



### Event Types

```
GET /api/event-types
GET /api/event-types/{eventTypeId}
```



### Categories

```
GET    /api/events/{eventId}/categories
GET    /api/categories/{categoryId}
POST   /api/events/{eventId}/categories
PUT    /api/categories/{categoryId}
DELETE /api/categories/{categoryId}
```



### Enrolments

```
POST /api/events/{eventId}/enrolments
GET  /api/enrolments/my
GET  /api/enrolments/{enrolmentId}
GET  /api/events/{eventId}/enrolments
PUT  /api/enrolments/{enrolmentId}/status
```



### Results

```
POST /api/enrolments/{enrolmentId}/result
PUT  /api/results/{resultId}
GET  /api/results/my
```

The complete API specification is available in:

```
/docs/RaceDay-API-Endpoint-Plan.md
```

The endpoint plan contains the HTTP methods, routes, descriptions, role requirements, request bodies and expected response codes.
## Authentication and Authorisation

RaceDay uses session-based authentication.

The API must:

- Authenticate users using their email and password.
- Store the authenticated user's identity and role in the server-side session.
- Protect endpoints that require authentication.
- Enforce Organiser-only operations.
- Enforce Participant-only operations.
- Prevent users from accessing resources they do not have permission to manage.
- Never return password hashes through API responses.



### Access Model


| Feature               | Public | Organiser | Participant |
| --------------------- | ------ | --------- | ----------- |
| Register              | Yes    | Yes       | Yes         |
| Login                 | Yes    | Yes       | Yes         |
| Browse events         | Yes    | Yes       | Yes         |
| Create event          | No     | Yes       | No          |
| Manage own events     | No     | Yes       | No          |
| Enrol in event        | No     | No        | Yes         |
| View own enrolments   | No     | No        | Yes         |
| View event enrolments | No     | Yes       | No          |
| Record results        | No     | Yes       | No          |
| View own results      | No     | No        | Yes         |




## Project Structure

The intended repository structure is:

```
RaceDay/
│
├── docs/
│   ├── RaceDay-ERD.png
│   ├── RaceDay-API-Endpoint-Plan.md
│   └── RaceDay-Database.sql
│
├── src/
│   └── RaceDay.Api/
│       ├── Controllers/
│       ├── Data/
│       ├── Models/
│       ├── DTOs/
│       ├── Services/
│       ├── Middleware/
│       └── Program.cs
│
├── tests/
│   └── RaceDay.Api.Tests/
│
├── .github/
│   └── workflows/
│
├── README.md
└── RaceDay.sln
```

The final implementation structure may be adjusted during Part 2 if required by the ASP.NET Core project setup.

## Getting Started



### Prerequisites

Install the following:

- .NET SDK compatible with the project target framework.
- SQL Server or SQL Server Express.
- SQL Server Management Studio (SSMS).
- Git.
- Visual Studio or Visual Studio Code.



### 1. Clone the Repository

```bash
git clone <repository-url>
cd RaceDay
```



### 2. Create the Database

Open:

```
/docs/RaceDay-Database.sql
```

Run the script in SQL Server Management Studio.

The script creates:

- The RaceDay database.
- All six database tables.
- Primary and foreign keys.
- Validation constraints.
- Unique constraints.
- Indexes.
- Sample data.
- Verification queries.



### 3. Configure the Connection String

Update the application's configuration with the SQL Server connection string.

Example:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=RaceDay;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

The actual connection string should match the SQL Server installation being used.