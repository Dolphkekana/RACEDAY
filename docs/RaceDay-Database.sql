/*
RaceDay Database
Sample user password for development:
RaceDay@2026
*/

USE master;
GO

IF DB_ID(N'RaceDay') IS NULL
    CREATE DATABASE RaceDay;
GO

USE RaceDay;
GO

/* Remove existing tables so the script can be tested repeatedly. */
DROP TABLE IF EXISTS dbo.Results;
DROP TABLE IF EXISTS dbo.Enrolments;
DROP TABLE IF EXISTS dbo.Categories;
DROP TABLE IF EXISTS dbo.Events;
DROP TABLE IF EXISTS dbo.EventTypes;
DROP TABLE IF EXISTS dbo.Users;
GO

--Users table
CREATE TABLE dbo.Users
(
    UserId INT IDENTITY(1,1) NOT NULL,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255) NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    PhoneNumber NVARCHAR(20) NOT NULL,
    DateOfBirth DATE NOT NULL,
    Role NVARCHAR(20) NOT NULL,
    CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_Users_CreatedAt DEFAULT SYSDATETIME(),

    CONSTRAINT PK_Users PRIMARY KEY (UserId),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT CK_Users_Role
        CHECK (Role IN (N'Organiser', N'Participant'))
);
GO

--EventTypes table
CREATE TABLE dbo.EventTypes
(
    EventTypeId INT IDENTITY(1,1) NOT NULL,
    Name NVARCHAR(50) NOT NULL,

    CONSTRAINT PK_EventTypes PRIMARY KEY (EventTypeId),
    CONSTRAINT UQ_EventTypes_Name UNIQUE (Name)
);
GO

--Events table
CREATE TABLE dbo.Events
(
    EventId INT IDENTITY(1,1) NOT NULL,
    OrganiserId INT NOT NULL,
    EventTypeId INT NOT NULL,
    Name NVARCHAR(150) NOT NULL,
    Description NVARCHAR(1000) NOT NULL,
    EventDate DATE NOT NULL,
    Location NVARCHAR(200) NOT NULL,
    DistanceKm DECIMAL(6,2) NOT NULL,
    BannerImageUrl NVARCHAR(500) NULL,
    CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_Events_CreatedAt DEFAULT SYSDATETIME(),
    UpdatedAt DATETIME2(0) NULL,

    CONSTRAINT PK_Events PRIMARY KEY (EventId),

    CONSTRAINT FK_Events_Users_Organiser
        FOREIGN KEY (OrganiserId)
        REFERENCES dbo.Users(UserId),

    CONSTRAINT FK_Events_EventTypes
        FOREIGN KEY (EventTypeId)
        REFERENCES dbo.EventTypes(EventTypeId),

    CONSTRAINT CK_Events_DistanceKm
        CHECK (DistanceKm > 0),

    CONSTRAINT CK_Events_Date
        CHECK (EventDate >= CONVERT(DATE, CreatedAt))
);
GO

--Categories table
CREATE TABLE dbo.Categories
(
    CategoryId INT IDENTITY(1,1) NOT NULL,
    EventId INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    MinimumAge INT NULL,
    MaximumAge INT NULL,
    DistanceKm DECIMAL(6,2) NULL,

    CONSTRAINT PK_Categories PRIMARY KEY (CategoryId),

    CONSTRAINT FK_Categories_Events
        FOREIGN KEY (EventId)
        REFERENCES dbo.Events(EventId),

    CONSTRAINT CK_Categories_MinimumAge
        CHECK (MinimumAge IS NULL OR MinimumAge >= 0),

    CONSTRAINT CK_Categories_MaximumAge
        CHECK (MaximumAge IS NULL OR MaximumAge >= 0),

    CONSTRAINT CK_Categories_AgeRange
        CHECK (
            MinimumAge IS NULL
            OR MaximumAge IS NULL
            OR MinimumAge <= MaximumAge
        ),

    CONSTRAINT CK_Categories_DistanceKm
        CHECK (DistanceKm IS NULL OR DistanceKm > 0),

    CONSTRAINT UQ_Categories_Event_Name
        UNIQUE (EventId, Name)
);
GO

