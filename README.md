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
/docs/RaceDay_API_Endpoint_Plan.md
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

The repository structure is:

```
RACEDAY/
│
├── docs/
│   ├── RaceDayERD.png
│   ├── RaceDay_API_Endpoint_Plan.md
│   ├── RaceDay-Database.sql
│   └── ci-run.png
│
├── src/
│   └── RaceDay.Api/
│       ├── Auth/
│       ├── Contracts/
│       ├── Controllers/
│       ├── Data/
│       │   └── Migrations/
│       ├── Documentation/
│       ├── Models/
│       ├── Services/
│       └── Program.cs
│
├── tests/
│   └── RaceDay.Api.Tests/
│
├── .github/
│   └── workflows/
│       ├── part1-validation.yml
│       └── part2-ci.yml
│
├── README.md
└── RaceDay.sln
```

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
git clone <https://github.com/Dolphkekana/RACEDAY>
cd RACEDAY
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
### 4. Restore Dependencies

From the project directory:

```bash
dotnet restore
```



### 5. Build the Project

```bash
dotnet build
```



### 6. Run the API

```bash
dotnet run
```

The terminal will display the local address used by the application.

### 7. Open Swagger

When the API is running, open the Swagger URL shown by the application.

Swagger will provide an interactive view of the API endpoints and will be used to test the API during development.

## Testing

The project will use automated tests to verify important API behaviour.

Testing will cover:

- User registration.
- Login and logout.
- Invalid authentication.
- Role-based access control.
- Event creation.
- Event updates.
- Event deletion.
- Category management.
- Participant enrolment.
- Duplicate enrolment prevention.
- Enrolment status updates.
- Result creation.
- Result updates.
- Result retrieval.
- Invalid IDs.
- Missing resources.
- Validation failures.

Run the automated tests with:

```bash
dotnet test
```
## HTTP Response Standards

The API uses standard HTTP response codes.


| Code             | Usage                                                    |
| ---------------- | -------------------------------------------------------- |
| 200 OK           | Successful request returning data.                       |
| 201 Created      | Resource successfully created.                           |
| 204 No Content   | Successful request with no response body.                |
| 400 Bad Request  | Invalid request data.                                    |
| 401 Unauthorized | Authentication is required or invalid.                   |
| 403 Forbidden    | User is authenticated but lacks permission.              |
| 404 Not Found    | Requested resource does not exist.                       |
| 409 Conflict     | Request conflicts with existing data or a business rule. |




## Validation and Business Rules

The API validates incoming data before writing it to the database.

Important rules include:

- Email addresses must be unique.
- User roles must be valid.
- Event distances must be greater than zero.
- Category ages must be valid.
- Category age ranges must be logical.
- An enrolment must reference an existing event.
- An enrolment must reference an existing category.
- A selected category must belong to the event being entered.
- A Participant cannot enrol in the same event more than once.
- An enrolment can have at most one result.
- Finishing positions must be greater than zero.
- Organisers may only manage events they own.
- Participants may only access their own participant-specific information.
- Invalid or unauthorised requests must return appropriate HTTP status codes.



## Development Workflow

RaceDay follows a staged development process.

```
Part 1
  |
  +-- Database Design
  |
  +-- ERD
  |
  +-- API Endpoint Plan
  |
  +-- SQL Script
  |
  +-- Repository / CI Setup
  |
  v
Part 2
  |
  +-- ASP.NET Core API
  +-- Authentication
  +-- Entity Framework Core
  +-- Controllers / Services
  +-- Validation
  +-- Swagger
  +-- Automated Tests
  +-- CI/CD
```

The Part 1 database and API documents form the specification for the Part 2 implementation.
## Documentation

Part 1 documentation is stored inside the `/docs` directory.


| File                         | Description                                                                             |
| ---------------------------- | --------------------------------------------------------------------------------------- |
| RaceDayERD.png              | Entity Relationship Diagram showing the database entities and relationships.            |
| RaceDay_API_Endpoint_Plan.md | Complete REST API endpoint specification.                                               |
| RaceDay-Database.sql         | SQL Server database creation, constraints, indexes, seed data and verification queries. |
| ci-run.png                   | Screenshot of a successful GitHub Actions CI build.                                     |

## CI Successful Build

Evidence of a successful **RaceDay Part 1 Validation** workflow run on GitHub Actions:

<img src="docs/ci-run.png" alt="CI Successful Build" width="800" />

## Sample Accounts

The database contains development accounts for testing.


