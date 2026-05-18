SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

USE [TrampBazaar];
GO

BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.CustomerOrders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CustomerOrders
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CustomerOrders PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        UserId UNIQUEIDENTIFIER NOT NULL,
        OrderNumber NVARCHAR(32) NOT NULL,
        OrderStatus NVARCHAR(32) NOT NULL CONSTRAINT DF_CustomerOrders_Status DEFAULT N'pending',
        PaymentMethod NVARCHAR(64) NOT NULL,
        TotalAmount DECIMAL(18,2) NOT NULL,
        CurrencyCode NCHAR(3) NOT NULL CONSTRAINT DF_CustomerOrders_Currency DEFAULT N'TRY',
        InstallmentCount INT NOT NULL CONSTRAINT DF_CustomerOrders_Installment DEFAULT 1,
        ItemCount INT NOT NULL CONSTRAINT DF_CustomerOrders_ItemCount DEFAULT 1,
        SummaryText NVARCHAR(300) NULL,
        OrderedAt DATETIME2 NOT NULL,
        DeliveredAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_CustomerOrders_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_CustomerOrders_UpdatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_CustomerOrders_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
        CONSTRAINT UQ_CustomerOrders_OrderNumber UNIQUE (OrderNumber),
        CONSTRAINT CK_CustomerOrders_Status CHECK (OrderStatus IN (N'pending', N'processing', N'shipped', N'delivered', N'cancelled'))
    );

    CREATE INDEX IX_CustomerOrders_User_OrderedAt ON dbo.CustomerOrders (UserId, OrderedAt DESC);
END;

IF OBJECT_ID(N'dbo.AccountLedgerEntries', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AccountLedgerEntries
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_AccountLedgerEntries PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        UserId UNIQUEIDENTIFIER NOT NULL,
        RelatedOrderId UNIQUEIDENTIFIER NULL,
        RelatedPaymentId UNIQUEIDENTIFIER NULL,
        EntryDate DATETIME2 NOT NULL,
        EntryType NVARCHAR(16) NOT NULL,
        OrderNumber NVARCHAR(32) NULL,
        Description NVARCHAR(400) NOT NULL,
        DebitAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_AccountLedgerEntries_Debit DEFAULT 0,
        CreditAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_AccountLedgerEntries_Credit DEFAULT 0,
        BalanceAfter DECIMAL(18,2) NOT NULL CONSTRAINT DF_AccountLedgerEntries_BalanceAfter DEFAULT 0,
        PaymentMethod NVARCHAR(64) NULL,
        ReceiptNumber NVARCHAR(64) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_AccountLedgerEntries_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_AccountLedgerEntries_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
        CONSTRAINT FK_AccountLedgerEntries_Order FOREIGN KEY (RelatedOrderId) REFERENCES dbo.CustomerOrders(Id),
        CONSTRAINT FK_AccountLedgerEntries_Payment FOREIGN KEY (RelatedPaymentId) REFERENCES dbo.Payments(Id),
        CONSTRAINT CK_AccountLedgerEntries_Type CHECK (EntryType IN (N'debit', N'credit'))
    );

    CREATE INDEX IX_AccountLedgerEntries_User_EntryDate ON dbo.AccountLedgerEntries (UserId, EntryDate DESC);
END;

IF OBJECT_ID(N'dbo.StockAlerts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StockAlerts
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_StockAlerts PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        UserId UNIQUEIDENTIFIER NOT NULL,
        ListingId UNIQUEIDENTIFIER NOT NULL,
        Note NVARCHAR(200) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_StockAlerts_IsActive DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_StockAlerts_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_StockAlerts_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
        CONSTRAINT FK_StockAlerts_Listing FOREIGN KEY (ListingId) REFERENCES dbo.Listings(Id)
    );

    CREATE UNIQUE INDEX UX_StockAlerts_User_Listing_Active
        ON dbo.StockAlerts (UserId, ListingId, IsActive);
END;

IF OBJECT_ID(N'dbo.PriceAlerts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PriceAlerts
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PriceAlerts PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        UserId UNIQUEIDENTIFIER NOT NULL,
        ListingId UNIQUEIDENTIFIER NOT NULL,
        TargetPrice DECIMAL(18,2) NOT NULL,
        CurrentPriceSnapshot DECIMAL(18,2) NOT NULL,
        CurrencyCode NCHAR(3) NOT NULL CONSTRAINT DF_PriceAlerts_Currency DEFAULT N'TRY',
        IsActive BIT NOT NULL CONSTRAINT DF_PriceAlerts_IsActive DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_PriceAlerts_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_PriceAlerts_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
        CONSTRAINT FK_PriceAlerts_Listing FOREIGN KEY (ListingId) REFERENCES dbo.Listings(Id)
    );

    CREATE UNIQUE INDEX UX_PriceAlerts_User_Listing_Target_Active
        ON dbo.PriceAlerts (UserId, ListingId, TargetPrice, IsActive);
END;

COMMIT TRANSACTION;
GO
