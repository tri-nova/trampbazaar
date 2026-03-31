SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

USE [master];
GO

IF DB_ID(N'TrampBazaar') IS NOT NULL
BEGIN
    ALTER DATABASE [TrampBazaar] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [TrampBazaar];
END
GO

CREATE DATABASE [TrampBazaar];
GO

USE [TrampBazaar];
GO

BEGIN TRANSACTION;

CREATE TABLE dbo.Users
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Users PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Email NVARCHAR(256) NOT NULL,
    NormalizedEmail AS UPPER(Email) PERSISTED,
    PhoneNumber NVARCHAR(32) NULL,
    NormalizedPhoneNumber AS UPPER(ISNULL(PhoneNumber, N'')) PERSISTED,
    PasswordHash NVARCHAR(512) NOT NULL,
    AccountType NVARCHAR(32) NOT NULL,
    Status NVARCHAR(32) NOT NULL CONSTRAINT DF_Users_Status DEFAULT N'pending_verification',
    EmailConfirmed BIT NOT NULL CONSTRAINT DF_Users_EmailConfirmed DEFAULT 0,
    PhoneConfirmed BIT NOT NULL CONSTRAINT DF_Users_PhoneConfirmed DEFAULT 0,
    LastLoginAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_Users_UpdatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT CK_Users_AccountType CHECK (AccountType IN (N'individual', N'corporate', N'admin')),
    CONSTRAINT CK_Users_Status CHECK (Status IN (N'pending_verification', N'active', N'passive', N'banned'))
);

CREATE TABLE dbo.UserProfiles
(
    UserId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_UserProfiles PRIMARY KEY,
    UserName NVARCHAR(64) NOT NULL,
    FullName NVARCHAR(200) NULL,
    ProfilePhotoUrl NVARCHAR(500) NULL,
    City NVARCHAR(100) NULL,
    District NVARCHAR(100) NULL,
    AddressLine NVARCHAR(300) NULL,
    RatingAverage DECIMAL(4,2) NOT NULL CONSTRAINT DF_UserProfiles_RatingAverage DEFAULT 0,
    RatingCount INT NOT NULL CONSTRAINT DF_UserProfiles_RatingCount DEFAULT 0,
    AboutText NVARCHAR(1000) NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_UserProfiles_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_UserProfiles_UpdatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_UserProfiles_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
    CONSTRAINT UQ_UserProfiles_UserName UNIQUE (UserName)
);

CREATE TABLE dbo.CompanyProfiles
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyProfiles PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    OwnerUserId UNIQUEIDENTIFIER NOT NULL,
    CompanyName NVARCHAR(250) NOT NULL,
    TaxNumber NVARCHAR(32) NULL,
    TaxOffice NVARCHAR(128) NULL,
    ContactEmail NVARCHAR(256) NULL,
    ContactPhone NVARCHAR(32) NULL,
    WebsiteUrl NVARCHAR(256) NULL,
    Description NVARCHAR(1200) NULL,
    City NVARCHAR(100) NULL,
    AddressLine NVARCHAR(300) NULL,
    Status NVARCHAR(32) NOT NULL CONSTRAINT DF_CompanyProfiles_Status DEFAULT N'active',
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_CompanyProfiles_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_CompanyProfiles_UpdatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_CompanyProfiles_Owner FOREIGN KEY (OwnerUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT CK_CompanyProfiles_TaxNumber_NotEmpty CHECK (TaxNumber IS NULL OR LEN(LTRIM(RTRIM(TaxNumber))) > 0)
);

CREATE TABLE dbo.Roles
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Roles PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Name NVARCHAR(64) NOT NULL,
    Description NVARCHAR(300) NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Roles_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Roles_Name UNIQUE (Name)
);

CREATE TABLE dbo.UserRoles
(
    UserId UNIQUEIDENTIFIER NOT NULL,
    RoleId UNIQUEIDENTIFIER NOT NULL,
    AssignedAt DATETIME2 NOT NULL CONSTRAINT DF_UserRoles_AssignedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_UserRoles PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_UserRoles_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_UserRoles_Role FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id)
);

CREATE TABLE dbo.Permissions
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Permissions PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    PermissionKey NVARCHAR(128) NOT NULL,
    Description NVARCHAR(300) NULL,
    CONSTRAINT UQ_Permissions_Key UNIQUE (PermissionKey)
);

