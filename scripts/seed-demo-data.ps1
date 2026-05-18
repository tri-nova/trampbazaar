param(
    [string]$ConnectionString
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-DefaultConnectionString {
    if (-not [string]::IsNullOrWhiteSpace($env:TB_SQLSERVER_CONNECTION)) {
        return [string]$env:TB_SQLSERVER_CONNECTION
    }

    $candidatePaths = @(
        (Join-Path $PSScriptRoot "..\trampbazaar.Api\appsettings.Development.Local.json"),
        (Join-Path $PSScriptRoot "..\trampbazaar.Api\appsettings.Local.json"),
        (Join-Path $PSScriptRoot "..\trampbazaar.Api\appsettings.Development.json")
    )

    foreach ($configPath in $candidatePaths) {
        if (-not (Test-Path $configPath)) {
            continue
        }

        $config = Get-Content $configPath -Raw | ConvertFrom-Json
        $connectionString = [string]$config.ConnectionStrings.SqlServer
        if (-not [string]::IsNullOrWhiteSpace($connectionString) -and $connectionString -notmatch 'replace-me') {
            return $connectionString
        }
    }

    return ""
}

function New-PasswordHash {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Password
    )

    $salt = New-Object byte[] 16
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $rng.GetBytes($salt)
    }
    finally {
        $rng.Dispose()
    }
    $pbkdf2 = New-Object System.Security.Cryptography.Rfc2898DeriveBytes(
        $Password,
        $salt,
        100000,
        [System.Security.Cryptography.HashAlgorithmName]::SHA256)
    try {
        $hash = $pbkdf2.GetBytes(32)
    }
    finally {
        $pbkdf2.Dispose()
    }

    return "{0}:{1}" -f [Convert]::ToBase64String($salt), [Convert]::ToBase64String($hash)
}

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    $ConnectionString = Get-DefaultConnectionString
}

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    throw "Connection string bulunamadi. -ConnectionString verin, TB_SQLSERVER_CONNECTION ayarlayin veya trampbazaar.Api/appsettings.Development.Local.json dosyasini doldurun."
}

$batuPasswordHash = New-PasswordHash -Password "Password123!"
$aysePasswordHash = New-PasswordHash -Password "Password123!"
$adminPasswordHash = New-PasswordHash -Password "Password123!"

$sql = @"
SET NOCOUNT ON;
SET XACT_ABORT ON;

USE [TrampBazaar];

BEGIN TRANSACTION;

DECLARE @BatuUserId UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
DECLARE @AyseUserId UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222222';
DECLARE @AdminUserId UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333333333';

DECLARE @BatuProfileId UNIQUEIDENTIFIER = @BatuUserId;
DECLARE @AyseProfileId UNIQUEIDENTIFIER = @AyseUserId;
DECLARE @AdminProfileId UNIQUEIDENTIFIER = @AdminUserId;

DECLARE @BatuOwnedProductId UNIQUEIDENTIFIER = '44444444-4444-4444-4444-444444444444';
DECLARE @BatuOwnedListingId UNIQUEIDENTIFIER = '55555555-5555-5555-5555-555555555555';
DECLARE @RetroProductId UNIQUEIDENTIFIER = '66666666-6666-6666-6666-666666666666';
DECLARE @RetroListingId UNIQUEIDENTIFIER = '77777777-7777-7777-7777-777777777777';
DECLARE @PikapProductId UNIQUEIDENTIFIER = '88888888-8888-8888-8888-888888888888';
DECLARE @PikapListingId UNIQUEIDENTIFIER = '99999999-9999-9999-9999-999999999999';
DECLARE @AuctionId UNIQUEIDENTIFIER = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
DECLARE @ConversationId UNIQUEIDENTIFIER = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
DECLARE @MessageOneId UNIQUEIDENTIFIER = 'cccccccc-cccc-cccc-cccc-cccccccccccc';
DECLARE @MessageTwoId UNIQUEIDENTIFIER = 'dddddddd-dddd-dddd-dddd-dddddddddddd';
DECLARE @NotificationOneId UNIQUEIDENTIFIER = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee';
DECLARE @NotificationTwoId UNIQUEIDENTIFIER = 'ffffffff-ffff-ffff-ffff-ffffffffffff';
DECLARE @PaymentId UNIQUEIDENTIFIER = '12121212-1212-1212-1212-121212121212';
DECLARE @ComplaintId UNIQUEIDENTIFIER = '13131313-1313-1313-1313-131313131313';
DECLARE @BatuBillingAddressId UNIQUEIDENTIFIER = '14141414-1414-1414-1414-141414141414';
DECLARE @AyseBillingAddressId UNIQUEIDENTIFIER = '15151515-1515-1515-1515-151515151515';
DECLARE @OrderOneId UNIQUEIDENTIFIER = '16161616-1616-1616-1616-161616161616';
DECLARE @OrderTwoId UNIQUEIDENTIFIER = '17171717-1717-1717-1717-171717171717';
DECLARE @LedgerOneId UNIQUEIDENTIFIER = '18181818-1818-1818-1818-181818181818';
DECLARE @LedgerTwoId UNIQUEIDENTIFIER = '19191919-1919-1919-1919-191919191919';
DECLARE @LedgerThreeId UNIQUEIDENTIFIER = '20202020-2020-2020-2020-202020202020';
DECLARE @StockAlertId UNIQUEIDENTIFIER = '21212121-2121-2121-2121-212121212121';
DECLARE @PriceAlertId UNIQUEIDENTIFIER = '23232323-2323-2323-2323-232323232323';

