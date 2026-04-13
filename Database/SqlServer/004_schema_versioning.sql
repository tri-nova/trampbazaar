IF DB_ID(N'TrampBazaar') IS NULL
BEGIN
    THROW 50000, N'TrampBazaar veritabani bulunamadi. Once 001_initial_setup.sql calistirilmalidir.', 1;
END;
GO

USE [TrampBazaar];
GO

IF OBJECT_ID(N'dbo.SchemaVersions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SchemaVersions
    (
        ScriptName NVARCHAR(255) NOT NULL PRIMARY KEY,
        AppliedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_SchemaVersions_AppliedAtUtc DEFAULT SYSUTCDATETIME(),
        AppliedBy NVARCHAR(255) NOT NULL CONSTRAINT DF_SchemaVersions_AppliedBy DEFAULT SUSER_SNAME()
    );
END;
GO

MERGE dbo.SchemaVersions AS target
USING
(
    VALUES
        (N'001_initial_setup.sql'),
        (N'002_listing_offers.sql'),
        (N'003_grant_admin_role.sql'),
        (N'004_schema_versioning.sql')
) AS source(ScriptName)
ON target.ScriptName = source.ScriptName
WHEN NOT MATCHED THEN
    INSERT (ScriptName)
    VALUES (source.ScriptName);
GO