CREATE TABLE dbo.RolePermissions
(
    RoleId UNIQUEIDENTIFIER NOT NULL,
    PermissionId UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT PK_RolePermissions PRIMARY KEY (RoleId, PermissionId),
    CONSTRAINT FK_RolePermissions_Role FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id),
    CONSTRAINT FK_RolePermissions_Permission FOREIGN KEY (PermissionId) REFERENCES dbo.Permissions(Id)
);

CREATE TABLE dbo.Categories
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Categories PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ParentCategoryId UNIQUEIDENTIFIER NULL,
    Name NVARCHAR(150) NOT NULL,
    Slug NVARCHAR(160) NOT NULL,
    SortOrder INT NOT NULL CONSTRAINT DF_Categories_SortOrder DEFAULT 0,
    IsActive BIT NOT NULL CONSTRAINT DF_Categories_IsActive DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Categories_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Categories_Parent FOREIGN KEY (ParentCategoryId) REFERENCES dbo.Categories(Id),
    CONSTRAINT UQ_Categories_Slug UNIQUE (Slug)
);

CREATE TABLE dbo.SaleModes
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_SaleModes PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ModeKey NVARCHAR(32) NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(300) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_SaleModes_IsActive DEFAULT 1,
    CONSTRAINT UQ_SaleModes_ModeKey UNIQUE (ModeKey)
);

CREATE TABLE dbo.Products
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Products PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    OwnerUserId UNIQUEIDENTIFIER NOT NULL,
    CategoryId UNIQUEIDENTIFIER NOT NULL,
    Brand NVARCHAR(120) NULL,
    Model NVARCHAR(120) NULL,
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    ConditionType NVARCHAR(32) NOT NULL,
    StockQuantity INT NOT NULL CONSTRAINT DF_Products_StockQuantity DEFAULT 1,
    CargoInfo NVARCHAR(300) NULL,
    City NVARCHAR(100) NULL,
    District NVARCHAR(100) NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_Products_UpdatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Products_Owner FOREIGN KEY (OwnerUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_Products_Category FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(Id),
    CONSTRAINT CK_Products_ConditionType CHECK (ConditionType IN (N'new', N'used'))
);

CREATE TABLE dbo.ProductMedia
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ProductMedia PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ProductId UNIQUEIDENTIFIER NOT NULL,
    MediaType NVARCHAR(16) NOT NULL,
    MediaUrl NVARCHAR(500) NOT NULL,
    SortOrder INT NOT NULL CONSTRAINT DF_ProductMedia_SortOrder DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_ProductMedia_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_ProductMedia_Product FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id),
    CONSTRAINT CK_ProductMedia_Type CHECK (MediaType IN (N'image', N'video'))
);

CREATE TABLE dbo.Listings
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Listings PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ProductId UNIQUEIDENTIFIER NOT NULL,
    SellerUserId UNIQUEIDENTIFIER NOT NULL,
    SaleModeId UNIQUEIDENTIFIER NOT NULL,
    ListingNumber BIGINT IDENTITY(1000, 1) NOT NULL,
    Price DECIMAL(18,2) NOT NULL CONSTRAINT DF_Listings_Price DEFAULT 0,
    CurrencyCode NCHAR(3) NOT NULL CONSTRAINT DF_Listings_CurrencyCode DEFAULT N'TRY',
    ListingStatus NVARCHAR(32) NOT NULL CONSTRAINT DF_Listings_Status DEFAULT N'draft',
    IsFeatured BIT NOT NULL CONSTRAINT DF_Listings_IsFeatured DEFAULT 0,
    IsHighlighted BIT NOT NULL CONSTRAINT DF_Listings_IsHighlighted DEFAULT 0,
    PublishStartAt DATETIME2 NULL,
    PublishEndAt DATETIME2 NULL,
    PublishedAt DATETIME2 NULL,
    ViewCount INT NOT NULL CONSTRAINT DF_Listings_ViewCount DEFAULT 0,
    FavoriteCount INT NOT NULL CONSTRAINT DF_Listings_FavoriteCount DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Listings_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_Listings_UpdatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Listings_Product FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id),
    CONSTRAINT FK_Listings_Seller FOREIGN KEY (SellerUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_Listings_SaleMode FOREIGN KEY (SaleModeId) REFERENCES dbo.SaleModes(Id),
    CONSTRAINT CK_Listings_Status CHECK (ListingStatus IN (N'draft', N'pending_approval', N'published', N'paused', N'sold', N'cancelled'))
);

