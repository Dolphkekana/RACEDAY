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