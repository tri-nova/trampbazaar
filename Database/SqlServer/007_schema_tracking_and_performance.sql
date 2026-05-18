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
            (N'005_account_profile_and_billing.sql'),
            (N'006_customer_account_modules.sql'),
            (N'007_schema_tracking_and_performance.sql')
    ) AS source(ScriptName)
    ON target.ScriptName = source.ScriptName
    WHEN NOT MATCHED THEN
        INSERT (ScriptName)
        VALUES (source.ScriptName);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.ConversationParticipants')
      AND name = N'IX_ConversationParticipants_UserId_ConversationId'
)
BEGIN
    CREATE INDEX IX_ConversationParticipants_UserId_ConversationId
        ON dbo.ConversationParticipants (UserId, ConversationId)
        INCLUDE (LastReadAt, JoinedAt, IsMuted);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Conversations')
      AND name = N'IX_Conversations_LastMessageAt'
)
BEGIN
    CREATE INDEX IX_Conversations_LastMessageAt
        ON dbo.Conversations (LastMessageAt DESC)
        INCLUDE (ConversationType, ListingId, DemandId, CreatedAt);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Notifications')
      AND name = N'IX_Notifications_UserId_CreatedAt'
)
BEGIN
    CREATE INDEX IX_Notifications_UserId_CreatedAt
        ON dbo.Notifications (UserId, CreatedAt DESC)
        INCLUDE (NotificationType, Title, IsRead, RelatedEntityType, RelatedEntityId);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Payments')
      AND name = N'IX_Payments_UserId_CreatedAt'
)
BEGIN
    CREATE INDEX IX_Payments_UserId_CreatedAt
        ON dbo.Payments (UserId, CreatedAt DESC)
        INCLUDE (PaymentStatus, PaymentType, Amount, CurrencyCode, ProviderName, PaidAt);
END;

IF OBJECT_ID(N'dbo.CustomerOrders', N'U') IS NOT NULL
   AND NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.CustomerOrders')
      AND name = N'IX_CustomerOrders_User_Status_OrderedAt'
)
BEGIN
    CREATE INDEX IX_CustomerOrders_User_Status_OrderedAt
        ON dbo.CustomerOrders (UserId, OrderStatus, OrderedAt DESC)
        INCLUDE (OrderNumber, PaymentMethod, TotalAmount, CurrencyCode, InstallmentCount, ItemCount, DeliveredAt);
END;

IF OBJECT_ID(N'dbo.AccountLedgerEntries', N'U') IS NOT NULL
   AND NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.AccountLedgerEntries')
      AND name = N'IX_AccountLedgerEntries_User_Date'
)
BEGIN
    CREATE INDEX IX_AccountLedgerEntries_User_Date
        ON dbo.AccountLedgerEntries (UserId, EntryDate DESC)
        INCLUDE (EntryType, OrderNumber, Description, DebitAmount, CreditAmount, BalanceAfter, PaymentMethod, ReceiptNumber);
END;

IF OBJECT_ID(N'dbo.StockAlerts', N'U') IS NOT NULL
   AND NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.StockAlerts')
      AND name = N'IX_StockAlerts_User_IsActive_CreatedAt'
)
BEGIN
    CREATE INDEX IX_StockAlerts_User_IsActive_CreatedAt
        ON dbo.StockAlerts (UserId, IsActive, CreatedAt DESC)
        INCLUDE (ListingId, Note);
END;

IF OBJECT_ID(N'dbo.PriceAlerts', N'U') IS NOT NULL
   AND NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.PriceAlerts')
      AND name = N'IX_PriceAlerts_User_IsActive_CreatedAt'
)
BEGIN
    CREATE INDEX IX_PriceAlerts_User_IsActive_CreatedAt
        ON dbo.PriceAlerts (UserId, IsActive, CreatedAt DESC)
        INCLUDE (ListingId, TargetPrice, CurrentPriceSnapshot, CurrencyCode);
END;

COMMIT TRANSACTION;
GO