CREATE TABLE dbo.Auctions
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Auctions PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ListingId UNIQUEIDENTIFIER NOT NULL,
    StartPrice DECIMAL(18,2) NOT NULL,
    MinBidIncrement DECIMAL(18,2) NOT NULL CONSTRAINT DF_Auctions_MinBidIncrement DEFAULT 1,
    CurrentBidAmount DECIMAL(18,2) NULL,
    CurrentWinnerUserId UNIQUEIDENTIFIER NULL,
    StartsAt DATETIME2 NOT NULL,
    EndsAt DATETIME2 NOT NULL,
    AutoExtendMinutes INT NOT NULL CONSTRAINT DF_Auctions_AutoExtendMinutes DEFAULT 0,
    AuctionStatus NVARCHAR(32) NOT NULL CONSTRAINT DF_Auctions_Status DEFAULT N'scheduled',
    ResultProcessed BIT NOT NULL CONSTRAINT DF_Auctions_ResultProcessed DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Auctions_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_Auctions_UpdatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Auctions_Listing FOREIGN KEY (ListingId) REFERENCES dbo.Listings(Id),
    CONSTRAINT FK_Auctions_Winner FOREIGN KEY (CurrentWinnerUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT UQ_Auctions_ListingId UNIQUE (ListingId),
    CONSTRAINT CK_Auctions_Status CHECK (AuctionStatus IN (N'scheduled', N'active', N'completed', N'cancelled'))
);

CREATE TABLE dbo.AuctionBids
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_AuctionBids PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    AuctionId UNIQUEIDENTIFIER NOT NULL,
    BidderUserId UNIQUEIDENTIFIER NOT NULL,
    BidAmount DECIMAL(18,2) NOT NULL,
    BidStatus NVARCHAR(32) NOT NULL CONSTRAINT DF_AuctionBids_Status DEFAULT N'active',
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_AuctionBids_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_AuctionBids_Auction FOREIGN KEY (AuctionId) REFERENCES dbo.Auctions(Id),
    CONSTRAINT FK_AuctionBids_Bidder FOREIGN KEY (BidderUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT CK_AuctionBids_Status CHECK (BidStatus IN (N'active', N'cancelled', N'winning', N'losing'))
);

CREATE TABLE dbo.Demands
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Demands PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    OwnerUserId UNIQUEIDENTIFIER NOT NULL,
    CompanyProfileId UNIQUEIDENTIFIER NULL,
    CategoryId UNIQUEIDENTIFIER NULL,
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    RequestedQuantity INT NULL,
    DeliveryLocation NVARCHAR(200) NULL,
    DeadlineAt DATETIME2 NOT NULL,
    DemandStatus NVARCHAR(32) NOT NULL CONSTRAINT DF_Demands_Status DEFAULT N'open',
    IsSealedBid BIT NOT NULL CONSTRAINT DF_Demands_IsSealedBid DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Demands_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_Demands_UpdatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Demands_Owner FOREIGN KEY (OwnerUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_Demands_Company FOREIGN KEY (CompanyProfileId) REFERENCES dbo.CompanyProfiles(Id),
    CONSTRAINT FK_Demands_Category FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(Id),
    CONSTRAINT CK_Demands_Status CHECK (DemandStatus IN (N'open', N'under_review', N'awarded', N'closed', N'cancelled'))
);

CREATE TABLE dbo.DemandOffers
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_DemandOffers PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    DemandId UNIQUEIDENTIFIER NOT NULL,
    OfferUserId UNIQUEIDENTIFIER NOT NULL,
    OfferAmount DECIMAL(18,2) NOT NULL,
    CurrencyCode NCHAR(3) NOT NULL CONSTRAINT DF_DemandOffers_Currency DEFAULT N'TRY',
    OfferNote NVARCHAR(1000) NULL,
    OfferStatus NVARCHAR(32) NOT NULL CONSTRAINT DF_DemandOffers_Status DEFAULT N'pending',
    RevisionNumber INT NOT NULL CONSTRAINT DF_DemandOffers_Revision DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_DemandOffers_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_DemandOffers_UpdatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_DemandOffers_Demand FOREIGN KEY (DemandId) REFERENCES dbo.Demands(Id),
    CONSTRAINT FK_DemandOffers_User FOREIGN KEY (OfferUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT CK_DemandOffers_Status CHECK (OfferStatus IN (N'pending', N'accepted', N'rejected', N'cancelled'))
);