DECLARE @ElectronicsCategoryId UNIQUEIDENTIFIER = (
    SELECT TOP 1 Id FROM dbo.Categories WHERE Slug = N'elektronik'
);
DECLARE @HobbyCategoryId UNIQUEIDENTIFIER = (
    SELECT TOP 1 Id FROM dbo.Categories WHERE Slug = N'hobi-ve-koleksiyon'
);
DECLARE @DirectSaleModeId UNIQUEIDENTIFIER = (
    SELECT TOP 1 Id FROM dbo.SaleModes WHERE ModeKey = N'direct'
);
DECLARE @AuctionSaleModeId UNIQUEIDENTIFIER = (
    SELECT TOP 1 Id FROM dbo.SaleModes WHERE ModeKey = N'auction'
);
DECLARE @PackageId UNIQUEIDENTIFIER = (
    SELECT TOP 1 Id FROM dbo.Packages WHERE Name = N'One Cikan Ilan'
);
DECLARE @SuperAdminRoleId UNIQUEIDENTIFIER = (
    SELECT TOP 1 Id FROM dbo.Roles WHERE Name = N'SuperAdmin'
);
DECLARE @UserRoleId UNIQUEIDENTIFIER = (
    SELECT TOP 1 Id FROM dbo.Roles WHERE Name = N'User'
);

IF @ElectronicsCategoryId IS NULL OR @HobbyCategoryId IS NULL
    THROW 51000, N'Demo seed icin gerekli kategoriler bulunamadi.', 1;

IF @DirectSaleModeId IS NULL OR @AuctionSaleModeId IS NULL
    THROW 51000, N'Demo seed icin gerekli satis modlari bulunamadi.', 1;

IF @PackageId IS NULL
    THROW 51000, N'Demo seed icin gerekli paket bulunamadi.', 1;

IF @SuperAdminRoleId IS NULL OR @UserRoleId IS NULL
    THROW 51000, N'Demo seed icin gerekli roller bulunamadi.', 1;

IF EXISTS (SELECT 1 FROM dbo.Users WHERE Id = @BatuUserId)
BEGIN
    UPDATE dbo.Users
    SET Email = N'batu@example.com',
        PhoneNumber = N'05050000000',
        PasswordHash = N'$batuPasswordHash',
        AccountType = N'individual',
        Status = N'active',
        EmailConfirmed = 1,
        UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @BatuUserId;
