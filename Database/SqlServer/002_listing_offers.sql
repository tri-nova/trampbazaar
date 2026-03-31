SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

USE [TrampBazaar];
GO

IF OBJECT_ID(N'dbo.ListingOffers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ListingOffers
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ListingOffers PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        ListingId UNIQUEIDENTIFIER NOT NULL,
        BuyerUserId UNIQUEIDENTIFIER NOT NULL,
        OfferedPrice DECIMAL(18,2) NOT NULL,
        CurrencyCode NCHAR(3) NOT NULL CONSTRAINT DF_ListingOffers_CurrencyCode DEFAULT N'TRY',
        OfferNote NVARCHAR(1000) NULL,
        OfferStatus NVARCHAR(32) NOT NULL CONSTRAINT DF_ListingOffers_Status DEFAULT N'pending',
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_ListingOffers_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_ListingOffers_UpdatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_ListingOffers_Listing FOREIGN KEY (ListingId) REFERENCES dbo.Listings(Id),
        CONSTRAINT FK_ListingOffers_Buyer FOREIGN KEY (BuyerUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_ListingOffers_Status CHECK (OfferStatus IN (N'pending', N'accepted', N'rejected', N'cancelled')),
        CONSTRAINT CK_ListingOffers_Price CHECK (OfferedPrice >= 0)
    );

    CREATE INDEX IX_ListingOffers_ListingId_CreatedAt ON dbo.ListingOffers (ListingId, CreatedAt DESC);
    CREATE INDEX IX_ListingOffers_BuyerUserId_CreatedAt ON dbo.ListingOffers (BuyerUserId, CreatedAt DESC);
END
GO