CREATE TABLE dbo.Favorites
(
    UserId UNIQUEIDENTIFIER NOT NULL,
    ListingId UNIQUEIDENTIFIER NOT NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Favorites_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Favorites PRIMARY KEY (UserId, ListingId),
    CONSTRAINT FK_Favorites_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_Favorites_Listing FOREIGN KEY (ListingId) REFERENCES dbo.Listings(Id)
);

CREATE TABLE dbo.Conversations
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Conversations PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ListingId UNIQUEIDENTIFIER NULL,
    DemandId UNIQUEIDENTIFIER NULL,
    ConversationType NVARCHAR(32) NOT NULL,
    LastMessageAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Conversations_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Conversations_Listing FOREIGN KEY (ListingId) REFERENCES dbo.Listings(Id),
    CONSTRAINT FK_Conversations_Demand FOREIGN KEY (DemandId) REFERENCES dbo.Demands(Id),
    CONSTRAINT CK_Conversations_Type CHECK (ConversationType IN (N'listing', N'demand', N'direct_support'))
);

CREATE TABLE dbo.ConversationParticipants
(
    ConversationId UNIQUEIDENTIFIER NOT NULL,
    UserId UNIQUEIDENTIFIER NOT NULL,
    IsMuted BIT NOT NULL CONSTRAINT DF_ConversationParticipants_IsMuted DEFAULT 0,
    LastReadAt DATETIME2 NULL,
    JoinedAt DATETIME2 NOT NULL CONSTRAINT DF_ConversationParticipants_JoinedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_ConversationParticipants PRIMARY KEY (ConversationId, UserId),
    CONSTRAINT FK_ConversationParticipants_Conversation FOREIGN KEY (ConversationId) REFERENCES dbo.Conversations(Id),
    CONSTRAINT FK_ConversationParticipants_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)
);

CREATE TABLE dbo.Messages
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Messages PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ConversationId UNIQUEIDENTIFIER NOT NULL,
    SenderUserId UNIQUEIDENTIFIER NOT NULL,
    MessageType NVARCHAR(16) NOT NULL CONSTRAINT DF_Messages_Type DEFAULT N'text',
    MessageText NVARCHAR(MAX) NULL,
    AttachmentUrl NVARCHAR(500) NULL,
    IsReported BIT NOT NULL CONSTRAINT DF_Messages_IsReported DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Messages_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Messages_Conversation FOREIGN KEY (ConversationId) REFERENCES dbo.Conversations(Id),
    CONSTRAINT FK_Messages_Sender FOREIGN KEY (SenderUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT CK_Messages_Type CHECK (MessageType IN (N'text', N'image', N'file', N'system'))
);

CREATE TABLE dbo.Notifications
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Notifications PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    NotificationType NVARCHAR(64) NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Body NVARCHAR(600) NOT NULL,
    RelatedEntityType NVARCHAR(64) NULL,
    RelatedEntityId UNIQUEIDENTIFIER NULL,
    IsRead BIT NOT NULL CONSTRAINT DF_Notifications_IsRead DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Notifications_CreatedAt DEFAULT SYSUTCDATETIME(),
    ReadAt DATETIME2 NULL,
    CONSTRAINT FK_Notifications_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)
);

