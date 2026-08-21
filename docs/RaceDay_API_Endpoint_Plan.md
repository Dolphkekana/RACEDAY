# RaceDay API Endpoint Plan

## 1. Document Purpose

This document defines the REST API contract for the **RaceDay** event management system.

The endpoint plan is based on the Part 1 database design and will be used as the specification for the ASP.NET Core Web API developed in Part 2. The implemented API should follow these routes, responsibilities, request structures, and response expectations unless a documented technical change is required.

RaceDay supports two roles:

- **Organiser** — creates and manages events, manages categories, views enrolments, and records participant results.
- **Participant** — creates an account, browses events, views categories, enters events, views personal enrolments, and tracks personal results.

The API will use session-based authentication. Protected endpoints require an authenticated session, and role-specific endpoints must enforce the required role at the API level.

---