END
ELSE
BEGIN
    INSERT INTO dbo.Users (Id, Email, PasswordHash, AccountType, Status, EmailConfirmed, PhoneConfirmed, CreatedAt, UpdatedAt)
    VALUES (@BatuUserId, N'batu@example.com', N'$batuPasswordHash', N'individual', N'active', 1, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    UPDATE dbo.Users SET PhoneNumber = N'05050000000' WHERE Id = @BatuUserId;
END;

IF EXISTS (SELECT 1 FROM dbo.UserProfiles WHERE UserId = @BatuProfileId)
BEGIN
    UPDATE dbo.UserProfiles
    SET UserName = N'batu',
        FullName = N'Batu Yildiz',
        City = N'Istanbul',
        District = N'Kadikoy',
        AboutText = N'Demo alici hesabi',
        UpdatedAt = SYSUTCDATETIME()
    WHERE UserId = @BatuProfileId;
END
ELSE
BEGIN
    INSERT INTO dbo.UserProfiles (UserId, UserName, FullName, City, District, AboutText, CreatedAt, UpdatedAt)
    VALUES (@BatuProfileId, N'batu', N'Batu Yildiz', N'Istanbul', N'Kadikoy', N'Demo alici hesabi', SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM dbo.Users WHERE Id = @AyseUserId)
BEGIN
    UPDATE dbo.Users
    SET Email = N'ayse@example.com',
        PhoneNumber = N'05320000000',
        PasswordHash = N'$aysePasswordHash',
        AccountType = N'individual',
        Status = N'active',
        EmailConfirmed = 1,
        UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @AyseUserId;
END
ELSE
BEGIN
    INSERT INTO dbo.Users (Id, Email, PasswordHash, AccountType, Status, EmailConfirmed, PhoneConfirmed, CreatedAt, UpdatedAt)
    VALUES (@AyseUserId, N'ayse@example.com', N'$aysePasswordHash', N'individual', N'active', 1, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    UPDATE dbo.Users SET PhoneNumber = N'05320000000' WHERE Id = @AyseUserId;
END;

IF EXISTS (SELECT 1 FROM dbo.UserProfiles WHERE UserId = @AyseProfileId)
BEGIN
    UPDATE dbo.UserProfiles
    SET UserName = N'ayse',
        FullName = N'Ayse Demir',
        City = N'Ankara',
        District = N'Cankaya',
        AboutText = N'Demo satici hesabi',
        UpdatedAt = SYSUTCDATETIME()
    WHERE UserId = @AyseProfileId;
END
ELSE
BEGIN
    INSERT INTO dbo.UserProfiles (UserId, UserName, FullName, City, District, AboutText, CreatedAt, UpdatedAt)
    VALUES (@AyseProfileId, N'ayse', N'Ayse Demir', N'Ankara', N'Cankaya', N'Demo satici hesabi', SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM dbo.Users WHERE Id = @AdminUserId)
BEGIN
    UPDATE dbo.Users
    SET Email = N'admin@example.com',
        PhoneNumber = N'03120000000',
        PasswordHash = N'$adminPasswordHash',
        AccountType = N'admin',
        Status = N'active',
        EmailConfirmed = 1,
        UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @AdminUserId;
END
ELSE
BEGIN
    INSERT INTO dbo.Users (Id, Email, PasswordHash, AccountType, Status, EmailConfirmed, PhoneConfirmed, CreatedAt, UpdatedAt)
    VALUES (@AdminUserId, N'admin@example.com', N'$adminPasswordHash', N'admin', N'active', 1, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    UPDATE dbo.Users SET PhoneNumber = N'03120000000' WHERE Id = @AdminUserId;
END;

IF EXISTS (SELECT 1 FROM dbo.UserProfiles WHERE UserId = @AdminProfileId)
BEGIN
    UPDATE dbo.UserProfiles
    SET UserName = N'superadmin',
        FullName = N'System Admin',
        City = N'Istanbul',
        AboutText = N'Demo yonetici hesabi',
        UpdatedAt = SYSUTCDATETIME()
    WHERE UserId = @AdminProfileId;
END
ELSE
BEGIN
    INSERT INTO dbo.UserProfiles (UserId, UserName, FullName, City, AboutText, CreatedAt, UpdatedAt)
    VALUES (@AdminProfileId, N'superadmin', N'System Admin', N'Istanbul', N'Demo yonetici hesabi', SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.UserRoles WHERE UserId = @BatuUserId AND RoleId = @UserRoleId)
    INSERT INTO dbo.UserRoles (UserId, RoleId, AssignedAt) VALUES (@BatuUserId, @UserRoleId, SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.UserRoles WHERE UserId = @AyseUserId AND RoleId = @UserRoleId)
    INSERT INTO dbo.UserRoles (UserId, RoleId, AssignedAt) VALUES (@AyseUserId, @UserRoleId, SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.UserRoles WHERE UserId = @AdminUserId AND RoleId = @SuperAdminRoleId)
    INSERT INTO dbo.UserRoles (UserId, RoleId, AssignedAt) VALUES (@AdminUserId, @SuperAdminRoleId, SYSUTCDATETIME());

IF OBJECT_ID(N'dbo.UserAccountDetails', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.UserAccountDetails WHERE UserId = @BatuUserId)
    BEGIN
        UPDATE dbo.UserAccountDetails
        SET FirstName = N'Batu',
            LastName = N'Yildiz',
            NationalId = N'12345678901',
            IsForeignCitizen = 0,
            BirthDate = '1990-01-15',
            Gender = N'male',
            MobilePhone = N'05050000000',
            WorkPhone = N'03120000000',
            PostalCode = N'34710',
            EmailOptIn = 1,
            SmsOptIn = 1,
            PhoneOptIn = 0,
            UpdatedAt = SYSUTCDATETIME()
        WHERE UserId = @BatuUserId;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.UserAccountDetails
        (
            UserId, FirstName, LastName, NationalId, IsForeignCitizen, BirthDate, Gender, MobilePhone, WorkPhone,
            PostalCode, EmailOptIn, SmsOptIn, PhoneOptIn, CreatedAt, UpdatedAt
        )
        VALUES
        (
            @BatuUserId, N'Batu', N'Yildiz', N'12345678901', 0, '1990-01-15', N'male', N'05050000000', N'03120000000',
            N'34710', 1, 1, 0, SYSUTCDATETIME(), SYSUTCDATETIME()
        );
    END;

    IF EXISTS (SELECT 1 FROM dbo.UserAccountDetails WHERE UserId = @AyseUserId)
    BEGIN
        UPDATE dbo.UserAccountDetails
        SET FirstName = N'Ayse',
            LastName = N'Demir',
            NationalId = N'23456789012',
            IsForeignCitizen = 0,
            BirthDate = '1992-05-08',
            Gender = N'female',
            MobilePhone = N'05320000000',
            WorkPhone = N'03124000000',
            PostalCode = N'06680',
            EmailOptIn = 1,
            SmsOptIn = 1,
            PhoneOptIn = 1,
            UpdatedAt = SYSUTCDATETIME()
        WHERE UserId = @AyseUserId;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.UserAccountDetails
        (
            UserId, FirstName, LastName, NationalId, IsForeignCitizen, BirthDate, Gender, MobilePhone, WorkPhone,
            PostalCode, EmailOptIn, SmsOptIn, PhoneOptIn, CreatedAt, UpdatedAt
        )
        VALUES
        (
            @AyseUserId, N'Ayse', N'Demir', N'23456789012', 0, '1992-05-08', N'female', N'05320000000', N'03124000000',
            N'06680', 1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
        );
    END;
END;

IF OBJECT_ID(N'dbo.UserBillingAddresses', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.UserBillingAddresses WHERE Id = @BatuBillingAddressId)
    BEGIN
        UPDATE dbo.UserBillingAddresses
        SET UserId = @BatuUserId,
            InvoiceType = N'individual',
            AddressTitle = N'Ev',
            FullName = N'Batu Yildiz',
            IdentityNumber = N'12345678901',
            Country = N'Turkiye',
            City = N'Istanbul',
            District = N'Kadikoy',
            Neighborhood = N'Feneryolu',
            PostalCode = N'34710',
            PhoneNumber = N'05050000000',
            AddressLine = N'Feneryolu Mah. Demo Sok. No:10 Daire:3 Kadikoy / Istanbul',
            IsDefault = 1,
            UpdatedAt = SYSUTCDATETIME()
        WHERE Id = @BatuBillingAddressId;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.UserBillingAddresses
        (
            Id, UserId, InvoiceType, AddressTitle, FullName, IdentityNumber, Country, City, District, Neighborhood,
            PostalCode, PhoneNumber, AddressLine, IsDefault, CreatedAt, UpdatedAt
        )
        VALUES
        (
            @BatuBillingAddressId, @BatuUserId, N'individual', N'Ev', N'Batu Yildiz', N'12345678901', N'Turkiye', N'Istanbul', N'Kadikoy', N'Feneryolu',
            N'34710', N'05050000000', N'Feneryolu Mah. Demo Sok. No:10 Daire:3 Kadikoy / Istanbul', 1, SYSUTCDATETIME(), SYSUTCDATETIME()
        );
    END;

    IF EXISTS (SELECT 1 FROM dbo.UserBillingAddresses WHERE Id = @AyseBillingAddressId)
    BEGIN
        UPDATE dbo.UserBillingAddresses
        SET UserId = @AyseUserId,
            InvoiceType = N'corporate',
            AddressTitle = N'Ofis',
            FullName = N'Demir Koleksiyon',
            TaxOffice = N'Cankaya',
            TaxNumber = N'1234567890',
            Country = N'Turkiye',
            City = N'Ankara',
            District = N'Cankaya',
            Neighborhood = N'Gaziosmanpasa',
            PostalCode = N'06680',
            PhoneNumber = N'05320000000',
            AddressLine = N'GOP Mah. Koleksiyon Cad. No:21 Cankaya / Ankara',
            IsDefault = 1,
            UpdatedAt = SYSUTCDATETIME()
        WHERE Id = @AyseBillingAddressId;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.UserBillingAddresses
        (
            Id, UserId, InvoiceType, AddressTitle, FullName, TaxOffice, TaxNumber, Country, City, District, Neighborhood,
            PostalCode, PhoneNumber, AddressLine, IsDefault, CreatedAt, UpdatedAt
        )
        VALUES
        (
            @AyseBillingAddressId, @AyseUserId, N'corporate', N'Ofis', N'Demir Koleksiyon', N'Cankaya', N'1234567890', N'Turkiye', N'Ankara', N'Cankaya', N'Gaziosmanpasa',
            N'06680', N'05320000000', N'GOP Mah. Koleksiyon Cad. No:21 Cankaya / Ankara', 1, SYSUTCDATETIME(), SYSUTCDATETIME()
        );
    END;
END;

IF EXISTS (SELECT 1 FROM dbo.Products WHERE Id = @BatuOwnedProductId)
BEGIN
    UPDATE dbo.Products
    SET OwnerUserId = @BatuUserId,
        CategoryId = @ElectronicsCategoryId,
        Brand = N'Coleman',
        Model = N'Compact 2024',
        Title = N'Kamp Sobasi',
        Description = N'Az kullanilmis kamp sobasi, kutulu ve hazir teslim.',
        ConditionType = N'used',
        City = N'Istanbul',
        District = N'Atasehir',
        UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @BatuOwnedProductId;
END
ELSE
BEGIN
    INSERT INTO dbo.Products (Id, OwnerUserId, CategoryId, Brand, Model, Title, Description, ConditionType, StockQuantity, City, District, CreatedAt, UpdatedAt)
    VALUES (@BatuOwnedProductId, @BatuUserId, @ElectronicsCategoryId, N'Coleman', N'Compact 2024', N'Kamp Sobasi', N'Az kullanilmis kamp sobasi, kutulu ve hazir teslim.', N'used', 1, N'Istanbul', N'Atasehir', SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM dbo.Listings WHERE Id = @BatuOwnedListingId)
BEGIN
    UPDATE dbo.Listings
    SET ProductId = @BatuOwnedProductId,
        SellerUserId = @BatuUserId,
        SaleModeId = @DirectSaleModeId,
        Price = 2750,
        CurrencyCode = N'TRY',
        ListingStatus = N'published',
        IsFeatured = 0,
        PublishedAt = DATEADD(day, -1, SYSUTCDATETIME()),
        UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @BatuOwnedListingId;
END
ELSE
BEGIN
    INSERT INTO dbo.Listings (Id, ProductId, SellerUserId, SaleModeId, Price, CurrencyCode, ListingStatus, IsFeatured, IsHighlighted, PublishStartAt, PublishedAt, CreatedAt, UpdatedAt)
    VALUES (@BatuOwnedListingId, @BatuOwnedProductId, @BatuUserId, @DirectSaleModeId, 2750, N'TRY', N'published', 0, 0, DATEADD(day, -1, SYSUTCDATETIME()), DATEADD(day, -1, SYSUTCDATETIME()), DATEADD(day, -1, SYSUTCDATETIME()), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM dbo.Products WHERE Id = @RetroProductId)
BEGIN
    UPDATE dbo.Products
    SET OwnerUserId = @AyseUserId,
        CategoryId = @HobbyCategoryId,
        Brand = N'Canon',
        Model = N'AE-1',
        Title = N'Retro Kamera',
        Description = N'Calisir durumda analog kamera. Kayis ve canta dahil.',
        ConditionType = N'used',
        City = N'Ankara',
        District = N'Cankaya',
        UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @RetroProductId;
END
ELSE
BEGIN
    INSERT INTO dbo.Products (Id, OwnerUserId, CategoryId, Brand, Model, Title, Description, ConditionType, StockQuantity, City, District, CreatedAt, UpdatedAt)
    VALUES (@RetroProductId, @AyseUserId, @HobbyCategoryId, N'Canon', N'AE-1', N'Retro Kamera', N'Calisir durumda analog kamera. Kayis ve canta dahil.', N'used', 1, N'Ankara', N'Cankaya', SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM dbo.Listings WHERE Id = @RetroListingId)
BEGIN
    UPDATE dbo.Listings
    SET ProductId = @RetroProductId,
        SellerUserId = @AyseUserId,
        SaleModeId = @DirectSaleModeId,
        Price = 3250,
        CurrencyCode = N'TRY',
        ListingStatus = N'published',
        IsFeatured = 1,
        IsHighlighted = 1,
        PublishedAt = DATEADD(day, -2, SYSUTCDATETIME()),
        UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @RetroListingId;
END
ELSE
BEGIN
    INSERT INTO dbo.Listings (Id, ProductId, SellerUserId, SaleModeId, Price, CurrencyCode, ListingStatus, IsFeatured, IsHighlighted, PublishStartAt, PublishedAt, CreatedAt, UpdatedAt)
    VALUES (@RetroListingId, @RetroProductId, @AyseUserId, @DirectSaleModeId, 3250, N'TRY', N'published', 1, 1, DATEADD(day, -2, SYSUTCDATETIME()), DATEADD(day, -2, SYSUTCDATETIME()), DATEADD(day, -2, SYSUTCDATETIME()), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM dbo.Products WHERE Id = @PikapProductId)
BEGIN
    UPDATE dbo.Products
    SET OwnerUserId = @AyseUserId,
        CategoryId = @HobbyCategoryId,
        Brand = N'Pioneer',
        Model = N'PL-12D',
        Title = N'Vintage Pikap',
        Description = N'Koleksiyonluk pikap. Igne yeni degisti.',
        ConditionType = N'used',
        City = N'Ankara',
        District = N'Cankaya',
        UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @PikapProductId;
END
ELSE
BEGIN
    INSERT INTO dbo.Products (Id, OwnerUserId, CategoryId, Brand, Model, Title, Description, ConditionType, StockQuantity, City, District, CreatedAt, UpdatedAt)
    VALUES (@PikapProductId, @AyseUserId, @HobbyCategoryId, N'Pioneer', N'PL-12D', N'Vintage Pikap', N'Koleksiyonluk pikap. Igne yeni degisti.', N'used', 1, N'Ankara', N'Cankaya', SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM dbo.Listings WHERE Id = @PikapListingId)
BEGIN
    UPDATE dbo.Listings
    SET ProductId = @PikapProductId,
        SellerUserId = @AyseUserId,
        SaleModeId = @AuctionSaleModeId,
        Price = 1500,
        CurrencyCode = N'TRY',
        ListingStatus = N'published',
        PublishedAt = DATEADD(hour, -3, SYSUTCDATETIME()),
        UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @PikapListingId;
END
ELSE
BEGIN
    INSERT INTO dbo.Listings (Id, ProductId, SellerUserId, SaleModeId, Price, CurrencyCode, ListingStatus, IsFeatured, IsHighlighted, PublishStartAt, PublishedAt, CreatedAt, UpdatedAt)
    VALUES (@PikapListingId, @PikapProductId, @AyseUserId, @AuctionSaleModeId, 1500, N'TRY', N'published', 0, 0, DATEADD(hour, -3, SYSUTCDATETIME()), DATEADD(hour, -3, SYSUTCDATETIME()), DATEADD(hour, -3, SYSUTCDATETIME()), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM dbo.Auctions WHERE Id = @AuctionId)
BEGIN
    UPDATE dbo.Auctions
    SET ListingId = @PikapListingId,
        StartPrice = 1500,
        MinBidIncrement = 100,
        CurrentBidAmount = 1700,
        CurrentWinnerUserId = NULL,
        StartsAt = DATEADD(hour, -2, SYSUTCDATETIME()),
        EndsAt = DATEADD(hour, 22, SYSUTCDATETIME()),
        AutoExtendMinutes = 10,
        AuctionStatus = N'active',
        ResultProcessed = 0,
        UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @AuctionId;
END
ELSE
BEGIN
    INSERT INTO dbo.Auctions (Id, ListingId, StartPrice, MinBidIncrement, CurrentBidAmount, CurrentWinnerUserId, StartsAt, EndsAt, AutoExtendMinutes, AuctionStatus, ResultProcessed, CreatedAt, UpdatedAt)
    VALUES (@AuctionId, @PikapListingId, 1500, 100, 1700, NULL, DATEADD(hour, -2, SYSUTCDATETIME()), DATEADD(hour, 22, SYSUTCDATETIME()), 10, N'active', 0, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM dbo.Conversations WHERE Id = @ConversationId)
BEGIN
    UPDATE dbo.Conversations
    SET ListingId = @RetroListingId,
        ConversationType = N'listing',
        LastMessageAt = DATEADD(minute, -25, SYSUTCDATETIME())
    WHERE Id = @ConversationId;
END
ELSE
BEGIN
    INSERT INTO dbo.Conversations (Id, ListingId, ConversationType, LastMessageAt, CreatedAt)
    VALUES (@ConversationId, @RetroListingId, N'listing', DATEADD(minute, -25, SYSUTCDATETIME()), DATEADD(day, -1, SYSUTCDATETIME()));
END;

IF NOT EXISTS (SELECT 1 FROM dbo.ConversationParticipants WHERE ConversationId = @ConversationId AND UserId = @BatuUserId)
    INSERT INTO dbo.ConversationParticipants (ConversationId, UserId, IsMuted, LastReadAt, JoinedAt)
    VALUES (@ConversationId, @BatuUserId, 0, NULL, DATEADD(day, -1, SYSUTCDATETIME()));

IF NOT EXISTS (SELECT 1 FROM dbo.ConversationParticipants WHERE ConversationId = @ConversationId AND UserId = @AyseUserId)
    INSERT INTO dbo.ConversationParticipants (ConversationId, UserId, IsMuted, LastReadAt, JoinedAt)
    VALUES (@ConversationId, @AyseUserId, 0, DATEADD(minute, -20, SYSUTCDATETIME()), DATEADD(day, -1, SYSUTCDATETIME()));

IF EXISTS (SELECT 1 FROM dbo.Messages WHERE Id = @MessageOneId)
BEGIN
    UPDATE dbo.Messages
    SET ConversationId = @ConversationId,
        SenderUserId = @AyseUserId,
        MessageText = N'Kamera hazir, bugun kargoya verebilirim.',
        CreatedAt = DATEADD(minute, -40, SYSUTCDATETIME())
    WHERE Id = @MessageOneId;
END
ELSE
BEGIN
    INSERT INTO dbo.Messages (Id, ConversationId, SenderUserId, MessageType, MessageText, CreatedAt)
    VALUES (@MessageOneId, @ConversationId, @AyseUserId, N'text', N'Kamera hazir, bugun kargoya verebilirim.', DATEADD(minute, -40, SYSUTCDATETIME()));
END;

IF EXISTS (SELECT 1 FROM dbo.Messages WHERE Id = @MessageTwoId)
BEGIN
    UPDATE dbo.Messages
    SET ConversationId = @ConversationId,
        SenderUserId = @BatuUserId,
        MessageText = N'Harika, odeme sonrasi teslimati hizlandiralim.',
        CreatedAt = DATEADD(minute, -25, SYSUTCDATETIME())
    WHERE Id = @MessageTwoId;
END
ELSE
BEGIN
    INSERT INTO dbo.Messages (Id, ConversationId, SenderUserId, MessageType, MessageText, CreatedAt)
    VALUES (@MessageTwoId, @ConversationId, @BatuUserId, N'text', N'Harika, odeme sonrasi teslimati hizlandiralim.', DATEADD(minute, -25, SYSUTCDATETIME()));
END;

IF EXISTS (SELECT 1 FROM dbo.Notifications WHERE Id = @NotificationOneId)
BEGIN
    UPDATE dbo.Notifications
    SET UserId = @BatuUserId,
        NotificationType = N'message.received',
        Title = N'Yeni mesaj geldi',
        Body = N'Ayse: Kamera hazir, bugun kargoya verebilirim.',
        RelatedEntityType = N'conversation',
        RelatedEntityId = @ConversationId,
        IsRead = 0,
        ReadAt = NULL,
        CreatedAt = DATEADD(minute, -20, SYSUTCDATETIME())
    WHERE Id = @NotificationOneId;
END
ELSE
BEGIN
    INSERT INTO dbo.Notifications (Id, UserId, NotificationType, Title, Body, RelatedEntityType, RelatedEntityId, IsRead, CreatedAt)
    VALUES (@NotificationOneId, @BatuUserId, N'message.received', N'Yeni mesaj geldi', N'Ayse: Kamera hazir, bugun kargoya verebilirim.', N'conversation', @ConversationId, 0, DATEADD(minute, -20, SYSUTCDATETIME()));
END;

IF EXISTS (SELECT 1 FROM dbo.Notifications WHERE Id = @NotificationTwoId)
BEGIN
    UPDATE dbo.Notifications
    SET UserId = @BatuUserId,
        NotificationType = N'listing.featured',
        Title = N'One cikan ilan paketi aktif',
        Body = N'One Cikan Ilan odemeniz basariyla tamamlandi.',
        RelatedEntityType = N'listing',
        RelatedEntityId = @BatuOwnedListingId,
        IsRead = 1,
        ReadAt = DATEADD(hour, -5, SYSUTCDATETIME()),
        CreatedAt = DATEADD(hour, -6, SYSUTCDATETIME())
    WHERE Id = @NotificationTwoId;
END
ELSE
BEGIN
    INSERT INTO dbo.Notifications (Id, UserId, NotificationType, Title, Body, RelatedEntityType, RelatedEntityId, IsRead, ReadAt, CreatedAt)
    VALUES (@NotificationTwoId, @BatuUserId, N'listing.featured', N'One cikan ilan paketi aktif', N'One Cikan Ilan odemeniz basariyla tamamlandi.', N'listing', @BatuOwnedListingId, 1, DATEADD(hour, -5, SYSUTCDATETIME()), DATEADD(hour, -6, SYSUTCDATETIME()));
END;

IF EXISTS (SELECT 1 FROM dbo.Payments WHERE Id = @PaymentId)
BEGIN
    UPDATE dbo.Payments
    SET UserId = @BatuUserId,
        PackageId = @PackageId,
        ListingId = @BatuOwnedListingId,
        PaymentType = N'featured_fee',
        Amount = 149.90,
        CurrencyCode = N'TRY',
        PaymentStatus = N'paid',
        ProviderName = N'demo',
        ProviderTransactionId = N'demo-featured-payment',
        PaidAt = DATEADD(hour, -6, SYSUTCDATETIME()),
        CreatedAt = DATEADD(hour, -6, SYSUTCDATETIME())
    WHERE Id = @PaymentId;
END
ELSE
BEGIN
    INSERT INTO dbo.Payments (Id, UserId, PackageId, ListingId, PaymentType, Amount, CurrencyCode, PaymentStatus, ProviderName, ProviderTransactionId, PaidAt, CreatedAt)
    VALUES (@PaymentId, @BatuUserId, @PackageId, @BatuOwnedListingId, N'featured_fee', 149.90, N'TRY', N'paid', N'demo', N'demo-featured-payment', DATEADD(hour, -6, SYSUTCDATETIME()), DATEADD(hour, -6, SYSUTCDATETIME()));
END;

IF EXISTS (SELECT 1 FROM dbo.Complaints WHERE Id = @ComplaintId)
BEGIN
    UPDATE dbo.Complaints
    SET ReporterUserId = @BatuUserId,
        TargetEntityType = N'listing',
        TargetEntityId = @RetroListingId,
        Subject = N'Fotograf ve aciklama uyumsuz',
        Description = N'Demo moderasyon akisi icin acik bir sikayet kaydi.',
        ComplaintStatus = N'open',
        AssignedAdminUserId = NULL,
        UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @ComplaintId;
END
ELSE
BEGIN
    INSERT INTO dbo.Complaints (Id, ReporterUserId, TargetEntityType, TargetEntityId, Subject, Description, ComplaintStatus, AssignedAdminUserId, CreatedAt, UpdatedAt)
    VALUES (@ComplaintId, @BatuUserId, N'listing', @RetroListingId, N'Fotograf ve aciklama uyumsuz', N'Demo moderasyon akisi icin acik bir sikayet kaydi.', N'open', NULL, DATEADD(hour, -2, SYSUTCDATETIME()), SYSUTCDATETIME());
END;

IF OBJECT_ID(N'dbo.CustomerOrders', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.CustomerOrders WHERE Id = @OrderOneId)
    BEGIN
        UPDATE dbo.CustomerOrders
        SET UserId = @BatuUserId,
            OrderNumber = N'TS0906263',
            OrderStatus = N'delivered',
            PaymentMethod = N'Kredi Karti',
            InstallmentCount = 3,
            TotalAmount = 25000,
            CurrencyCode = N'TRY',
            ItemCount = 1,
            SummaryText = N'Retro Kamera siparisi tamamlandi.',
            OrderedAt = DATEADD(day, -14, SYSUTCDATETIME()),
            UpdatedAt = SYSUTCDATETIME()
        WHERE Id = @OrderOneId;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.CustomerOrders (Id, UserId, OrderNumber, OrderStatus, PaymentMethod, InstallmentCount, TotalAmount, CurrencyCode, ItemCount, SummaryText, OrderedAt, CreatedAt, UpdatedAt)
        VALUES (@OrderOneId, @BatuUserId, N'TS0906263', N'delivered', N'Kredi Karti', 3, 25000, N'TRY', 1, N'Retro Kamera siparisi tamamlandi.', DATEADD(day, -14, SYSUTCDATETIME()), DATEADD(day, -14, SYSUTCDATETIME()), SYSUTCDATETIME());
    END;

    IF EXISTS (SELECT 1 FROM dbo.CustomerOrders WHERE Id = @OrderTwoId)
    BEGIN
        UPDATE dbo.CustomerOrders
        SET UserId = @BatuUserId,
            OrderNumber = N'TS0206261',
            OrderStatus = N'processing',
            PaymentMethod = N'Havale',
            InstallmentCount = 1,
            TotalAmount = 66733,
            CurrencyCode = N'TRY',
            ItemCount = 1,
            SummaryText = N'Vintage Pikap siparisi isleme alindi.',
            OrderedAt = DATEADD(day, -7, SYSUTCDATETIME()),
            UpdatedAt = SYSUTCDATETIME()
        WHERE Id = @OrderTwoId;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.CustomerOrders (Id, UserId, OrderNumber, OrderStatus, PaymentMethod, InstallmentCount, TotalAmount, CurrencyCode, ItemCount, SummaryText, OrderedAt, CreatedAt, UpdatedAt)
        VALUES (@OrderTwoId, @BatuUserId, N'TS0206261', N'processing', N'Havale', 1, 66733, N'TRY', 1, N'Vintage Pikap siparisi isleme alindi.', DATEADD(day, -7, SYSUTCDATETIME()), DATEADD(day, -7, SYSUTCDATETIME()), SYSUTCDATETIME());
    END;
END;

IF OBJECT_ID(N'dbo.AccountLedgerEntries', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.AccountLedgerEntries WHERE Id = @LedgerOneId)
    BEGIN
        UPDATE dbo.AccountLedgerEntries
        SET UserId = @BatuUserId,
            RelatedOrderId = @OrderOneId,
            RelatedPaymentId = NULL,
            OrderNumber = N'TS0906263',
            EntryType = N'debit',
            Description = N'Retro Kamera siparis borcu',
            PaymentMethod = N'Kredi Karti',
            EntryDate = DATEADD(day, -14, SYSUTCDATETIME()),
            DebitAmount = 25000,
            CreditAmount = 0,
            BalanceAfter = 25000
        WHERE Id = @LedgerOneId;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.AccountLedgerEntries (Id, UserId, RelatedOrderId, RelatedPaymentId, OrderNumber, EntryType, Description, PaymentMethod, EntryDate, DebitAmount, CreditAmount, BalanceAfter, CreatedAt)
        VALUES (@LedgerOneId, @BatuUserId, @OrderOneId, NULL, N'TS0906263', N'debit', N'Retro Kamera siparis borcu', N'Kredi Karti', DATEADD(day, -14, SYSUTCDATETIME()), 25000, 0, 25000, DATEADD(day, -14, SYSUTCDATETIME()));
    END;

    IF EXISTS (SELECT 1 FROM dbo.AccountLedgerEntries WHERE Id = @LedgerTwoId)
    BEGIN
        UPDATE dbo.AccountLedgerEntries
        SET UserId = @BatuUserId,
            RelatedOrderId = @OrderOneId,
            RelatedPaymentId = @PaymentId,
            OrderNumber = N'TS0906263',
            EntryType = N'credit',
            Description = N'Kart odemesi alindi',
            PaymentMethod = N'Kredi Karti',
            EntryDate = DATEADD(day, -13, SYSUTCDATETIME()),
            DebitAmount = 0,
            CreditAmount = 149.90,
            BalanceAfter = 24850.10
        WHERE Id = @LedgerTwoId;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.AccountLedgerEntries (Id, UserId, RelatedOrderId, RelatedPaymentId, OrderNumber, EntryType, Description, PaymentMethod, EntryDate, DebitAmount, CreditAmount, BalanceAfter, CreatedAt)
        VALUES (@LedgerTwoId, @BatuUserId, @OrderOneId, @PaymentId, N'TS0906263', N'credit', N'Kart odemesi alindi', N'Kredi Karti', DATEADD(day, -13, SYSUTCDATETIME()), 0, 149.90, 24850.10, DATEADD(day, -13, SYSUTCDATETIME()));
    END;

    IF EXISTS (SELECT 1 FROM dbo.AccountLedgerEntries WHERE Id = @LedgerThreeId)
    BEGIN
        UPDATE dbo.AccountLedgerEntries
        SET UserId = @BatuUserId,
            RelatedOrderId = @OrderTwoId,
            RelatedPaymentId = NULL,
            OrderNumber = N'TS0206261',
            EntryType = N'debit',
            Description = N'Vintage Pikap siparis borcu',
            PaymentMethod = N'Havale',
            EntryDate = DATEADD(day, -7, SYSUTCDATETIME()),
            DebitAmount = 66733,
            CreditAmount = 0,
            BalanceAfter = 91583.10
        WHERE Id = @LedgerThreeId;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.AccountLedgerEntries (Id, UserId, RelatedOrderId, RelatedPaymentId, OrderNumber, EntryType, Description, PaymentMethod, EntryDate, DebitAmount, CreditAmount, BalanceAfter, CreatedAt)
        VALUES (@LedgerThreeId, @BatuUserId, @OrderTwoId, NULL, N'TS0206261', N'debit', N'Vintage Pikap siparis borcu', N'Havale', DATEADD(day, -7, SYSUTCDATETIME()), 66733, 0, 91583.10, DATEADD(day, -7, SYSUTCDATETIME()));
    END;
END;

IF OBJECT_ID(N'dbo.Favorites', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.Favorites WHERE UserId = @BatuUserId AND ListingId = @RetroListingId)
        INSERT INTO dbo.Favorites (UserId, ListingId, CreatedAt)
        VALUES (@BatuUserId, @RetroListingId, DATEADD(day, -10, SYSUTCDATETIME()));
END;

IF OBJECT_ID(N'dbo.StockAlerts', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.StockAlerts WHERE Id = @StockAlertId)
    BEGIN
        UPDATE dbo.StockAlerts
        SET UserId = @BatuUserId,
            ListingId = @PikapListingId,
            Note = N'Stok acilinca haber verin',
            IsActive = 1
        WHERE Id = @StockAlertId;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.StockAlerts (Id, UserId, ListingId, Note, IsActive, CreatedAt)
        VALUES (@StockAlertId, @BatuUserId, @PikapListingId, N'Stok acilinca haber verin', 1, DATEADD(day, -3, SYSUTCDATETIME()));
    END;
END;

IF OBJECT_ID(N'dbo.PriceAlerts', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.PriceAlerts WHERE Id = @PriceAlertId)
    BEGIN
        UPDATE dbo.PriceAlerts
        SET UserId = @BatuUserId,
            ListingId = @RetroListingId,
            TargetPrice = 3000,
            CurrentPriceSnapshot = 3250,
            CurrencyCode = N'TRY',
            IsActive = 1
        WHERE Id = @PriceAlertId;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.PriceAlerts (Id, UserId, ListingId, TargetPrice, CurrentPriceSnapshot, CurrencyCode, IsActive, CreatedAt)
        VALUES (@PriceAlertId, @BatuUserId, @RetroListingId, 3000, 3250, N'TRY', 1, DATEADD(day, -2, SYSUTCDATETIME()));
    END;
END;

COMMIT TRANSACTION;
"@

$connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
try {
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandTimeout = 60
    $command.CommandText = $sql
    [void]$command.ExecuteNonQuery()
}
finally {
    if ($connection.State -ne [System.Data.ConnectionState]::Closed) {
        $connection.Close()
    }
}

Write-Host "Demo data seeded successfully." -ForegroundColor Green
Write-Host "Web user: batu@example.com / Password123!" -ForegroundColor Yellow
Write-Host "Seller user: ayse@example.com / Password123!" -ForegroundColor Yellow
Write-Host "Admin user: admin@example.com / Password123!" -ForegroundColor Yellow
