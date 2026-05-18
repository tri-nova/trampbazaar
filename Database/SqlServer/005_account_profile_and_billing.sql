SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

USE [TrampBazaar];
GO

BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.UserAccountDetails', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserAccountDetails
    (
        UserId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_UserAccountDetails PRIMARY KEY,
        FirstName NVARCHAR(100) NULL,
        LastName NVARCHAR(100) NULL,
        NationalId NVARCHAR(32) NULL,
        IsForeignCitizen BIT NOT NULL CONSTRAINT DF_UserAccountDetails_IsForeignCitizen DEFAULT 0,
        BirthDate DATE NULL,
        Gender NVARCHAR(32) NOT NULL CONSTRAINT DF_UserAccountDetails_Gender DEFAULT N'unspecified',
        MobilePhone NVARCHAR(32) NULL,
        WorkPhone NVARCHAR(32) NULL,
        PostalCode NVARCHAR(16) NULL,
        EmailOptIn BIT NOT NULL CONSTRAINT DF_UserAccountDetails_EmailOptIn DEFAULT 0,
        SmsOptIn BIT NOT NULL CONSTRAINT DF_UserAccountDetails_SmsOptIn DEFAULT 0,
        PhoneOptIn BIT NOT NULL CONSTRAINT DF_UserAccountDetails_PhoneOptIn DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_UserAccountDetails_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_UserAccountDetails_UpdatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_UserAccountDetails_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_UserAccountDetails_Gender CHECK (Gender IN (N'male', N'female', N'unspecified'))
    );
END;

IF OBJECT_ID(N'dbo.UserBillingAddresses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserBillingAddresses
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_UserBillingAddresses PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        UserId UNIQUEIDENTIFIER NOT NULL,
        InvoiceType NVARCHAR(32) NOT NULL CONSTRAINT DF_UserBillingAddresses_InvoiceType DEFAULT N'individual',
        AddressTitle NVARCHAR(120) NOT NULL,
        FullName NVARCHAR(200) NOT NULL,
        IdentityNumber NVARCHAR(32) NULL,
        TaxOffice NVARCHAR(128) NULL,
        TaxNumber NVARCHAR(32) NULL,
        Country NVARCHAR(100) NOT NULL,
        City NVARCHAR(100) NOT NULL,
        District NVARCHAR(100) NOT NULL,
        Neighborhood NVARCHAR(100) NULL,
        PostalCode NVARCHAR(16) NULL,
        PhoneNumber NVARCHAR(32) NOT NULL,
        AddressLine NVARCHAR(300) NOT NULL,
        IsDefault BIT NOT NULL CONSTRAINT DF_UserBillingAddresses_IsDefault DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_UserBillingAddresses_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_UserBillingAddresses_UpdatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_UserBillingAddresses_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_UserBillingAddresses_InvoiceType CHECK (InvoiceType IN (N'individual', N'corporate'))
    );

    CREATE INDEX IX_UserBillingAddresses_UserId_Default
        ON dbo.UserBillingAddresses (UserId, IsDefault DESC, UpdatedAt DESC);
END;

COMMIT TRANSACTION;
GO