--Enrolments table
CREATE TABLE dbo.Enrolments
(
    EnrolmentId INT IDENTITY(1,1) NOT NULL,
    ParticipantId INT NOT NULL,
    EventId INT NOT NULL,
    CategoryId INT NOT NULL,
    EnrolmentDate DATETIME2(0) NOT NULL
        CONSTRAINT DF_Enrolments_EnrolmentDate DEFAULT SYSDATETIME(),
    Status NVARCHAR(20) NOT NULL,

    CONSTRAINT PK_Enrolments PRIMARY KEY (EnrolmentId),

    CONSTRAINT FK_Enrolments_Users_Participant
        FOREIGN KEY (ParticipantId)
        REFERENCES dbo.Users(UserId),

    CONSTRAINT FK_Enrolments_Events
        FOREIGN KEY (EventId)
        REFERENCES dbo.Events(EventId),

    CONSTRAINT FK_Enrolments_Categories
        FOREIGN KEY (CategoryId)
        REFERENCES dbo.Categories(CategoryId),

    CONSTRAINT CK_Enrolments_Status
        CHECK (Status IN (N'Pending', N'Confirmed', N'Cancelled')),

    CONSTRAINT UQ_Enrolments_Participant_Event
        UNIQUE (ParticipantId, EventId)
);
GO

--Results table
CREATE TABLE dbo.Results
(
    ResultId INT IDENTITY(1,1) NOT NULL,
    EnrolmentId INT NOT NULL,
    FinishTime TIME(0) NOT NULL,
    FinishingPosition INT NOT NULL,
    RecordedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_Results_RecordedAt DEFAULT SYSDATETIME(),

    CONSTRAINT PK_Results PRIMARY KEY (ResultId),

    CONSTRAINT FK_Results_Enrolments
        FOREIGN KEY (EnrolmentId)
        REFERENCES dbo.Enrolments(EnrolmentId),

    CONSTRAINT CK_Results_FinishingPosition
        CHECK (FinishingPosition > 0),

    CONSTRAINT UQ_Results_Enrolment
        UNIQUE (EnrolmentId)
);
GO
--Indexes
CREATE INDEX IX_Events_OrganiserId
    ON dbo.Events(OrganiserId);

CREATE INDEX IX_Events_EventTypeId
    ON dbo.Events(EventTypeId);

CREATE INDEX IX_Events_EventDate
    ON dbo.Events(EventDate);

CREATE INDEX IX_Categories_EventId
    ON dbo.Categories(EventId);

CREATE INDEX IX_Enrolments_EventId
    ON dbo.Enrolments(EventId);

CREATE INDEX IX_Enrolments_ParticipantId
    ON dbo.Enrolments(ParticipantId);

CREATE INDEX IX_Enrolments_CategoryId
    ON dbo.Enrolments(CategoryId);

CREATE INDEX IX_Results_FinishingPosition
    ON dbo.Results(FinishingPosition);
GO
--Seed Event Types
SET IDENTITY_INSERT dbo.EventTypes ON;
INSERT INTO dbo.EventTypes (EventTypeId, Name)
VALUES
(1, N'Run'),
(2, N'Walk'),
(3, N'Cycle');

SET IDENTITY_INSERT dbo.EventTypes OFF;
GO

--Seed Users
SET IDENTITY_INSERT dbo.Users ON;

INSERT INTO dbo.Users
(
    UserId, FirstName, LastName, Email, PasswordHash,
    PhoneNumber, DateOfBirth, Role, CreatedAt
)
VALUES
(1, N'Adolf', N'Kekana',
 N'adolf.kekana@raceday.co.za',
 N'AQAAAAEAAYagAAAAEBs8HkANVrgFnBt/Y6NI81mlszreepfA5CfSsBhKTxvX5dacqbPXP4OnrCWPl/8zZw==',
 N'0712345678', '1990-03-18', N'Organiser',
 '2026-08-01T08:30:00'),

(2, N'Kabelo', N'Dlamini',
 N'kabelo.dlamini@raceday.co.za',
 N'AQAAAAEAAYagAAAAEBs8HkANVrgFnBt/Y6NI81mlszreepfA5CfSsBhKTxvX5dacqbPXP4OnrCWPl/8zZw==',
 N'0723456789', '1987-11-02', N'Organiser',
 '2026-08-01T09:00:00'),

(3, N'Thabang', N'Sekwati',
 N'thabang.sekwati@example.com',
 N'AQAAAAEAAYagAAAAEDN+K03dwM5fDpvJKPKlQF5O4v/GlqlHhqA5isHtcUHFF/BBNq8sA1rvGNba2gMoSw==',
 N'0734567890', '2002-05-14', N'Participant',
 '2026-08-02T10:15:00'),

(4, N'Amanda', N'Nkosi',
 N'amanda.nkosi@example.com',
 N'AQAAAAEAAYagAAAAEDN+K03dwM5fDpvJKPKlQF5O4v/GlqlHhqA5isHtcUHFF/BBNq8sA1rvGNba2gMoSw==',
 N'0745678901', '1998-08-21', N'Participant',
 '2026-08-02T11:00:00');

SET IDENTITY_INSERT dbo.Users OFF;
GO

--Seed Events
SET IDENTITY_INSERT dbo.Events ON;