CREATE TABLE dbo.Reviews
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Reviews PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ReviewerUserId UNIQUEIDENTIFIER NOT NULL,
    ReviewedUserId UNIQUEIDENTIFIER NULL,
    ReviewedCompanyId UNIQUEIDENTIFIER NULL,
    ListingId UNIQUEIDENTIFIER NULL,
    Rating TINYINT NOT NULL,
    CommentText NVARCHAR(1000) NULL,
    ReviewStatus NVARCHAR(32) NOT NULL CONSTRAINT DF_Reviews_Status DEFAULT N'published',
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Reviews_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Reviews_Reviewer FOREIGN KEY (ReviewerUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_Reviews_ReviewedUser FOREIGN KEY (ReviewedUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_Reviews_ReviewedCompany FOREIGN KEY (ReviewedCompanyId) REFERENCES dbo.CompanyProfiles(Id),
    CONSTRAINT FK_Reviews_Listing FOREIGN KEY (ListingId) REFERENCES dbo.Listings(Id),
    CONSTRAINT CK_Reviews_Rating CHECK (Rating BETWEEN 1 AND 5),
    CONSTRAINT CK_Reviews_Status CHECK (ReviewStatus IN (N'published', N'pending', N'hidden', N'rejected'))
);

CREATE TABLE dbo.Packages
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Packages PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    PackageType NVARCHAR(32) NOT NULL,
    Name NVARCHAR(150) NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    CurrencyCode NCHAR(3) NOT NULL CONSTRAINT DF_Packages_Currency DEFAULT N'TRY',
    DurationDays INT NULL,
    ListingQuota INT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Packages_IsActive DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Packages_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_Packages_Type CHECK (PackageType IN (N'listing', N'featured', N'showcase', N'corporate_membership'))
);

CREATE TABLE dbo.Payments
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Payments PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    PackageId UNIQUEIDENTIFIER NULL,
    ListingId UNIQUEIDENTIFIER NULL,
    PaymentType NVARCHAR(32) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    CurrencyCode NCHAR(3) NOT NULL CONSTRAINT DF_Payments_Currency DEFAULT N'TRY',
    PaymentStatus NVARCHAR(32) NOT NULL CONSTRAINT DF_Payments_Status DEFAULT N'pending',
    ProviderName NVARCHAR(100) NULL,
    ProviderTransactionId NVARCHAR(100) NULL,
    PaidAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Payments_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Payments_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_Payments_Package FOREIGN KEY (PackageId) REFERENCES dbo.Packages(Id),
    CONSTRAINT FK_Payments_Listing FOREIGN KEY (ListingId) REFERENCES dbo.Listings(Id),
    CONSTRAINT CK_Payments_Type CHECK (PaymentType IN (N'listing_fee', N'featured_fee', N'showcase_fee', N'membership_fee', N'commission')),
    CONSTRAINT CK_Payments_Status CHECK (PaymentStatus IN (N'pending', N'paid', N'failed', N'refunded'))
);

CREATE TABLE dbo.Complaints
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Complaints PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ReporterUserId UNIQUEIDENTIFIER NOT NULL,
    TargetEntityType NVARCHAR(64) NOT NULL,
    TargetEntityId UNIQUEIDENTIFIER NOT NULL,
    Subject NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    ComplaintStatus NVARCHAR(32) NOT NULL CONSTRAINT DF_Complaints_Status DEFAULT N'open',
    AssignedAdminUserId UNIQUEIDENTIFIER NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Complaints_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_Complaints_UpdatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Complaints_Reporter FOREIGN KEY (ReporterUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_Complaints_Admin FOREIGN KEY (AssignedAdminUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT CK_Complaints_Status CHECK (ComplaintStatus IN (N'open', N'in_review', N'resolved', N'rejected'))
);

CREATE TABLE dbo.AuditLogs
(
    Id BIGINT NOT NULL IDENTITY(1,1) CONSTRAINT PK_AuditLogs PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NULL,
    ActionName NVARCHAR(128) NOT NULL,
    EntityType NVARCHAR(128) NULL,
    EntityId UNIQUEIDENTIFIER NULL,
    IpAddress NVARCHAR(64) NULL,
    UserAgent NVARCHAR(300) NULL,
    Details NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_AuditLogs_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_AuditLogs_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)
);

