SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF DB_ID(N'TrampBazaar') IS NULL
BEGIN
    THROW 50000, N'TrampBazaar veritabani bulunamadi. Once temel kurulum scriptleri calistirilmalidir.', 1;
END;
GO

USE [TrampBazaar];
GO

BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.SchemaVersions', N'U') IS NOT NULL
BEGIN
    MERGE dbo.SchemaVersions AS target
    USING
    (
        VALUES
            (N'008_support_procedures.sql')
    ) AS source(ScriptName)
    ON target.ScriptName = source.ScriptName
    WHEN NOT MATCHED THEN
        INSERT (ScriptName)
        VALUES (source.ScriptName);
END;

COMMIT TRANSACTION;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Ops_HealthSnapshot
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        (SELECT COUNT(*) FROM dbo.Users) AS TotalUsers,
        (SELECT COUNT(*) FROM dbo.Listings) AS TotalListings,
        (SELECT COUNT(*) FROM dbo.Listings WHERE ListingStatus = N'published') AS PublishedListings,
        (SELECT COUNT(*) FROM dbo.Conversations) AS TotalConversations,
        (SELECT COUNT(*) FROM dbo.Notifications WHERE IsRead = 0) AS UnreadNotifications,
        (SELECT COUNT(*) FROM dbo.Payments WHERE PaymentStatus = N'pending') AS PendingPayments,
        (SELECT COUNT(*) FROM dbo.Payments WHERE PaymentStatus = N'paid') AS PaidPayments,
        (SELECT COUNT(*) FROM dbo.Complaints WHERE ComplaintStatus = N'open') AS OpenComplaints,
        (SELECT COUNT(*) FROM dbo.CustomerOrders WHERE OrderStatus IN (N'pending', N'processing', N'shipped')) AS ActiveOrders,
        (SELECT COUNT(*) FROM dbo.StockAlerts WHERE IsActive = 1) AS ActiveStockAlerts,
        (SELECT COUNT(*) FROM dbo.PriceAlerts WHERE IsActive = 1) AS ActivePriceAlerts,
        SYSUTCDATETIME() AS SnapshotAtUtc;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Ops_UserAccountSnapshot
    @UserNameOrEmail NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UserId UNIQUEIDENTIFIER;

    SELECT TOP (1)
        @UserId = u.Id
    FROM dbo.Users u
    LEFT JOIN dbo.UserProfiles up ON up.UserId = u.Id
    WHERE u.Email = @UserNameOrEmail
       OR up.UserName = @UserNameOrEmail;

    IF @UserId IS NULL
    BEGIN
        THROW 50001, N'Kullanici bulunamadi.', 1;
    END;

    SELECT TOP (1)
        u.Id,
        u.Email,
        u.PhoneNumber,
        u.AccountType,
        u.Status,
        u.EmailConfirmed,
        up.UserName,
        up.FullName,
        up.City,
        up.District,
        u.CreatedAt,
        u.UpdatedAt
    FROM dbo.Users u
    LEFT JOIN dbo.UserProfiles up ON up.UserId = u.Id
    WHERE u.Id = @UserId;

    SELECT TOP (10)
        OrderNumber,
        OrderStatus,
        PaymentMethod,
        TotalAmount,
        CurrencyCode,
        OrderedAt,
        DeliveredAt
    FROM dbo.CustomerOrders
    WHERE UserId = @UserId
    ORDER BY OrderedAt DESC;

    SELECT TOP (20)
        EntryDate,
        EntryType,
        OrderNumber,
        Description,
        DebitAmount,
        CreditAmount,
        BalanceAfter,
        PaymentMethod
    FROM dbo.AccountLedgerEntries
    WHERE UserId = @UserId
    ORDER BY EntryDate DESC, CreatedAt DESC;

    SELECT TOP (20)
        n.NotificationType,
        n.Title,
        n.IsRead,
        n.CreatedAt
    FROM dbo.Notifications n
    WHERE n.UserId = @UserId
    ORDER BY n.CreatedAt DESC;

    SELECT TOP (20)
        p.PaymentType,
        p.PaymentStatus,
        p.Amount,
        p.CurrencyCode,
        p.ProviderName,
        p.CreatedAt,
        p.PaidAt
    FROM dbo.Payments p
    WHERE p.UserId = @UserId
    ORDER BY p.CreatedAt DESC;
END;
GO