INSERT INTO dbo.Events
(
    EventId, OrganiserId, EventTypeId, Name, Description,
    EventDate, Location, DistanceKm, BannerImageUrl,
    CreatedAt, UpdatedAt
)
VALUES
(1, 1, 1, N'Soweto Spring Run',
 N'A community road running event supporting local fitness and healthy living.',
 '2026-09-20', N'Soweto, Johannesburg', 21.00,
 N'https://example.com/images/soweto-spring-run.jpg',
 '2026-08-03T08:00:00', NULL),

(2, 2, 2, N'Johannesburg Family Walk',
 N'A family-friendly walking event suitable for participants of different ages.',
 '2026-10-04', N'Johannesburg, Gauteng', 10.00,
 N'https://example.com/images/johannesburg-family-walk.jpg',
 '2026-08-04T09:00:00', NULL),

(3, 1, 3, N'Cradle Cycle Challenge',
 N'A road cycling event through the scenic Cradle of Humankind area.',
 '2026-10-18', N'Cradle of Humankind, Gauteng', 42.00,
 N'https://example.com/images/cradle-cycle-challenge.jpg',
 '2026-08-05T07:30:00', NULL);

SET IDENTITY_INSERT dbo.Events OFF;
GO

--Seeding Categories for each event
SET IDENTITY_INSERT dbo.Categories ON;

INSERT INTO dbo.Categories
(
    CategoryId, EventId, Name,
    MinimumAge, MaximumAge, DistanceKm
)
VALUES
(1, 1, N'Junior 10 km', 14, 17, 10.00),
(2, 1, N'Senior 21 km', 18, NULL, 21.00),
(3, 2, N'Family 5 km', NULL, NULL, 5.00),
(4, 2, N'Senior 10 km', 18, NULL, 10.00),
(5, 3, N'Open 42 km', 18, NULL, 42.00),
(6, 3, N'Junior 20 km', 16, 17, 20.00);

SET IDENTITY_INSERT dbo.Categories OFF;
GO

--Seeding Enrolments for each event
SET IDENTITY_INSERT dbo.Enrolments ON;

INSERT INTO dbo.Enrolments
(
    EnrolmentId, ParticipantId, EventId,
    CategoryId, EnrolmentDate, Status
)
VALUES
(1, 3, 1, 2, '2026-08-06T10:15:00', N'Confirmed'),
(2, 4, 1, 2, '2026-08-06T11:20:00', N'Confirmed'),
(3, 3, 2, 4, '2026-08-07T09:45:00', N'Confirmed'),
(4, 4, 3, 5, '2026-08-07T14:10:00', N'Pending');

SET IDENTITY_INSERT dbo.Enrolments OFF;
GO
--Seeding Results for each enrolment
SET IDENTITY_INSERT dbo.Results ON;

INSERT INTO dbo.Results
(
    ResultId, EnrolmentId,
    FinishTime, FinishingPosition, RecordedAt
)
VALUES
(1, 1, '01:42:35', 47, '2026-09-20T12:15:00'),
(2, 2, '01:51:20', 68, '2026-09-20T12:20:00');

SET IDENTITY_INSERT dbo.Results OFF;
GO

--Verification Queries

--Verifying all six tables
SELECT TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'dbo'
  AND TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;
GO

-- Verifying users and roles
SELECT
    UserId, FirstName, LastName, Email, Role
FROM dbo.Users
ORDER BY UserId;
GO

-- Verifying events, organisers and event types
SELECT
    e.EventId,
    e.Name AS EventName,
    CONCAT(u.FirstName, N' ', u.LastName) AS Organiser,
    et.Name AS EventType,
    e.EventDate,
    e.Location,
    e.DistanceKm
FROM dbo.Events AS e
INNER JOIN dbo.Users AS u
    ON e.OrganiserId = u.UserId
INNER JOIN dbo.EventTypes AS et
    ON e.EventTypeId = et.EventTypeId
ORDER BY e.EventDate;
GO
-- Verifying every event has categories
SELECT
    c.CategoryId,
    e.Name AS EventName,
    c.Name AS CategoryName,
    c.MinimumAge,
    c.MaximumAge,
    c.DistanceKm
FROM dbo.Categories AS c
INNER JOIN dbo.Events AS e
    ON c.EventId = e.EventId
ORDER BY e.EventId, c.CategoryId;
GO
-- Verifying participant enrolments
SELECT
    en.EnrolmentId,
    CONCAT(u.FirstName, N' ', u.LastName) AS Participant,
    e.Name AS EventName,
    c.Name AS Category,
    en.Status,
    en.EnrolmentDate
FROM dbo.Enrolments AS en
INNER JOIN dbo.Users AS u
    ON en.ParticipantId = u.UserId
INNER JOIN dbo.Events AS e
    ON en.EventId = e.EventId
INNER JOIN dbo.Categories AS c
    ON en.CategoryId = c.CategoryId
ORDER BY en.EnrolmentId;
GO
