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