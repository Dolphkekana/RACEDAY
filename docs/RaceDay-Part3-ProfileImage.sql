-- Part 3 addition for databases created from docs/RaceDay-Database.sql.
-- EF Core migrations add this column automatically when the API starts in Development.
IF COL_LENGTH('dbo.Users', 'ProfileImageUrl') IS NULL
BEGIN
    ALTER TABLE dbo.Users
        ADD ProfileImageUrl NVARCHAR(500) NULL;
END