| Role        | Email                                                               |
| ----------- | ------------------------------------------------------------------- |
| Organiser   | [adolf.kekana@raceday.co.za](mailto:adolf.kekana@raceday.co.za) |
| Organiser   | [kabelo.dlamini@raceday.co.za](mailto:kabelo.dlamini@raceday.co.za) |
| Participant | [thabang.sekwati@example.com'](mailto:thabang.sekwati@example.com') |
| Participant | [amanda.nkosi@example.com](mailto:amanda.nkosi@example.com)         |


Development password:

```
RaceDay@2026
```

These accounts are intended for local development and testing only.

Production credentials must be secure and must never be committed to source control.

## Part 2: RESTful Web API

Part 2 is an ASP.NET Core 8 Web API that uses Entity Framework Core Code-First with SQL Server. The schema matches the Part 1 ERD and SQL script.

[![RaceDay Part 2 CI](https://github.com/Dolphkekana/RACEDAY/actions/workflows/part2-ci.yml/badge.svg?branch=part-2-api)](https://github.com/Dolphkekana/RACEDAY/actions/workflows/part2-ci.yml)

### Where to find the Part 2 files

| Location | What it holds |
| --- | --- |
| `RaceDay.sln` | Solution file that contains the API project and the test project. |
| `src/RaceDay.Api/Program.cs` | Startup: EF Core, session, authentication, Swagger and the automatic migration step. |
| `src/RaceDay.Api/Auth/` | `SessionAuthenticationHandler` reads the user id and role from the server-side session. `UserSession` holds the session key names. |
| `src/RaceDay.Api/Contracts/` | Request and response records (DTOs) with validation attributes. Password hashes are never returned. |
| `src/RaceDay.Api/Controllers/` | One controller per resource: Auth, Users, Events, EventTypes, Categories, Enrolments, Results and Health. |
| `src/RaceDay.Api/Data/RaceDayDbContext.cs` | EF Core DbContext with keys, lengths, unique indexes, check constraints and the seeded event types. |
| `src/RaceDay.Api/Data/Migrations/` | The `InitialCreate` EF Core migration that builds the database. |
| `src/RaceDay.Api/Documentation/` | Swagger filter that marks protected endpoints with the session cookie. |
| `src/RaceDay.Api/Models/` | Entity classes: User, EventType, Event, Category, Enrolment and Result. |
| `src/RaceDay.Api/Services/` | `AuthenticationService` for registration, login, password hashing and profile updates. |
| `src/RaceDay.Api/RaceDay.Api.http` | Sample requests for testing in Visual Studio or VS Code. |
| `tests/RaceDay.Api.Tests/` | xUnit tests that run the API in memory with `WebApplicationFactory` and the EF Core InMemory provider. |
| `.github/workflows/part2-ci.yml` | GitHub Actions workflow that restores, builds and tests the solution on every push. |
| `docs/` | ERD, SQL script, endpoint plan and CI screenshot. |

### Setting up the database

You can create the database in one of two ways. Use one of them, not both, on the same database.

**Option 1: EF Core migrations (recommended)**

The connection string is in `src/RaceDay.Api/appsettings.json` and `appsettings.Development.json`. It points at SQL Server LocalDB and a database called `RaceDayApiDB`:

```
Server=(localdb)\mssqllocaldb;Database=RaceDayApiDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```

When the API runs in Development it applies any pending migrations by itself. You can also run them by hand:

```bash
dotnet tool install --global dotnet-ef --version 8.0.10
dotnet ef database update --project src/RaceDay.Api
```

The migration seeds the three event types: Run (1), Walk (2) and Cycle (3).

**Option 2: Part 1 SQL script**

Run `docs/RaceDay-Database.sql` in SSMS. It creates a database called `RaceDay` with sample data. Then change `Database=RaceDayApiDB` to `Database=RaceDay` in both appsettings files. Do not run the migrations against this database because the tables already exist.

### Running the API and Swagger

```bash
dotnet restore RaceDay.sln
dotnet build RaceDay.sln
dotnet run --project src/RaceDay.Api
```

Open the Swagger UI at `https://localhost:<port>/swagger` (the port is shown in the console and in `Properties/launchSettings.json`). Every endpoint in Swagger shows a summary, a description of the input format, and the status codes it can return.

To try the protected endpoints in Swagger, call `POST /api/auth/login` first. The browser keeps the `RaceDay.Session` cookie, so the next requests are sent as that user.

### Authentication and roles

- `POST /api/auth/register` creates an account. The user selects the role `Organiser` or `Participant`. Any other role returns `400 Bad Request`.
- Passwords must be at least 8 characters with an upper case letter, a lower case letter and a digit. They are hashed with the ASP.NET Core `PasswordHasher` before they are saved.
- `POST /api/auth/login` checks the password and stores the user id and role in the server-side session. The client only gets an HttpOnly `RaceDay.Session` cookie. The session expires after 30 minutes without activity.
- `POST /api/auth/logout` clears the session.
- Endpoints use `[Authorize]` or `[Authorize(Roles = "...")]`. No session returns `401 Unauthorized` and the wrong role returns `403 Forbidden`.
- Organisers can only change events, categories, enrolments and results for events they created. Participants only see their own enrolments and results. The participant id always comes from the session, never from the request body.

### Endpoint summary

| Method | Route | Role |
| --- | --- | --- |
| POST | `/api/auth/register` | Public |
| POST | `/api/auth/login` | Public |
| POST | `/api/auth/logout` | Any logged in user |
| POST | `/api/auth/provision-organiser` | Organiser |
| GET | `/api/users/me` | Any logged in user |
| PUT | `/api/users/me` | Any logged in user |
| GET | `/api/events` | Public |
| GET | `/api/events/{eventId}` | Public |
| GET | `/api/events/mine` | Organiser |
| POST | `/api/events` | Organiser |
| PUT | `/api/events/{eventId}` | Organiser (owner) |
| DELETE | `/api/events/{eventId}` | Organiser (owner) |
| GET | `/api/event-types` | Public |
| GET | `/api/event-types/{eventTypeId}` | Public |
| GET | `/api/events/{eventId}/categories` | Public |
| GET | `/api/categories/{categoryId}` | Public |
| POST | `/api/events/{eventId}/categories` | Organiser (owner) |
| PUT | `/api/categories/{categoryId}` | Organiser (owner) |
| DELETE | `/api/categories/{categoryId}` | Organiser (owner) |
| POST | `/api/events/{eventId}/enrolments` | Participant |
| GET | `/api/enrolments/my` | Participant |
| GET | `/api/enrolments/{enrolmentId}` | Participant (own) or Organiser (owner) |
| GET | `/api/events/{eventId}/enrolments` | Organiser (owner) |
| PUT | `/api/enrolments/{enrolmentId}/status` | Organiser (owner) |
| POST | `/api/enrolments/{enrolmentId}/result` | Organiser (owner) |
| PUT | `/api/results/{resultId}` | Organiser (owner) |
| GET | `/api/results/my` | Participant |
| GET | `/api/events/{eventId}/results` | Organiser (owner) |
| GET | `/api/health` | Public |

The full plan with request bodies and response codes is in `docs/RaceDay_API_Endpoint_Plan.md`.

### Running the tests

```bash
dotnet test RaceDay.sln
```

The tests do not need SQL Server because they use the EF Core InMemory provider. They cover:

- Registration with both roles, invalid roles, weak passwords, invalid emails and duplicate emails.
- Login, logout, wrong passwords and profile view and update.
- Event and category create, update and delete, including date and distance validation.
- Role rejection: anonymous users get 401 and the wrong role gets 403.
- Ownership: one Organiser cannot manage another Organiser's event, enrolments or results.
- Enrolments: entering by category, duplicate enrolments, missing references and status updates.
- Results: capturing, updating, viewing own results, viewing event results and invalid values.
- Swagger: every endpoint has a summary and documented responses.

### Continuous integration

`.github/workflows/part2-ci.yml` runs on every push to any branch and on pull requests. It sets up .NET 8, then restores, builds (Release) and runs all the tests in `RaceDay.sln`. Runs can be viewed on the [Actions tab](https://github.com/Dolphkekana/RACEDAY/actions/workflows/part2-ci.yml).

## Project Status



### Part 1

- [x] ERD designed.
- [x] API endpoint plan created.
- [x] SQL Server database script created.
- [x] Database relationships defined.
- [x] Primary and foreign keys defined.
- [x] Validation constraints defined.
- [x] Indexes defined.
- [x] Sample data prepared.
- [x] SQL script tested in the target SQL Server environment.
- [x] Repository and CI evidence completed.



### Part 2

- [x] ASP.NET Core Web API implementation.
- [x] Entity Framework Core integration.
- [x] Session-based authentication.
- [x] Role-based authorisation.
- [x] Event management.
- [x] Category management.
- [x] Participant enrolments.
- [x] Result management.
- [x] Swagger documentation.
- [x] Automated tests.
- [x] CI/CD pipeline.



## Repository Quality

The repository aims to maintain:

- Clear project structure.
- Meaningful naming conventions.
- Small and focused commits.
- Useful comments where logic is not immediately obvious.
- No committed secrets.
- Repeatable database setup.
- Documented API behaviour.
- Automated build and test checks.
- Consistency between the Part 1 design and Part 2 implementation.