CREATE INDEX IX_Users_NormalizedEmail ON dbo.Users (NormalizedEmail);
CREATE UNIQUE INDEX UX_Users_PhoneNumber_NotNull ON dbo.Users (PhoneNumber) WHERE PhoneNumber IS NOT NULL;
CREATE INDEX IX_UserProfiles_FullName ON dbo.UserProfiles (FullName);
CREATE UNIQUE INDEX UX_CompanyProfiles_TaxNumber_NotNull ON dbo.CompanyProfiles (TaxNumber) WHERE TaxNumber IS NOT NULL;
CREATE INDEX IX_Products_CategoryId ON dbo.Products (CategoryId);
CREATE INDEX IX_Products_OwnerUserId ON dbo.Products (OwnerUserId);
CREATE INDEX IX_Listings_SellerUserId_Status ON dbo.Listings (SellerUserId, ListingStatus);
CREATE INDEX IX_Listings_SaleModeId_Status ON dbo.Listings (SaleModeId, ListingStatus);
CREATE INDEX IX_Listings_PublishedAt ON dbo.Listings (PublishedAt DESC);
CREATE INDEX IX_Auctions_EndsAt_Status ON dbo.Auctions (EndsAt, AuctionStatus);
CREATE INDEX IX_AuctionBids_AuctionId_CreatedAt ON dbo.AuctionBids (AuctionId, CreatedAt DESC);
CREATE INDEX IX_Demands_DeadlineAt_Status ON dbo.Demands (DeadlineAt, DemandStatus);
CREATE INDEX IX_DemandOffers_DemandId_CreatedAt ON dbo.DemandOffers (DemandId, CreatedAt DESC);
CREATE INDEX IX_Messages_ConversationId_CreatedAt ON dbo.Messages (ConversationId, CreatedAt DESC);
CREATE INDEX IX_Notifications_UserId_IsRead ON dbo.Notifications (UserId, IsRead);
CREATE INDEX IX_Reviews_ReviewedUserId ON dbo.Reviews (ReviewedUserId);
CREATE INDEX IX_AuditLogs_CreatedAt ON dbo.AuditLogs (CreatedAt DESC);

INSERT INTO dbo.Roles (Name, Description)
VALUES
    (N'Visitor', N'Sisteme giris yapmamis kullanici'),
    (N'User', N'Bireysel kullanici'),
    (N'CorporateUser', N'Kurumsal kullanici'),
    (N'Moderator', N'Icerik ve sikayet yoneticisi'),
    (N'Admin', N'Sistem yoneticisi'),
    (N'SuperAdmin', N'Tam yetkili sistem sahibi');

INSERT INTO dbo.Permissions (PermissionKey, Description)
VALUES
    (N'listing.create', N'Ilan olusturma'),
    (N'listing.edit', N'Ilan duzenleme'),
    (N'offer.create', N'Teklif verme'),
    (N'demand.create', N'Ihale acma'),
    (N'category.manage', N'Kategori yonetimi'),
    (N'user.manage', N'Kullanici yonetimi'),
    (N'package.manage', N'Paket ve fiyat yonetimi'),
    (N'report.view', N'Rapor goruntuleme');

INSERT INTO dbo.SaleModes (ModeKey, Name, Description)
VALUES
    (N'direct', N'Direkt Satis', N'Sabit fiyatli klasik ilan akisi'),
    (N'auction', N'Acik Artirma', N'Sureli teklif toplama akisi'),
    (N'trade', N'Takas', N'Karsilikli urun degisimi akisi'),
    (N'demand', N'Ihale / Talep', N'Kurumsal veya bireysel talep akisi');

INSERT INTO dbo.Categories (ParentCategoryId, Name, Slug, SortOrder)
VALUES
    (NULL, N'Elektronik', N'elektronik', 1),
    (NULL, N'Moda', N'moda', 2),
    (NULL, N'Ev ve Yasam', N'ev-ve-yasam', 3),
    (NULL, N'Hobi ve Koleksiyon', N'hobi-ve-koleksiyon', 4),
    (NULL, N'Otomotiv', N'otomotiv', 5),
    (NULL, N'Yedek Parca', N'yedek-parca', 6);

INSERT INTO dbo.Packages (PackageType, Name, Price, DurationDays, ListingQuota)
VALUES
    (N'listing', N'Ucretsiz Ilan', 0, 30, 1),
    (N'featured', N'One Cikan Ilan', 149.90, 7, NULL),
    (N'showcase', N'Vitrin Ilan', 349.90, 30, NULL),
    (N'corporate_membership', N'Kurumsal Aylik Paket', 1999.90, 30, 50);

COMMIT TRANSACTION;
GO
