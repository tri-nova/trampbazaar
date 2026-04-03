using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using trampbazaar.Shared.Contracts;

namespace trampbazaar.Api.Services;

public sealed class MarketplaceRepository
{
    private readonly string connectionString;
    private readonly PaymentGatewayRouter paymentGatewayRouter;

    public MarketplaceRepository(IConfiguration configuration, PaymentGatewayRouter paymentGatewayRouter)
    {
        connectionString = configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("SqlServer connection string bulunamadi.");
        this.paymentGatewayRouter = paymentGatewayRouter;
    }

    public async Task<DashboardResponse> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                (SELECT COUNT(*) FROM dbo.Listings) AS ListingCount,
                (SELECT COUNT(*) FROM dbo.Categories WHERE IsActive = 1) AS CategoryCount,
                (SELECT COUNT(*) FROM dbo.SaleModes WHERE IsActive = 1) AS SaleModeCount,
                (SELECT COUNT(*) FROM dbo.Users WHERE Status = N'active') AS UserCount;
            """;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var listingCount = 0;
        var categoryCount = 0;
        var saleModeCount = 0;
        var userCount = 0;

        if (await reader.ReadAsync(cancellationToken))
        {
            listingCount = reader.GetInt32(0);
            categoryCount = reader.GetInt32(1);
            saleModeCount = reader.GetInt32(2);
            userCount = reader.GetInt32(3);
        }

        return new DashboardResponse
        {
            PlatformName = "TrampBazaar",
            QuickStats =
            [
                new QuickStatDto { Label = "Aktif ilan", Value = listingCount.ToString() },
                new QuickStatDto { Label = "Kategori", Value = categoryCount.ToString() },
                new QuickStatDto { Label = "Satis modu", Value = saleModeCount.ToString() },
                new QuickStatDto { Label = "Aktif kullanici", Value = userCount.ToString() }
            ],
            SaleModes = await GetSaleModesAsync(cancellationToken),
            Features =
            [
                new FeatureDto { Title = "SQL Server", Description = "Kalici veri tabani altyapisi." },
                new FeatureDto { Title = "Auth", Description = "Register ve login SQL uzerinden calisiyor." },
                new FeatureDto { Title = "Listings", Description = "Ilanlar veritabanindan listeleniyor." },
                new FeatureDto { Title = "Create Listing", Description = "Yeni ilan SQL'e kaydediliyor." }
            ]
        };
    }

    public async Task<AdminOverviewDto> GetAdminOverviewAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                (SELECT COUNT(*) FROM dbo.Users WHERE Status = N'active') AS ActiveUsers,
                (SELECT COUNT(*) FROM dbo.Listings WHERE ListingStatus = N'published') AS PublishedListings,
                (SELECT COUNT(*) FROM dbo.Conversations) AS OpenConversations,
                (SELECT COUNT(*) FROM dbo.Notifications WHERE IsRead = 0) AS UnreadNotifications;
            """;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var overview = new AdminOverviewDto();
        if (await reader.ReadAsync(cancellationToken))
        {
            overview.ActiveUsers = reader.GetInt32(0);
            overview.PublishedListings = reader.GetInt32(1);
            overview.OpenConversations = reader.GetInt32(2);
            overview.UnreadNotifications = reader.GetInt32(3);
        }

        overview.Highlights =
        [
            new QuickStatDto { Label = "Aktif kullanici", Value = overview.ActiveUsers.ToString() },
            new QuickStatDto { Label = "Yayindaki ilan", Value = overview.PublishedListings.ToString() },
            new QuickStatDto { Label = "Acik konusma", Value = overview.OpenConversations.ToString() },
            new QuickStatDto { Label = "Okunmamis bildirim", Value = overview.UnreadNotifications.ToString() }
        ];

        return overview;
    }

    public async Task<IReadOnlyList<AdminUserDto>> GetAdminUsersAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                u.Id,
                ISNULL(up.UserName, u.Email) AS UserName,
                u.Email,
                u.AccountType,
                u.Status,
                (
                    SELECT COUNT(*)
                    FROM dbo.Listings l
                    WHERE l.SellerUserId = u.Id
                ) AS ListingCount,
                u.CreatedAt
            FROM dbo.Users u
            LEFT JOIN dbo.UserProfiles up ON up.UserId = u.Id
            ORDER BY u.CreatedAt DESC;
            """;

        var users = new List<AdminUserDto>();
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            users.Add(new AdminUserDto
            {
                Id = reader.GetGuid(0),
                UserName = reader.GetString(1),
                Email = reader.GetString(2),
                AccountType = reader.GetString(3),
                Status = reader.GetString(4),
                ListingCount = reader.GetInt32(5),
                CreatedAt = new DateTimeOffset(reader.GetDateTime(6), TimeSpan.Zero)
            });
        }

        return users;
    }

    public async Task<IReadOnlyList<AdminListingDto>> GetAdminListingsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                l.Id,
                p.Title,
                ISNULL(up.UserName, u.Email) AS SellerUserName,
                c.Name AS CategoryName,
                sm.Name AS SaleModeName,
                l.Price,
                l.CurrencyCode,
                l.ListingStatus,
                l.IsFeatured,
                l.ViewCount,
                l.CreatedAt
            FROM dbo.Listings l
            INNER JOIN dbo.Products p ON p.Id = l.ProductId
            INNER JOIN dbo.Users u ON u.Id = l.SellerUserId
            LEFT JOIN dbo.UserProfiles up ON up.UserId = u.Id
            INNER JOIN dbo.Categories c ON c.Id = p.CategoryId
            INNER JOIN dbo.SaleModes sm ON sm.Id = l.SaleModeId
            ORDER BY l.CreatedAt DESC;
            """;

        var listings = new List<AdminListingDto>();
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            listings.Add(new AdminListingDto
            {
                Id = reader.GetGuid(0),
                Title = reader.GetString(1),
                SellerUserName = reader.GetString(2),
                Category = reader.GetString(3),
                SaleMode = reader.GetString(4),
                Price = reader.GetDecimal(5),
                Currency = reader.GetString(6),
                Status = reader.GetString(7),
                IsFeatured = reader.GetBoolean(8),
                ViewCount = reader.GetInt32(9),
                CreatedAt = new DateTimeOffset(reader.GetDateTime(10), TimeSpan.Zero)
            });
        }

        return listings;
    }

    public async Task<IReadOnlyList<AdminCategoryDto>> GetAdminCategoriesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                c.Id,
                c.ParentCategoryId,
                parent.Name AS ParentCategoryName,
                c.Name,
                c.Slug,
                c.SortOrder,
                c.IsActive,
                c.CreatedAt
            FROM dbo.Categories c
            LEFT JOIN dbo.Categories parent ON parent.Id = c.ParentCategoryId
            ORDER BY c.SortOrder, c.Name;
            """;

        var categories = new List<AdminCategoryDto>();
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            categories.Add(new AdminCategoryDto
            {
                Id = reader.GetGuid(0),
                ParentCategoryId = reader.IsDBNull(1) ? null : reader.GetGuid(1),
                ParentCategoryName = reader.IsDBNull(2) ? null : reader.GetString(2),
                Name = reader.GetString(3),
                Slug = reader.GetString(4),
                SortOrder = reader.GetInt32(5),
                IsActive = reader.GetBoolean(6),
                CreatedAt = new DateTimeOffset(reader.GetDateTime(7), TimeSpan.Zero)
            });
        }

        return categories;
    }

    public async Task<AdminCategoryDto> AddAdminCategoryAsync(AdminCategoryUpsertRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var categoryId = Guid.NewGuid();
        const string sql = """
            INSERT INTO dbo.Categories
            (
                Id, ParentCategoryId, Name, Slug, SortOrder, IsActive, CreatedAt
            )
            VALUES
            (
                @Id, @ParentCategoryId, @Name, @Slug, @SortOrder, 1, SYSUTCDATETIME()
            );
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", categoryId);
        command.Parameters.AddWithValue("@ParentCategoryId", request.ParentCategoryId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Name", request.Name.Trim());
        command.Parameters.AddWithValue("@Slug", request.Slug.Trim());
        command.Parameters.AddWithValue("@SortOrder", request.SortOrder);
        await command.ExecuteNonQueryAsync(cancellationToken);

        var created = (await GetAdminCategoriesAsync(cancellationToken)).FirstOrDefault(x => x.Id == categoryId);
        return created ?? throw new InvalidOperationException("Kategori kaydedildi ancak okunamadi.");
    }

    public async Task<bool> UpdateAdminCategoryAsync(Guid categoryId, AdminCategoryUpsertRequest request, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Categories
            SET ParentCategoryId = @ParentCategoryId,
                Name = @Name,
                Slug = @Slug,
                SortOrder = @SortOrder
            WHERE Id = @CategoryId;
            """;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@CategoryId", categoryId);
        command.Parameters.AddWithValue("@ParentCategoryId", request.ParentCategoryId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Name", request.Name.Trim());
        command.Parameters.AddWithValue("@Slug", request.Slug.Trim());
        command.Parameters.AddWithValue("@SortOrder", request.SortOrder);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> UpdateAdminCategoryStatusAsync(Guid categoryId, bool isActive, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Categories
            SET IsActive = @IsActive
            WHERE Id = @CategoryId;
            """;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@CategoryId", categoryId);
        command.Parameters.AddWithValue("@IsActive", isActive);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<IReadOnlyList<AdminPackageDto>> GetAdminPackagesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                Id,
                PackageType,
                Name,
                Price,
                CurrencyCode,
                DurationDays,
                ListingQuota,
                IsActive,
                CreatedAt
            FROM dbo.Packages
            ORDER BY CreatedAt DESC;
            """;

        var packages = new List<AdminPackageDto>();
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            packages.Add(new AdminPackageDto
            {
                Id = reader.GetGuid(0),
                PackageType = reader.GetString(1),
                Name = reader.GetString(2),
                Price = reader.GetDecimal(3),
                CurrencyCode = reader.GetString(4).Trim(),
                DurationDays = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                ListingQuota = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                IsActive = reader.GetBoolean(7),
                CreatedAt = new DateTimeOffset(reader.GetDateTime(8), TimeSpan.Zero)
            });
        }

        return packages;
    }

    public async Task<AdminPackageDto> AddAdminPackageAsync(AdminPackageUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedType = NormalizePackageType(request.PackageType);
        var packageId = Guid.NewGuid();

        const string sql = """
            INSERT INTO dbo.Packages
            (
                Id, PackageType, Name, Price, CurrencyCode, DurationDays, ListingQuota, IsActive, CreatedAt
            )
            VALUES
            (
                @Id, @PackageType, @Name, @Price, N'TRY', @DurationDays, @ListingQuota, 1, SYSUTCDATETIME()
            );
            """;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", packageId);
        command.Parameters.AddWithValue("@PackageType", normalizedType);
        command.Parameters.AddWithValue("@Name", request.Name.Trim());
        command.Parameters.AddWithValue("@Price", request.Price);
        command.Parameters.AddWithValue("@DurationDays", request.DurationDays ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ListingQuota", request.ListingQuota ?? (object)DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);

        var created = (await GetAdminPackagesAsync(cancellationToken)).FirstOrDefault(x => x.Id == packageId);
        return created ?? throw new InvalidOperationException("Paket kaydedildi ancak okunamadi.");
    }

    public async Task<bool> UpdateAdminPackageAsync(Guid packageId, AdminPackageUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedType = NormalizePackageType(request.PackageType);

        const string sql = """
            UPDATE dbo.Packages
            SET PackageType = @PackageType,
                Name = @Name,
                Price = @Price,
                DurationDays = @DurationDays,
                ListingQuota = @ListingQuota
            WHERE Id = @PackageId;
            """;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@PackageId", packageId);
        command.Parameters.AddWithValue("@PackageType", normalizedType);
        command.Parameters.AddWithValue("@Name", request.Name.Trim());
        command.Parameters.AddWithValue("@Price", request.Price);
        command.Parameters.AddWithValue("@DurationDays", request.DurationDays ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ListingQuota", request.ListingQuota ?? (object)DBNull.Value);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> UpdateAdminPackageStatusAsync(Guid packageId, bool isActive, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Packages
            SET IsActive = @IsActive
            WHERE Id = @PackageId;
            """;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@PackageId", packageId);
        command.Parameters.AddWithValue("@IsActive", isActive);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<AdminPaymentsDashboardDto> GetAdminPaymentsAsync(CancellationToken cancellationToken = default)
    {
        const string summarySql = """
            SELECT
                ISNULL(SUM(CASE WHEN PaymentStatus = N'paid' THEN Amount ELSE 0 END), 0) AS TotalPaidAmount,
                SUM(CASE WHEN PaymentStatus = N'paid' THEN 1 ELSE 0 END) AS PaidCount,
                SUM(CASE WHEN PaymentStatus = N'pending' THEN 1 ELSE 0 END) AS PendingCount
            FROM dbo.Payments;
            """;

        const string paymentsSql = """
            SELECT TOP 200
                p.Id,
                ISNULL(up.UserName, u.Email) AS UserName,
                p.PaymentType,
                p.PaymentStatus,
                p.Amount,
                p.CurrencyCode,
                ISNULL(pkg.Name, N'') AS PackageName,
                ISNULL(prod.Title, N'') AS ListingTitle,
                ISNULL(p.ProviderName, N'') AS ProviderName,
                p.PaidAt,
                p.CreatedAt
            FROM dbo.Payments p
            INNER JOIN dbo.Users u ON u.Id = p.UserId
            LEFT JOIN dbo.UserProfiles up ON up.UserId = u.Id
            LEFT JOIN dbo.Packages pkg ON pkg.Id = p.PackageId
            LEFT JOIN dbo.Listings l ON l.Id = p.ListingId
            LEFT JOIN dbo.Products prod ON prod.Id = l.ProductId
            ORDER BY p.CreatedAt DESC;
            """;

        var dashboard = new AdminPaymentsDashboardDto();
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using (var summaryCommand = new SqlCommand(summarySql, connection))
        await using (var reader = await summaryCommand.ExecuteReaderAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                dashboard.TotalPaidAmount = reader.GetDecimal(0);
                dashboard.PaidCount = reader.GetInt32(1);
                dashboard.PendingCount = reader.GetInt32(2);
            }
        }

        var payments = new List<AdminPaymentDto>();
        await using (var paymentsCommand = new SqlCommand(paymentsSql, connection))
        await using (var reader = await paymentsCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                payments.Add(new AdminPaymentDto
                {
                    Id = reader.GetGuid(0),
                    UserName = reader.GetString(1),
                    PaymentType = reader.GetString(2),
                    PaymentStatus = reader.GetString(3),
                    Amount = reader.GetDecimal(4),
                    CurrencyCode = reader.GetString(5).Trim(),
                    PackageName = reader.GetString(6),
                    ListingTitle = reader.GetString(7),
                    ProviderName = reader.GetString(8),
                    PaidAt = reader.IsDBNull(9) ? null : new DateTimeOffset(reader.GetDateTime(9), TimeSpan.Zero),
                    CreatedAt = new DateTimeOffset(reader.GetDateTime(10), TimeSpan.Zero)
                });
            }
        }

        dashboard.Payments = payments;
        return dashboard;
    }

    public async Task<IReadOnlyList<AdminComplaintDto>> GetAdminComplaintsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                c.Id,
                ISNULL(reporterUp.UserName, reporter.Email) AS ReporterUserName,
                c.TargetEntityType,
                c.TargetEntityId,
                c.Subject,
                c.Description,
                c.ComplaintStatus,
                ISNULL(adminUp.UserName, adminUser.Email) AS AssignedAdminUserName,
                c.CreatedAt,
                c.UpdatedAt
            FROM dbo.Complaints c
            INNER JOIN dbo.Users reporter ON reporter.Id = c.ReporterUserId
            LEFT JOIN dbo.UserProfiles reporterUp ON reporterUp.UserId = reporter.Id
            LEFT JOIN dbo.Users adminUser ON adminUser.Id = c.AssignedAdminUserId
            LEFT JOIN dbo.UserProfiles adminUp ON adminUp.UserId = adminUser.Id
            ORDER BY c.CreatedAt DESC;
            """;

        var complaints = new List<AdminComplaintDto>();
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            complaints.Add(new AdminComplaintDto
            {
                Id = reader.GetGuid(0),
                ReporterUserName = reader.GetString(1),
                TargetEntityType = reader.GetString(2),
                TargetEntityId = reader.GetGuid(3),
                Subject = reader.GetString(4),
                Description = reader.GetString(5),
                ComplaintStatus = reader.GetString(6),
                AssignedAdminUserName = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                CreatedAt = new DateTimeOffset(reader.GetDateTime(8), TimeSpan.Zero),
                UpdatedAt = new DateTimeOffset(reader.GetDateTime(9), TimeSpan.Zero)
            });
        }

        return complaints;
    }

    public async Task<bool> UpdateAdminComplaintStatusAsync(Guid complaintId, AdminComplaintStatusUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedStatus = NormalizeComplaintStatus(request.Status);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        Guid? assignedAdminUserId = null;
        if (!string.IsNullOrWhiteSpace(request.AssignedAdminUserName))
        {
            await using var resolveCommand = new SqlCommand("""
                SELECT TOP 1 u.Id
                FROM dbo.Users u
                LEFT JOIN dbo.UserProfiles up ON up.UserId = u.Id
                WHERE up.UserName = @UserName OR u.Email = @UserName;
                """, connection);
            resolveCommand.Parameters.AddWithValue("@UserName", request.AssignedAdminUserName.Trim());
            var result = await resolveCommand.ExecuteScalarAsync(cancellationToken);
            assignedAdminUserId = result is Guid id ? id : null;
        }

        const string sql = """
            UPDATE dbo.Complaints
            SET ComplaintStatus = @ComplaintStatus,
                AssignedAdminUserId = @AssignedAdminUserId,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @ComplaintId;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ComplaintId", complaintId);
        command.Parameters.AddWithValue("@ComplaintStatus", normalizedStatus);
        command.Parameters.AddWithValue("@AssignedAdminUserId", assignedAdminUserId ?? (object)DBNull.Value);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> UpdateAdminUserStatusAsync(Guid userId, string status, CancellationToken cancellationToken = default)
    {
        var normalizedStatus = string.Equals(status, "active", StringComparison.OrdinalIgnoreCase)
            ? "active"
            : "passive";

        const string sql = """
            UPDATE dbo.Users
            SET Status = @Status,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @UserId;
            """;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@Status", normalizedStatus);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> UpdateAdminListingStatusAsync(Guid listingId, string status, CancellationToken cancellationToken = default)
    {
        var normalizedStatus = string.Equals(status, "published", StringComparison.OrdinalIgnoreCase)
            ? "published"
            : "paused";

        const string sql = """
            UPDATE dbo.Listings
            SET ListingStatus = @Status,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @ListingId;
            """;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ListingId", listingId);
        command.Parameters.AddWithValue("@Status", normalizedStatus);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<IReadOnlyList<ConversationSummaryDto>> GetAdminConversationsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                c.Id,
                c.ListingId,
                c.ConversationType,
                ISNULL(p.Title, N'Dogrudan Mesaj') AS Title,
                STRING_AGG(ISNULL(up.UserName, u.Email), N', ') WITHIN GROUP (ORDER BY ISNULL(up.UserName, u.Email)) AS Participants,
                ISNULL(lastMessage.MessageText, N'') AS LastMessagePreview,
                c.LastMessageAt
            FROM dbo.Conversations c
            LEFT JOIN dbo.Listings l ON l.Id = c.ListingId
            LEFT JOIN dbo.Products p ON p.Id = l.ProductId
            INNER JOIN dbo.ConversationParticipants cp ON cp.ConversationId = c.Id
            INNER JOIN dbo.Users u ON u.Id = cp.UserId
            LEFT JOIN dbo.UserProfiles up ON up.UserId = u.Id
            OUTER APPLY
            (
                SELECT TOP 1 m.MessageText
                FROM dbo.Messages m
                WHERE m.ConversationId = c.Id
                ORDER BY m.CreatedAt DESC
            ) lastMessage
            GROUP BY c.Id, c.ListingId, c.ConversationType, p.Title, lastMessage.MessageText, c.LastMessageAt, c.CreatedAt
            ORDER BY ISNULL(c.LastMessageAt, c.CreatedAt) DESC;
            """;

        var conversations = new List<ConversationSummaryDto>();
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            conversations.Add(new ConversationSummaryDto
            {
                Id = reader.GetGuid(0),
                ListingId = reader.IsDBNull(1) ? null : reader.GetGuid(1),
                ConversationType = reader.GetString(2),
                Title = reader.GetString(3),
                CounterpartyUserName = reader.GetString(4),
                LastMessagePreview = reader.GetString(5),
                LastMessageAt = reader.IsDBNull(6) ? null : new DateTimeOffset(reader.GetDateTime(6), TimeSpan.Zero),
                UnreadCount = 0
            });
        }

        return conversations;
    }

    public async Task<IReadOnlyList<NotificationDto>> GetAdminNotificationsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP 200
                n.Id,
                n.NotificationType,
                n.Title,
                n.Body,
                n.RelatedEntityType,
                n.RelatedEntityId,
                n.IsRead,
                n.CreatedAt
            FROM dbo.Notifications n
            ORDER BY n.CreatedAt DESC;
            """;

        var notifications = new List<NotificationDto>();
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            notifications.Add(new NotificationDto
            {
                Id = reader.GetGuid(0),
                NotificationType = reader.GetString(1),
                Title = reader.GetString(2),
                Body = reader.GetString(3),
                RelatedEntityType = reader.IsDBNull(4) ? null : reader.GetString(4),
                RelatedEntityId = reader.IsDBNull(5) ? null : reader.GetGuid(5),
                IsRead = reader.GetBoolean(6),
                CreatedAt = new DateTimeOffset(reader.GetDateTime(7), TimeSpan.Zero)
            });
        }

        return notifications;
    }

    public async Task<PaymentResultDto> CreatePaymentAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        var gateway = paymentGatewayRouter.Resolve();
        var successUrl = paymentGatewayRouter.GetSuccessUrl(request.SuccessUrl);
        var cancelUrl = paymentGatewayRouter.GetCancelUrl(request.CancelUrl);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var userId = await ResolveSellerUserIdAsync(connection, transaction, request.UserName, cancellationToken);
            if (userId == Guid.Empty)
            {
                throw new InvalidOperationException("Kullanici bulunamadi.");
            }

            const string packageSql = """
                SELECT TOP 1 PackageType, Name, Price, CurrencyCode
                FROM dbo.Packages
                WHERE Id = @PackageId AND IsActive = 1;
                """;

            string packageType;
            string packageName;
            decimal amount;
            string currencyCode;

            await using (var packageCommand = new SqlCommand(packageSql, connection, transaction))
            {
                packageCommand.Parameters.AddWithValue("@PackageId", request.PackageId);
                await using var reader = await packageCommand.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                {
                    throw new InvalidOperationException("Paket bulunamadi.");
                }

                packageType = reader.GetString(0);
                packageName = reader.GetString(1);
                amount = reader.GetDecimal(2);
                currencyCode = reader.GetString(3).Trim();
            }

            var paymentId = Guid.NewGuid();
            var paymentType = MapPackageTypeToPaymentType(packageType);
            var providerName = gateway.ProviderName;
            var providerTransactionId = string.Empty;
            var checkoutUrl = string.Empty;
            var paymentStatus = "paid";
            DateTimeOffset? paidAt = DateTimeOffset.UtcNow;

            if (string.Equals(providerName, "stripe", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(successUrl) || string.IsNullOrWhiteSpace(cancelUrl))
                {
                    throw new InvalidOperationException("Odeme donus URL ayarlari eksik.");
                }

                var session = await gateway.CreatePackageCheckoutAsync(new PaymentGatewayCheckoutRequest
                {
                    PaymentId = paymentId,
                    PackageId = request.PackageId,
                    UserName = request.UserName,
                    PackageName = packageName,
                    PaymentType = paymentType,
                    Amount = amount,
                    CurrencyCode = currencyCode,
                    SuccessUrl = successUrl,
                    CancelUrl = cancelUrl
                }, cancellationToken);

                providerTransactionId = session.ProviderTransactionId;
                checkoutUrl = session.CheckoutUrl;
                paymentStatus = "pending";
                paidAt = null;
            }
            else
            {
                providerTransactionId = paymentId.ToString("N");
            }

            const string insertSql = """
                INSERT INTO dbo.Payments
                (
                    Id, UserId, PackageId, PaymentType, Amount, CurrencyCode, PaymentStatus, ProviderName, ProviderTransactionId, PaidAt, CreatedAt
                )
                VALUES
                (
                    @Id, @UserId, @PackageId, @PaymentType, @Amount, @CurrencyCode, @PaymentStatus, @ProviderName, @ProviderTransactionId, @PaidAt, SYSUTCDATETIME()
                );
                """;

            await using (var command = new SqlCommand(insertSql, connection, transaction))
            {
                command.Parameters.AddWithValue("@Id", paymentId);
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@PackageId", request.PackageId);
                command.Parameters.AddWithValue("@PaymentType", paymentType);
                command.Parameters.AddWithValue("@Amount", amount);
                command.Parameters.AddWithValue("@CurrencyCode", currencyCode);
                command.Parameters.AddWithValue("@PaymentStatus", paymentStatus);
                command.Parameters.AddWithValue("@ProviderName", providerName);
                command.Parameters.AddWithValue("@ProviderTransactionId", string.IsNullOrWhiteSpace(providerTransactionId) ? DBNull.Value : providerTransactionId);
                command.Parameters.AddWithValue("@PaidAt", paidAt.HasValue ? paidAt.Value.UtcDateTime : DBNull.Value);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            if (paymentStatus == "paid")
            {
                await InsertNotificationAsync(connection, transaction, userId, "payment.completed", "Paket satin alindi", $"{packageName} paketi hesabiniza tanimlandi.", "package", request.PackageId, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            return new PaymentResultDto
            {
                PaymentId = paymentId,
                PaymentStatus = paymentStatus,
                Amount = amount,
                CurrencyCode = currencyCode,
                ProviderName = providerName,
                CheckoutUrl = checkoutUrl,
                Message = paymentStatus == "pending"
                    ? "Odeme oturumu olusturuldu."
                    : "Paket satin alma kaydi olusturuldu."
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> MarkPaymentPaidAsync(string providerName, string providerTransactionId, DateTimeOffset paidAt, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        const string updateSql = """
            UPDATE dbo.Payments
            SET PaymentStatus = N'paid',
                PaidAt = @PaidAt
            WHERE ProviderName = @ProviderName
              AND ProviderTransactionId = @ProviderTransactionId
              AND PaymentStatus <> N'paid';
            """;

        const string paymentInfoSql = """
            SELECT TOP 1 p.UserId, p.PackageId, ISNULL(pkg.Name, N'Paket') AS PackageName
            FROM dbo.Payments p
            LEFT JOIN dbo.Packages pkg ON pkg.Id = p.PackageId
            WHERE p.ProviderName = @ProviderName
              AND p.ProviderTransactionId = @ProviderTransactionId;
            """;

        try
        {
            await using (var updateCommand = new SqlCommand(updateSql, connection, transaction))
            {
                updateCommand.Parameters.AddWithValue("@PaidAt", paidAt.UtcDateTime);
                updateCommand.Parameters.AddWithValue("@ProviderName", providerName);
                updateCommand.Parameters.AddWithValue("@ProviderTransactionId", providerTransactionId);
                await updateCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            Guid userId = Guid.Empty;
            Guid? packageId = null;
            var packageName = "Paket";

            await using (var infoCommand = new SqlCommand(paymentInfoSql, connection, transaction))
            {
                infoCommand.Parameters.AddWithValue("@ProviderName", providerName);
                infoCommand.Parameters.AddWithValue("@ProviderTransactionId", providerTransactionId);
                await using var reader = await infoCommand.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                {
                    userId = reader.GetGuid(0);
                    packageId = reader.IsDBNull(1) ? null : reader.GetGuid(1);
                    packageName = reader.GetString(2);
                }
            }

            if (userId != Guid.Empty)
            {
                await InsertNotificationAsync(connection, transaction, userId, "payment.completed", "Paket satin alindi", $"{packageName} paketi hesabiniza tanimlandi.", "package", packageId, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> MarkPaymentFailedAsync(string providerName, string providerTransactionId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Payments
            SET PaymentStatus = N'failed'
            WHERE ProviderName = @ProviderName
              AND ProviderTransactionId = @ProviderTransactionId
              AND PaymentStatus = N'pending';
            """;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ProviderName", providerName);
        command.Parameters.AddWithValue("@ProviderTransactionId", providerTransactionId);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<ComplaintResultDto> CreateComplaintAsync(CreateComplaintRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var reporterUserId = await ResolveSellerUserIdAsync(connection, transaction, request.UserName, cancellationToken);
            if (reporterUserId == Guid.Empty)
            {
                throw new InvalidOperationException("Kullanici bulunamadi.");
            }

            var complaintId = Guid.NewGuid();
            const string insertSql = """
                INSERT INTO dbo.Complaints
                (
                    Id, ReporterUserId, TargetEntityType, TargetEntityId, Subject, Description, ComplaintStatus, CreatedAt, UpdatedAt
                )
                VALUES
                (
                    @Id, @ReporterUserId, @TargetEntityType, @TargetEntityId, @Subject, @Description, N'open', SYSUTCDATETIME(), SYSUTCDATETIME()
                );
                """;

            await using (var command = new SqlCommand(insertSql, connection, transaction))
            {
                command.Parameters.AddWithValue("@Id", complaintId);
                command.Parameters.AddWithValue("@ReporterUserId", reporterUserId);
                command.Parameters.AddWithValue("@TargetEntityType", request.TargetEntityType.Trim());
                command.Parameters.AddWithValue("@TargetEntityId", request.TargetEntityId);
                command.Parameters.AddWithValue("@Subject", request.Subject.Trim());
                command.Parameters.AddWithValue("@Description", request.Description.Trim());
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            return new ComplaintResultDto
            {
                ComplaintId = complaintId,
                ComplaintStatus = "open",
                Message = "Sikayetiniz kayda alindi."
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, Name, Slug
            FROM dbo.Categories
            WHERE IsActive = 1
            ORDER BY SortOrder, Name;
            """;

        var categories = new List<CategoryDto>();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            categories.Add(new CategoryDto
            {
                Id = reader.GetGuid(0),
                Name = reader.GetString(1),
                Slug = reader.GetString(2)
            });
        }

        return categories;
    }

    public async Task<IReadOnlyList<SaleModeDto>> GetSaleModesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT ModeKey, Name, ISNULL(Description, N'')
            FROM dbo.SaleModes
            WHERE IsActive = 1
            ORDER BY Name;
            """;

        var saleModes = new List<SaleModeDto>();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var key = reader.GetString(0);
            saleModes.Add(new SaleModeDto
            {
                Key = key,
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                Steps = GetSaleModeSteps(key)
            });
        }

        return saleModes;
    }

    public async Task<IReadOnlyList<PackageDto>> GetPackagesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, PackageType, Name, Price, CurrencyCode, DurationDays, ListingQuota
            FROM dbo.Packages
            WHERE IsActive = 1
            ORDER BY Price, Name;
            """;

        var packages = new List<PackageDto>();
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            packages.Add(new PackageDto
            {
                Id = reader.GetGuid(0),
                PackageType = reader.GetString(1),
                Name = reader.GetString(2),
                Price = reader.GetDecimal(3),
                CurrencyCode = reader.GetString(4).Trim(),
                DurationDays = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                ListingQuota = reader.IsDBNull(6) ? null : reader.GetInt32(6)
            });
        }

        return packages;
    }

    public async Task<UserAccountDashboardDto?> GetUserAccountDashboardAsync(string userName, CancellationToken cancellationToken = default)
    {
        const string summarySql = """
            SELECT TOP 1
                ISNULL(up.UserName, u.Email) AS UserName,
                u.AccountType,
                (
                    SELECT COUNT(*)
                    FROM dbo.Listings l
                    WHERE l.SellerUserId = u.Id
                ) AS ListingCount,
                (
                    SELECT COUNT(*)
                    FROM dbo.Listings l
                    WHERE l.SellerUserId = u.Id
                      AND l.ListingStatus IN (N'published', N'active')
                ) AS ActiveListingCount,
                (
                    SELECT COUNT(*)
                    FROM dbo.Notifications n
                    WHERE n.UserId = u.Id
                      AND n.IsRead = 0
                ) AS UnreadNotificationCount,
                (
                    SELECT COUNT(*)
                    FROM dbo.Payments p
                    WHERE p.UserId = u.Id
                ) AS PaymentCount,
                (
                    SELECT ISNULL(SUM(CASE WHEN p.PaymentStatus = N'paid' THEN p.Amount ELSE 0 END), 0)
                    FROM dbo.Payments p
                    WHERE p.UserId = u.Id
                ) AS TotalPaidAmount
            FROM dbo.Users u
            LEFT JOIN dbo.UserProfiles up ON up.UserId = u.Id
            WHERE up.UserName = @UserName OR u.Email = @UserName;
            """;

        const string listingsSql = """
            SELECT TOP 6
                l.Id,
                p.Title,
                p.Description,
                c.Name AS CategoryName,
                sm.ModeKey,
                l.Price,
                l.CurrencyCode,
                ISNULL(up.UserName, u.Email) AS SellerName,
                l.ListingStatus,
                l.CreatedAt
            FROM dbo.Listings l
            INNER JOIN dbo.Products p ON p.Id = l.ProductId
            INNER JOIN dbo.Categories c ON c.Id = p.CategoryId
            INNER JOIN dbo.SaleModes sm ON sm.Id = l.SaleModeId
            INNER JOIN dbo.Users u ON u.Id = l.SellerUserId
            LEFT JOIN dbo.UserProfiles up ON up.UserId = u.Id
            WHERE up.UserName = @UserName OR u.Email = @UserName
            ORDER BY l.CreatedAt DESC;
            """;

        const string paymentsSql = """
            SELECT TOP 6
                p.Id,
                p.PaymentType,
                p.PaymentStatus,
                p.Amount,
                p.CurrencyCode,
                ISNULL(pkg.Name, N'') AS PackageName,
                p.CreatedAt
            FROM dbo.Payments p
            INNER JOIN dbo.Users u ON u.Id = p.UserId
            LEFT JOIN dbo.UserProfiles up ON up.UserId = u.Id
            LEFT JOIN dbo.Packages pkg ON pkg.Id = p.PackageId
            WHERE up.UserName = @UserName OR u.Email = @UserName
            ORDER BY p.CreatedAt DESC;
            """;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        UserAccountDashboardDto? dashboard = null;

        await using (var summaryCommand = new SqlCommand(summarySql, connection))
        {
            summaryCommand.Parameters.AddWithValue("@UserName", userName.Trim());
            await using var reader = await summaryCommand.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                dashboard = new UserAccountDashboardDto
                {
                    UserName = reader.GetString(0),
                    AccountType = reader.GetString(1),
                    ListingCount = reader.GetInt32(2),
                    ActiveListingCount = reader.GetInt32(3),
                    UnreadNotificationCount = reader.GetInt32(4),
                    PaymentCount = reader.GetInt32(5),
                    TotalPaidAmount = reader.GetDecimal(6)
                };
            }
        }

        if (dashboard is null)
        {
            return null;
        }

        var listings = new List<ListingDto>();
        await using (var listingsCommand = new SqlCommand(listingsSql, connection))
        {
            listingsCommand.Parameters.AddWithValue("@UserName", userName.Trim());
            await using var reader = await listingsCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                listings.Add(new ListingDto
                {
                    Id = reader.GetGuid(0),
                    Title = reader.GetString(1),
                    Description = reader.GetString(2),
                    Category = reader.GetString(3),
                    SaleMode = reader.GetString(4),
                    Price = reader.GetDecimal(5),
                    Currency = reader.GetString(6),
                    SellerName = reader.GetString(7),
                    Status = reader.GetString(8),
                    CreatedAt = new DateTimeOffset(reader.GetDateTime(9), TimeSpan.Zero)
                });
            }
        }

        var payments = new List<UserPaymentDto>();
        await using (var paymentsCommand = new SqlCommand(paymentsSql, connection))
        {
            paymentsCommand.Parameters.AddWithValue("@UserName", userName.Trim());
            await using var reader = await paymentsCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                payments.Add(new UserPaymentDto
                {
                    Id = reader.GetGuid(0),
                    PaymentType = reader.GetString(1),
                    PaymentStatus = reader.GetString(2),
                    Amount = reader.GetDecimal(3),
                    CurrencyCode = reader.GetString(4).Trim(),
                    PackageName = reader.GetString(5),
                    CreatedAt = new DateTimeOffset(reader.GetDateTime(6), TimeSpan.Zero)
                });
            }
        }

        dashboard.RecentListings = listings;
        dashboard.RecentPayments = payments;
        return dashboard;
    }

    public async Task<IReadOnlyList<ListingDto>> GetListingsAsync(string? categorySlug, string? saleModeKey, CancellationToken cancellationToken = default)
    {
        var sql = new StringBuilder("""
            SELECT
                l.Id,
                p.Title,
                p.Description,
                c.Name AS CategoryName,
                sm.ModeKey,
                l.Price,
                l.CurrencyCode,
                ISNULL(up.UserName, u.Email) AS SellerName,
                l.ListingStatus,
                l.CreatedAt
            FROM dbo.Listings l
            INNER JOIN dbo.Products p ON p.Id = l.ProductId
            INNER JOIN dbo.Categories c ON c.Id = p.CategoryId
            INNER JOIN dbo.SaleModes sm ON sm.Id = l.SaleModeId
            INNER JOIN dbo.Users u ON u.Id = l.SellerUserId
            LEFT JOIN dbo.UserProfiles up ON up.UserId = u.Id
            WHERE 1 = 1
            """);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand();
        command.Connection = connection;

        if (!string.IsNullOrWhiteSpace(categorySlug))
        {
            sql.AppendLine(" AND c.Slug = @CategorySlug");
            command.Parameters.AddWithValue("@CategorySlug", categorySlug);
        }

        if (!string.IsNullOrWhiteSpace(saleModeKey))
        {
            sql.AppendLine(" AND sm.ModeKey = @SaleModeKey");
            command.Parameters.AddWithValue("@SaleModeKey", saleModeKey);
        }

        sql.AppendLine(" ORDER BY l.CreatedAt DESC;");
        command.CommandText = sql.ToString();

        var listings = new List<ListingDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            listings.Add(new ListingDto
            {
                Id = reader.GetGuid(0),
                Title = reader.GetString(1),
                Description = reader.GetString(2),
                Category = reader.GetString(3),
                SaleMode = reader.GetString(4),
                Price = reader.GetDecimal(5),
                Currency = reader.GetString(6),
                SellerName = reader.GetString(7),
                Status = reader.GetString(8),
                CreatedAt = new DateTimeOffset(reader.GetDateTime(9), TimeSpan.Zero)
            });
        }

        return listings;
    }

    public async Task<ListingDto?> GetListingByIdAsync(Guid listingId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP 1
                l.Id,
                p.Title,
                p.Description,
                c.Name AS CategoryName,
                sm.ModeKey,
                l.Price,
                l.CurrencyCode,
                ISNULL(up.UserName, u.Email) AS SellerName,
                l.ListingStatus,
                l.CreatedAt
            FROM dbo.Listings l
            INNER JOIN dbo.Products p ON p.Id = l.ProductId
            INNER JOIN dbo.Categories c ON c.Id = p.CategoryId
            INNER JOIN dbo.SaleModes sm ON sm.Id = l.SaleModeId
            INNER JOIN dbo.Users u ON u.Id = l.SellerUserId
            LEFT JOIN dbo.UserProfiles up ON up.UserId = u.Id
            WHERE l.Id = @ListingId;
            """;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ListingId", listingId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new ListingDto
        {
            Id = reader.GetGuid(0),
            Title = reader.GetString(1),
            Description = reader.GetString(2),
            Category = reader.GetString(3),
            SaleMode = reader.GetString(4),
            Price = reader.GetDecimal(5),
            Currency = reader.GetString(6),
            SellerName = reader.GetString(7),
            Status = reader.GetString(8),
            CreatedAt = new DateTimeOffset(reader.GetDateTime(9), TimeSpan.Zero)
        };
    }

    public async Task<AuctionDto?> GetAuctionByListingIdAsync(Guid listingId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await EnsureAuctionLifecycleAsync(connection, listingId, cancellationToken);

        const string sql = """
            SELECT TOP 1
                a.Id,
                a.ListingId,
                a.StartPrice,
                a.MinBidIncrement,
                a.CurrentBidAmount,
                ISNULL(winnerUp.UserName, winnerUser.Email) AS CurrentWinnerUserName,
                a.StartsAt,
                a.EndsAt,
                a.AutoExtendMinutes,
                a.AuctionStatus,
                a.ResultProcessed
            FROM dbo.Auctions a
            LEFT JOIN dbo.Users winnerUser ON winnerUser.Id = a.CurrentWinnerUserId
            LEFT JOIN dbo.UserProfiles winnerUp ON winnerUp.UserId = winnerUser.Id
            WHERE a.ListingId = @ListingId;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ListingId", listingId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new AuctionDto
        {
            Id = reader.GetGuid(0),
            ListingId = reader.GetGuid(1),
            StartPrice = reader.GetDecimal(2),
            MinBidIncrement = reader.GetDecimal(3),
            CurrentBidAmount = reader.IsDBNull(4) ? null : reader.GetDecimal(4),
            CurrentWinnerUserName = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
            StartsAt = new DateTimeOffset(reader.GetDateTime(6), TimeSpan.Zero),
            EndsAt = new DateTimeOffset(reader.GetDateTime(7), TimeSpan.Zero),
            AutoExtendMinutes = reader.GetInt32(8),
            AuctionStatus = reader.GetString(9),
            ResultProcessed = reader.GetBoolean(10)
        };
    }

    public async Task<IReadOnlyList<AuctionBidDto>> GetAuctionBidsByListingIdAsync(Guid listingId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await EnsureAuctionLifecycleAsync(connection, listingId, cancellationToken);

        const string sql = """
            SELECT
                b.Id,
                b.AuctionId,
                ISNULL(up.UserName, u.Email) AS BidderUserName,
                b.BidAmount,
                b.BidStatus,
                b.CreatedAt
            FROM dbo.AuctionBids b
            INNER JOIN dbo.Auctions a ON a.Id = b.AuctionId
            INNER JOIN dbo.Users u ON u.Id = b.BidderUserId
            LEFT JOIN dbo.UserProfiles up ON up.UserId = u.Id
            WHERE a.ListingId = @ListingId
            ORDER BY b.CreatedAt DESC;
            """;

        var bids = new List<AuctionBidDto>();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ListingId", listingId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            bids.Add(new AuctionBidDto
            {
                Id = reader.GetGuid(0),
                AuctionId = reader.GetGuid(1),
                BidderUserName = reader.GetString(2),
                BidAmount = reader.GetDecimal(3),
                BidStatus = reader.GetString(4),
                CreatedAt = new DateTimeOffset(reader.GetDateTime(5), TimeSpan.Zero)
            });
        }

        return bids;
    }

    public async Task<AuctionBidDto> AddAuctionBidAsync(Guid listingId, CreateAuctionBidRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureAuctionLifecycleAsync(connection, listingId, cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var auction = await GetAuctionRecordAsync(connection, transaction, listingId, cancellationToken);
            if (auction is null)
            {
                throw new InvalidOperationException("Bu ilana ait acik artirma bulunamadi.");
            }

            if (!string.Equals(auction.Status, "active", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Acik artirma su anda teklif kabul etmiyor.");
            }

            var bidderUserId = await ResolveSellerUserIdAsync(connection, transaction, request.BidderUserName, cancellationToken);
            if (bidderUserId == Guid.Empty)
            {
                throw new InvalidOperationException("Teklif veren kullanici bulunamadi.");
            }

            var sellerUserId = await ResolveListingSellerUserIdAsync(connection, transaction, listingId, cancellationToken);
            if (sellerUserId == bidderUserId)
            {
                throw new InvalidOperationException("Kendi acik artirmaniza teklif veremezsiniz.");
            }

            var minimumBid = auction.CurrentBidAmount.HasValue
                ? auction.CurrentBidAmount.Value + auction.MinBidIncrement
                : auction.StartPrice;

            if (request.BidAmount < minimumBid)
            {
                throw new InvalidOperationException($"Minimum teklif {minimumBid:N0} TRY olmali.");
            }

            var bidId = Guid.NewGuid();

            const string insertBidSql = """
                INSERT INTO dbo.AuctionBids (Id, AuctionId, BidderUserId, BidAmount, BidStatus, CreatedAt)
                VALUES (@Id, @AuctionId, @BidderUserId, @BidAmount, N'winning', SYSUTCDATETIME());
                """;

            await using (var insertBidCommand = new SqlCommand(insertBidSql, connection, transaction))
            {
                insertBidCommand.Parameters.AddWithValue("@Id", bidId);
                insertBidCommand.Parameters.AddWithValue("@AuctionId", auction.Id);
                insertBidCommand.Parameters.AddWithValue("@BidderUserId", bidderUserId);
                insertBidCommand.Parameters.AddWithValue("@BidAmount", request.BidAmount);
                await insertBidCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            const string markOthersSql = """
                UPDATE dbo.AuctionBids
                SET BidStatus = N'losing'
                WHERE AuctionId = @AuctionId AND Id <> @BidId AND BidStatus IN (N'active', N'winning');
                """;

            await using (var markOthersCommand = new SqlCommand(markOthersSql, connection, transaction))
            {
                markOthersCommand.Parameters.AddWithValue("@AuctionId", auction.Id);
                markOthersCommand.Parameters.AddWithValue("@BidId", bidId);
                await markOthersCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            var effectiveEndsAt = auction.EndsAt;
            if (auction.AutoExtendMinutes > 0 && auction.EndsAt <= DateTime.UtcNow.AddMinutes(auction.AutoExtendMinutes))
            {
                effectiveEndsAt = auction.EndsAt.AddMinutes(auction.AutoExtendMinutes);
            }

            const string updateAuctionSql = """
                UPDATE dbo.Auctions
                SET CurrentBidAmount = @CurrentBidAmount,
                    CurrentWinnerUserId = @CurrentWinnerUserId,
                    EndsAt = @EndsAt,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE Id = @AuctionId;
                """;

            await using (var updateAuctionCommand = new SqlCommand(updateAuctionSql, connection, transaction))
            {
                updateAuctionCommand.Parameters.AddWithValue("@CurrentBidAmount", request.BidAmount);
                updateAuctionCommand.Parameters.AddWithValue("@CurrentWinnerUserId", bidderUserId);
                updateAuctionCommand.Parameters.AddWithValue("@EndsAt", effectiveEndsAt);
                updateAuctionCommand.Parameters.AddWithValue("@AuctionId", auction.Id);
                await updateAuctionCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            const string updateListingSql = """
                UPDATE dbo.Listings
                SET Price = @Price,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE Id = @ListingId;
                """;

            await using (var updateListingCommand = new SqlCommand(updateListingSql, connection, transaction))
            {
                updateListingCommand.Parameters.AddWithValue("@Price", request.BidAmount);
                updateListingCommand.Parameters.AddWithValue("@ListingId", listingId);
                await updateListingCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await InsertNotificationAsync(
                connection,
                transaction,
                sellerUserId,
                "auction.bid.received",
                "Acik artirmaya yeni teklif geldi",
                $"{request.BidderUserName} {request.BidAmount:N0} TRY teklif verdi.",
                "listing",
                listingId,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            var createdBid = (await GetAuctionBidsByListingIdAsync(listingId, cancellationToken)).FirstOrDefault(x => x.Id == bidId);
            return createdBid ?? throw new InvalidOperationException("Teklif kaydedildi ancak okunamadi.");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<ListingOfferDto>> GetListingOffersAsync(Guid listingId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                lo.Id,
                lo.ListingId,
                ISNULL(up.UserName, u.Email) AS BuyerName,
                lo.OfferedPrice,
                lo.CurrencyCode,
                ISNULL(lo.OfferNote, N''),
                lo.OfferStatus,
                lo.CreatedAt
            FROM dbo.ListingOffers lo
            INNER JOIN dbo.Users u ON u.Id = lo.BuyerUserId
            LEFT JOIN dbo.UserProfiles up ON up.UserId = u.Id
            WHERE lo.ListingId = @ListingId
            ORDER BY lo.CreatedAt DESC;
            """;

        var offers = new List<ListingOfferDto>();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ListingId", listingId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            offers.Add(new ListingOfferDto
            {
                Id = reader.GetGuid(0),
                ListingId = reader.GetGuid(1),
                BuyerName = reader.GetString(2),
                OfferedPrice = reader.GetDecimal(3),
                Currency = reader.GetString(4),
                OfferNote = reader.GetString(5),
                Status = reader.GetString(6),
                CreatedAt = new DateTimeOffset(reader.GetDateTime(7), TimeSpan.Zero)
            });
        }

        return offers;
    }

    public async Task<ListingOfferDto> AddListingOfferAsync(Guid listingId, CreateListingOfferRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var listingExists = await ListingExistsAsync(connection, transaction, listingId, cancellationToken);
            if (!listingExists)
            {
                throw new InvalidOperationException("Ilan bulunamadi.");
            }

            var buyerUserId = await ResolveSellerUserIdAsync(connection, transaction, request.BuyerName, cancellationToken);
            if (buyerUserId == Guid.Empty)
            {
                throw new InvalidOperationException("Teklif veren kullanici bulunamadi.");
            }

            var offerId = Guid.NewGuid();

            const string insertSql = """
                INSERT INTO dbo.ListingOffers
                (
                    Id, ListingId, BuyerUserId, OfferedPrice, CurrencyCode, OfferNote, OfferStatus, CreatedAt, UpdatedAt
                )
                VALUES
                (
                    @Id, @ListingId, @BuyerUserId, @OfferedPrice, N'TRY', @OfferNote, N'pending', SYSUTCDATETIME(), SYSUTCDATETIME()
                );
                """;

            await using (var command = new SqlCommand(insertSql, connection, transaction))
            {
                command.Parameters.AddWithValue("@Id", offerId);
                command.Parameters.AddWithValue("@ListingId", listingId);
                command.Parameters.AddWithValue("@BuyerUserId", buyerUserId);
                command.Parameters.AddWithValue("@OfferedPrice", request.OfferedPrice);
                command.Parameters.AddWithValue("@OfferNote", string.IsNullOrWhiteSpace(request.OfferNote) ? DBNull.Value : request.OfferNote.Trim());
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            var sellerUserId = await ResolveListingSellerUserIdAsync(connection, transaction, listingId, cancellationToken);
            if (sellerUserId != Guid.Empty)
            {
                await InsertNotificationAsync(
                    connection,
                    transaction,
                    sellerUserId,
                    "offer.received",
                    "Yeni teklif geldi",
                    $"{request.BuyerName} ilaniniz icin {request.OfferedPrice:N0} TRY teklif verdi.",
                    "listing",
                    listingId,
                    cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            var createdOffer = (await GetListingOffersAsync(listingId, cancellationToken)).FirstOrDefault(x => x.Id == offerId);
            return createdOffer ?? throw new InvalidOperationException("Olusturulan teklif okunamadi.");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ListingOfferDto> UpdateListingOfferStatusAsync(Guid listingId, Guid offerId, UpdateListingOfferStatusRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var actorUserId = await ResolveSellerUserIdAsync(connection, transaction, request.ActorUserName, cancellationToken);
            if (actorUserId == Guid.Empty)
            {
                throw new InvalidOperationException("Islemi yapan kullanici bulunamadi.");
            }

            var sellerUserId = await ResolveListingSellerUserIdAsync(connection, transaction, listingId, cancellationToken);
            if (sellerUserId == Guid.Empty)
            {
                throw new InvalidOperationException("Ilan bulunamadi.");
            }

            if (sellerUserId != actorUserId)
            {
                throw new InvalidOperationException("Bu teklifi yonetme yetkiniz yok.");
            }

            var normalizedStatus = request.Status.Trim().ToLowerInvariant();
            if (normalizedStatus is not ("accepted" or "rejected"))
            {
                throw new InvalidOperationException("Gecersiz teklif durumu.");
            }

            const string updateTargetSql = """
                UPDATE dbo.ListingOffers
                SET OfferStatus = @Status,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE Id = @OfferId AND ListingId = @ListingId;
                """;

            await using (var command = new SqlCommand(updateTargetSql, connection, transaction))
            {
                command.Parameters.AddWithValue("@Status", normalizedStatus);
                command.Parameters.AddWithValue("@OfferId", offerId);
                command.Parameters.AddWithValue("@ListingId", listingId);
                var affected = await command.ExecuteNonQueryAsync(cancellationToken);
                if (affected == 0)
                {
                    throw new InvalidOperationException("Teklif bulunamadi.");
                }
            }

            if (normalizedStatus == "accepted")
            {
                const string rejectOthersSql = """
                    UPDATE dbo.ListingOffers
                    SET OfferStatus = N'rejected',
                        UpdatedAt = SYSUTCDATETIME()
                    WHERE ListingId = @ListingId
                      AND Id <> @OfferId
                      AND OfferStatus = N'pending';
                    """;

                await using var rejectOthersCommand = new SqlCommand(rejectOthersSql, connection, transaction);
                rejectOthersCommand.Parameters.AddWithValue("@ListingId", listingId);
                rejectOthersCommand.Parameters.AddWithValue("@OfferId", offerId);
                await rejectOthersCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            var targetBuyerUserId = await ResolveOfferBuyerUserIdAsync(connection, transaction, offerId, cancellationToken);
            if (targetBuyerUserId != Guid.Empty)
            {
                await InsertNotificationAsync(
                    connection,
                    transaction,
                    targetBuyerUserId,
                    normalizedStatus == "accepted" ? "offer.accepted" : "offer.rejected",
                    normalizedStatus == "accepted" ? "Teklifiniz kabul edildi" : "Teklifiniz reddedildi",
                    normalizedStatus == "accepted"
                        ? "Satici teklifinizi kabul etti."
                        : "Satici teklifinizi reddetti.",
                    "listing",
                    listingId,
                    cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            var updatedOffer = (await GetListingOffersAsync(listingId, cancellationToken)).FirstOrDefault(x => x.Id == offerId);
            return updatedOffer ?? throw new InvalidOperationException("Guncellenen teklif okunamadi.");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<ConversationSummaryDto>> GetConversationsAsync(string userName, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                c.Id,
                c.ListingId,
                c.ConversationType,
                ISNULL(p.Title, N'Dogrudan Mesaj') AS Title,
                ISNULL(otherUp.UserName, otherUser.Email) AS CounterpartyUserName,
                ISNULL(lastMessage.MessageText, N'') AS LastMessagePreview,
                c.LastMessageAt,
                (
                    SELECT COUNT(*)
                    FROM dbo.Messages unreadMessages
                    WHERE unreadMessages.ConversationId = c.Id
                      AND unreadMessages.SenderUserId <> cp.UserId
                      AND (cp.LastReadAt IS NULL OR unreadMessages.CreatedAt > cp.LastReadAt)
                ) AS UnreadCount
            FROM dbo.ConversationParticipants cp
            INNER JOIN dbo.Conversations c ON c.Id = cp.ConversationId
            LEFT JOIN dbo.Listings l ON l.Id = c.ListingId
            LEFT JOIN dbo.Products p ON p.Id = l.ProductId
            OUTER APPLY
            (
                SELECT TOP 1 m.MessageText
                FROM dbo.Messages m
                WHERE m.ConversationId = c.Id
                ORDER BY m.CreatedAt DESC
            ) lastMessage
            INNER JOIN dbo.ConversationParticipants otherCp ON otherCp.ConversationId = c.Id AND otherCp.UserId <> cp.UserId
            INNER JOIN dbo.Users otherUser ON otherUser.Id = otherCp.UserId
            LEFT JOIN dbo.UserProfiles otherUp ON otherUp.UserId = otherUser.Id
            WHERE cp.UserId =
            (
                SELECT TOP 1 u.Id
                FROM dbo.Users u
                LEFT JOIN dbo.UserProfiles up ON up.UserId = u.Id
                WHERE up.UserName = @UserName OR u.Email = @UserName
            )
            ORDER BY ISNULL(c.LastMessageAt, c.CreatedAt) DESC;
            """;

        var conversations = new List<ConversationSummaryDto>();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserName", userName.Trim());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            conversations.Add(new ConversationSummaryDto
            {
                Id = reader.GetGuid(0),
                ListingId = reader.IsDBNull(1) ? null : reader.GetGuid(1),
                ConversationType = reader.GetString(2),
                Title = reader.GetString(3),
                CounterpartyUserName = reader.GetString(4),
                LastMessagePreview = reader.GetString(5),
                LastMessageAt = reader.IsDBNull(6) ? null : new DateTimeOffset(reader.GetDateTime(6), TimeSpan.Zero),
                UnreadCount = reader.GetInt32(7)
            });
        }

        return conversations;
    }

    public async Task<ConversationDetailDto?> GetConversationByIdAsync(Guid conversationId, string userName, CancellationToken cancellationToken = default)
    {
        const string headerSql = """
            SELECT TOP 1
                c.Id,
                c.ListingId,
                c.ConversationType,
                ISNULL(p.Title, N'Dogrudan Mesaj') AS Title,
                ISNULL(otherUp.UserName, otherUser.Email) AS CounterpartyUserName
            FROM dbo.Conversations c
            INNER JOIN dbo.ConversationParticipants cp ON cp.ConversationId = c.Id
            LEFT JOIN dbo.Listings l ON l.Id = c.ListingId
            LEFT JOIN dbo.Products p ON p.Id = l.ProductId
            INNER JOIN dbo.ConversationParticipants otherCp ON otherCp.ConversationId = c.Id AND otherCp.UserId <> cp.UserId
            INNER JOIN dbo.Users otherUser ON otherUser.Id = otherCp.UserId
            LEFT JOIN dbo.UserProfiles otherUp ON otherUp.UserId = otherUser.Id
            WHERE c.Id = @ConversationId
              AND cp.UserId =
              (
                  SELECT TOP 1 u.Id
                  FROM dbo.Users u
                  LEFT JOIN dbo.UserProfiles up ON up.UserId = u.Id
                  WHERE up.UserName = @UserName OR u.Email = @UserName
              );
            """;

        const string messagesSql = """
            SELECT
                m.Id,
                m.ConversationId,
                ISNULL(up.UserName, u.Email) AS SenderUserName,
                ISNULL(m.MessageText, N''),
                m.CreatedAt
            FROM dbo.Messages m
            INNER JOIN dbo.Users u ON u.Id = m.SenderUserId
            LEFT JOIN dbo.UserProfiles up ON up.UserId = u.Id
            WHERE m.ConversationId = @ConversationId
            ORDER BY m.CreatedAt ASC;
            """;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await MarkConversationReadAsync(connection, conversationId, userName, cancellationToken);

        ConversationDetailDto? conversation = null;

        await using (var headerCommand = new SqlCommand(headerSql, connection))
        {
            headerCommand.Parameters.AddWithValue("@ConversationId", conversationId);
            headerCommand.Parameters.AddWithValue("@UserName", userName.Trim());

            await using var reader = await headerCommand.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                conversation = new ConversationDetailDto
                {
                    Id = reader.GetGuid(0),
                    ListingId = reader.IsDBNull(1) ? null : reader.GetGuid(1),
                    ConversationType = reader.GetString(2),
                    Title = reader.GetString(3),
                    CounterpartyUserName = reader.GetString(4)
                };
            }
        }

        if (conversation is null)
        {
            return null;
        }

        var messages = new List<MessageDto>();
        await using (var messagesCommand = new SqlCommand(messagesSql, connection))
        {
            messagesCommand.Parameters.AddWithValue("@ConversationId", conversationId);
            await using var reader = await messagesCommand.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var senderUserName = reader.GetString(2);
                messages.Add(new MessageDto
                {
                    Id = reader.GetGuid(0),
                    ConversationId = reader.GetGuid(1),
                    SenderUserName = senderUserName,
                    MessageText = reader.GetString(3),
                    CreatedAt = new DateTimeOffset(reader.GetDateTime(4), TimeSpan.Zero),
                    IsMine = string.Equals(senderUserName, userName, StringComparison.OrdinalIgnoreCase)
                });
            }
        }

        conversation.Messages = messages;
        return conversation;
    }

    public async Task<ConversationDetailDto> StartListingConversationAsync(Guid listingId, StartListingConversationRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var actorUserId = await ResolveSellerUserIdAsync(connection, transaction, request.UserName, cancellationToken);
            if (actorUserId == Guid.Empty)
            {
                throw new InvalidOperationException("Kullanici bulunamadi.");
            }

            var sellerUserId = await ResolveListingSellerUserIdAsync(connection, transaction, listingId, cancellationToken);
            if (sellerUserId == Guid.Empty)
            {
                throw new InvalidOperationException("Ilan bulunamadi.");
            }

            if (actorUserId == sellerUserId)
            {
                throw new InvalidOperationException("Kendi ilaniniza mesaj gonderemezsiniz.");
            }

            var existingConversationId = await FindListingConversationAsync(connection, transaction, listingId, actorUserId, sellerUserId, cancellationToken);
            var conversationId = existingConversationId == Guid.Empty ? Guid.NewGuid() : existingConversationId;

            if (existingConversationId == Guid.Empty)
            {
                const string insertConversationSql = """
                    INSERT INTO dbo.Conversations (Id, ListingId, ConversationType, LastMessageAt, CreatedAt)
                    VALUES (@Id, @ListingId, N'listing', NULL, SYSUTCDATETIME());
                    """;

                await using (var conversationCommand = new SqlCommand(insertConversationSql, connection, transaction))
                {
                    conversationCommand.Parameters.AddWithValue("@Id", conversationId);
                    conversationCommand.Parameters.AddWithValue("@ListingId", listingId);
                    await conversationCommand.ExecuteNonQueryAsync(cancellationToken);
                }

                const string insertParticipantSql = """
                    INSERT INTO dbo.ConversationParticipants (ConversationId, UserId, JoinedAt)
                    VALUES (@ConversationId, @UserId, SYSUTCDATETIME());
                    """;

                foreach (var userId in new[] { actorUserId, sellerUserId })
                {
                    await using var participantCommand = new SqlCommand(insertParticipantSql, connection, transaction);
                    participantCommand.Parameters.AddWithValue("@ConversationId", conversationId);
                    participantCommand.Parameters.AddWithValue("@UserId", userId);
                    await participantCommand.ExecuteNonQueryAsync(cancellationToken);
                }
            }

            await transaction.CommitAsync(cancellationToken);

            return await GetConversationByIdAsync(conversationId, request.UserName, cancellationToken)
                ?? throw new InvalidOperationException("Konusma olusturulamadi.");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<MessageDto> AddMessageAsync(Guid conversationId, SendMessageRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var senderUserId = await ResolveSellerUserIdAsync(connection, transaction, request.SenderUserName, cancellationToken);
            if (senderUserId == Guid.Empty)
            {
                throw new InvalidOperationException("Mesaj gonderen kullanici bulunamadi.");
            }

            if (!await IsConversationParticipantAsync(connection, transaction, conversationId, senderUserId, cancellationToken))
            {
                throw new InvalidOperationException("Bu konusmaya mesaj gonderme yetkiniz yok.");
            }

            var messageId = Guid.NewGuid();

            const string insertMessageSql = """
                INSERT INTO dbo.Messages (Id, ConversationId, SenderUserId, MessageType, MessageText, CreatedAt)
                VALUES (@Id, @ConversationId, @SenderUserId, N'text', @MessageText, SYSUTCDATETIME());
                """;

            await using (var messageCommand = new SqlCommand(insertMessageSql, connection, transaction))
            {
                messageCommand.Parameters.AddWithValue("@Id", messageId);
                messageCommand.Parameters.AddWithValue("@ConversationId", conversationId);
                messageCommand.Parameters.AddWithValue("@SenderUserId", senderUserId);
                messageCommand.Parameters.AddWithValue("@MessageText", request.MessageText.Trim());
                await messageCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            const string updateConversationSql = """
                UPDATE dbo.Conversations
                SET LastMessageAt = SYSUTCDATETIME()
                WHERE Id = @ConversationId;
                """;

            await using (var conversationCommand = new SqlCommand(updateConversationSql, connection, transaction))
            {
                conversationCommand.Parameters.AddWithValue("@ConversationId", conversationId);
                await conversationCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            var recipientUserId = await ResolveConversationRecipientUserIdAsync(connection, transaction, conversationId, senderUserId, cancellationToken);
            if (recipientUserId != Guid.Empty)
            {
                await InsertNotificationAsync(
                    connection,
                    transaction,
                    recipientUserId,
                    "message.received",
                    "Yeni mesaj geldi",
                    $"{request.SenderUserName}: {request.MessageText.Trim()}",
                    "conversation",
                    conversationId,
                    cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            var conversation = await GetConversationByIdAsync(conversationId, request.SenderUserName, cancellationToken);
            var message = conversation?.Messages.FirstOrDefault(x => x.Id == messageId);
            return message ?? throw new InvalidOperationException("Mesaj kaydedildi ancak okunamadi.");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(string userName, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                n.Id,
                n.NotificationType,
                n.Title,
                n.Body,
                n.RelatedEntityType,
                n.RelatedEntityId,
                n.IsRead,
                n.CreatedAt
            FROM dbo.Notifications n
            WHERE n.UserId =
            (
                SELECT TOP 1 u.Id
                FROM dbo.Users u
                LEFT JOIN dbo.UserProfiles up ON up.UserId = u.Id
                WHERE up.UserName = @UserName OR u.Email = @UserName
            )
            ORDER BY n.CreatedAt DESC;
            """;

        var notifications = new List<NotificationDto>();
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserName", userName.Trim());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            notifications.Add(new NotificationDto
            {
                Id = reader.GetGuid(0),
                NotificationType = reader.GetString(1),
                Title = reader.GetString(2),
                Body = reader.GetString(3),
                RelatedEntityType = reader.IsDBNull(4) ? null : reader.GetString(4),
                RelatedEntityId = reader.IsDBNull(5) ? null : reader.GetGuid(5),
                IsRead = reader.GetBoolean(6),
                CreatedAt = new DateTimeOffset(reader.GetDateTime(7), TimeSpan.Zero)
            });
        }

        return notifications;
    }

    public async Task<bool> MarkNotificationReadAsync(Guid notificationId, string userName, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Notifications
            SET IsRead = 1,
                ReadAt = SYSUTCDATETIME()
            WHERE Id = @NotificationId
              AND UserId =
              (
                  SELECT TOP 1 u.Id
                  FROM dbo.Users u
                  LEFT JOIN dbo.UserProfiles up ON up.UserId = u.Id
                  WHERE up.UserName = @UserName OR u.Email = @UserName
              );
            """;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@NotificationId", notificationId);
        command.Parameters.AddWithValue("@UserName", userName.Trim());
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<ListingDto> AddListingAsync(CreateListingRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var sellerUserId = await ResolveSellerUserIdAsync(connection, transaction, request.SellerName, cancellationToken);
            if (sellerUserId == Guid.Empty)
            {
                throw new InvalidOperationException("Satici bulunamadi. Once kayit olun veya giris yapin.");
            }

            var categoryId = await ResolveCategoryIdAsync(connection, transaction, request.CategorySlug, cancellationToken);
            var saleModeId = await ResolveSaleModeIdAsync(connection, transaction, request.SaleModeKey, cancellationToken);

            var productId = Guid.NewGuid();
            var listingId = Guid.NewGuid();

            const string insertProductSql = """
                INSERT INTO dbo.Products
                (
                    Id, OwnerUserId, CategoryId, Title, Description, ConditionType, StockQuantity, CreatedAt, UpdatedAt
                )
                VALUES
                (
                    @Id, @OwnerUserId, @CategoryId, @Title, @Description, N'used', 1, SYSUTCDATETIME(), SYSUTCDATETIME()
                );
                """;

            await using (var productCommand = new SqlCommand(insertProductSql, connection, transaction))
            {
                productCommand.Parameters.AddWithValue("@Id", productId);
                productCommand.Parameters.AddWithValue("@OwnerUserId", sellerUserId);
                productCommand.Parameters.AddWithValue("@CategoryId", categoryId);
                productCommand.Parameters.AddWithValue("@Title", request.Title.Trim());
                productCommand.Parameters.AddWithValue("@Description", request.Description.Trim());
                await productCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            const string insertListingSql = """
                INSERT INTO dbo.Listings
                (
                    Id, ProductId, SellerUserId, SaleModeId, Price, CurrencyCode, ListingStatus, PublishedAt, CreatedAt, UpdatedAt
                )
                VALUES
                (
                    @Id, @ProductId, @SellerUserId, @SaleModeId, @Price, N'TRY', N'published', SYSUTCDATETIME(), SYSUTCDATETIME(), SYSUTCDATETIME()
                );
                """;

            await using (var listingCommand = new SqlCommand(insertListingSql, connection, transaction))
            {
                listingCommand.Parameters.AddWithValue("@Id", listingId);
                listingCommand.Parameters.AddWithValue("@ProductId", productId);
                listingCommand.Parameters.AddWithValue("@SellerUserId", sellerUserId);
                listingCommand.Parameters.AddWithValue("@SaleModeId", saleModeId);
                listingCommand.Parameters.AddWithValue("@Price", request.Price);
                await listingCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            if (string.Equals(request.SaleModeKey, "auction", StringComparison.OrdinalIgnoreCase))
            {
                if (!request.AuctionStartPrice.HasValue || !request.AuctionMinBidIncrement.HasValue ||
                    !request.AuctionStartsAt.HasValue || !request.AuctionEndsAt.HasValue)
                {
                    throw new InvalidOperationException("Acik artirma alanlari eksik.");
                }

                if (request.AuctionStartPrice.Value <= 0 || request.AuctionMinBidIncrement.Value <= 0)
                {
                    throw new InvalidOperationException("Acik artirma fiyat alanlari sifirdan buyuk olmali.");
                }

                if (request.AuctionEndsAt.Value <= request.AuctionStartsAt.Value)
                {
                    throw new InvalidOperationException("Acik artirma bitis tarihi baslangictan sonra olmali.");
                }

                var auctionStatus = request.AuctionStartsAt.Value.UtcDateTime <= DateTime.UtcNow ? "active" : "scheduled";

                const string insertAuctionSql = """
                    INSERT INTO dbo.Auctions
                    (
                        Id, ListingId, StartPrice, MinBidIncrement, CurrentBidAmount, CurrentWinnerUserId, StartsAt, EndsAt, AutoExtendMinutes, AuctionStatus, ResultProcessed, CreatedAt, UpdatedAt
                    )
                    VALUES
                    (
                        @Id, @ListingId, @StartPrice, @MinBidIncrement, NULL, NULL, @StartsAt, @EndsAt, @AutoExtendMinutes, @AuctionStatus, 0, SYSUTCDATETIME(), SYSUTCDATETIME()
                    );
                    """;

                await using var auctionCommand = new SqlCommand(insertAuctionSql, connection, transaction);
                auctionCommand.Parameters.AddWithValue("@Id", Guid.NewGuid());
                auctionCommand.Parameters.AddWithValue("@ListingId", listingId);
                auctionCommand.Parameters.AddWithValue("@StartPrice", request.AuctionStartPrice.Value);
                auctionCommand.Parameters.AddWithValue("@MinBidIncrement", request.AuctionMinBidIncrement.Value);
                auctionCommand.Parameters.AddWithValue("@StartsAt", request.AuctionStartsAt.Value.UtcDateTime);
                auctionCommand.Parameters.AddWithValue("@EndsAt", request.AuctionEndsAt.Value.UtcDateTime);
                auctionCommand.Parameters.AddWithValue("@AutoExtendMinutes", request.AuctionAutoExtendMinutes ?? 0);
                auctionCommand.Parameters.AddWithValue("@AuctionStatus", auctionStatus);
                await auctionCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            var createdListing = (await GetListingsAsync(null, null, cancellationToken)).FirstOrDefault(x => x.Id == listingId);
            return createdListing ?? throw new InvalidOperationException("Olusturulan ilan okunamadi.");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            if (await EmailExistsAsync(connection, transaction, request.Email, cancellationToken))
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Bu e-posta zaten kayitli."
                };
            }

            if (await UserNameExistsAsync(connection, transaction, request.UserName, cancellationToken))
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Bu kullanici adi zaten kullaniliyor."
                };
            }

            var userId = Guid.NewGuid();
            var passwordHash = HashPassword(request.Password);
            var accountType = request.AccountType == "corporate" ? "corporate" : "individual";

            const string insertUserSql = """
                INSERT INTO dbo.Users
                (
                    Id, Email, PasswordHash, AccountType, Status, EmailConfirmed, PhoneConfirmed, CreatedAt, UpdatedAt
                )
                VALUES
                (
                    @Id, @Email, @PasswordHash, @AccountType, N'active', 1, 0, SYSUTCDATETIME(), SYSUTCDATETIME()
                );
                """;

            await using (var userCommand = new SqlCommand(insertUserSql, connection, transaction))
            {
                userCommand.Parameters.AddWithValue("@Id", userId);
                userCommand.Parameters.AddWithValue("@Email", request.Email.Trim());
                userCommand.Parameters.AddWithValue("@PasswordHash", passwordHash);
                userCommand.Parameters.AddWithValue("@AccountType", accountType);
                await userCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            const string insertProfileSql = """
                INSERT INTO dbo.UserProfiles
                (
                    UserId, UserName, FullName, CreatedAt, UpdatedAt
                )
                VALUES
                (
                    @UserId, @UserName, @FullName, SYSUTCDATETIME(), SYSUTCDATETIME()
                );
                """;

            await using (var profileCommand = new SqlCommand(insertProfileSql, connection, transaction))
            {
                profileCommand.Parameters.AddWithValue("@UserId", userId);
                profileCommand.Parameters.AddWithValue("@UserName", request.UserName.Trim());
                profileCommand.Parameters.AddWithValue("@FullName", request.FullName.Trim());
                await profileCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await AssignRoleAsync(connection, transaction, userId, accountType == "corporate" ? "CorporateUser" : "User", cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new AuthResponseDto
            {
                IsSuccess = true,
                Message = "Kayit basarili.",
                UserName = request.UserName.Trim(),
                RoleName = accountType == "corporate" ? "CorporateUser" : "User"
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP 1 u.PasswordHash, ISNULL(up.UserName, u.Email) AS UserName
            FROM dbo.Users u
            LEFT JOIN dbo.UserProfiles up ON up.UserId = u.Id
            WHERE u.Email = @Email AND u.Status = N'active';
            """;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Email", request.Email.Trim());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return new AuthResponseDto
            {
                IsSuccess = false,
                Message = "Kullanici bulunamadi."
            };
        }

        var passwordHash = reader.GetString(0);
        var userName = reader.GetString(1);
        if (!VerifyPassword(request.Password, passwordHash))
        {
            return new AuthResponseDto
            {
                IsSuccess = false,
                Message = "Sifre hatali."
            };
        }

        return new AuthResponseDto
        {
            IsSuccess = true,
            Message = "Giris basarili.",
            UserName = userName,
            RoleName = "User"
        };
    }

    public async Task<AuthResponseDto> LoginAdminAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP 1
                u.PasswordHash,
                ISNULL(up.UserName, u.Email) AS UserName,
                r.Name AS RoleName
            FROM dbo.Users u
            LEFT JOIN dbo.UserProfiles up ON up.UserId = u.Id
            INNER JOIN dbo.UserRoles ur ON ur.UserId = u.Id
            INNER JOIN dbo.Roles r ON r.Id = ur.RoleId
            WHERE u.Email = @Email
              AND u.Status = N'active'
              AND r.Name IN (N'Admin', N'SuperAdmin', N'Moderator')
            ORDER BY CASE r.Name
                WHEN N'SuperAdmin' THEN 1
                WHEN N'Admin' THEN 2
                ELSE 3
            END;
            """;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Email", request.Email.Trim());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return new AuthResponseDto
            {
                IsSuccess = false,
                Message = "Admin yetkili kullanici bulunamadi."
            };
        }

        var passwordHash = reader.GetString(0);
        var userName = reader.GetString(1);
        var roleName = reader.GetString(2);
        if (!VerifyPassword(request.Password, passwordHash))
        {
            return new AuthResponseDto
            {
                IsSuccess = false,
                Message = "Sifre hatali."
            };
        }

        return new AuthResponseDto
        {
            IsSuccess = true,
            Message = "Admin girisi basarili.",
            UserName = userName,
            RoleName = roleName
        };
    }

    private static IReadOnlyList<string> GetSaleModeSteps(string saleModeKey) => saleModeKey switch
    {
        "direct" => ["Fiyat belirle", "Yayinla", "Mesaj al", "Anlas", "Teslim et"],
        "auction" => ["Baslangic fiyati", "Sure ayarla", "Teklif topla", "Kazanan sec", "Teslim et"],
        "trade" => ["Takas ilani", "Teklif al", "Kabul et", "Teslim et", "Onayla"],
        "demand" => ["Talep ac", "Teklifleri topla", "Karsilastir", "Kazanan sec", "Kapat"],
        _ => ["Yayinla", "Takip et"]
    };

    private static string NormalizePackageType(string packageType) => packageType.Trim().ToLowerInvariant() switch
    {
        "listing" => "listing",
        "featured" => "featured",
        "showcase" => "showcase",
        "corporate_membership" => "corporate_membership",
        _ => throw new InvalidOperationException("Gecersiz paket tipi.")
    };

    private static string NormalizeComplaintStatus(string status) => status.Trim().ToLowerInvariant() switch
    {
        "open" => "open",
        "in_review" => "in_review",
        "resolved" => "resolved",
        "rejected" => "rejected",
        _ => throw new InvalidOperationException("Gecersiz sikayet durumu.")
    };

    private static string MapPackageTypeToPaymentType(string packageType) => packageType switch
    {
        "listing" => "listing_fee",
        "featured" => "featured_fee",
        "showcase" => "showcase_fee",
        "corporate_membership" => "membership_fee",
        _ => "listing_fee"
    };

    private static async Task<AuctionRecord?> GetAuctionRecordAsync(SqlConnection connection, SqlTransaction transaction, Guid listingId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP 1 Id, StartPrice, MinBidIncrement, CurrentBidAmount, CurrentWinnerUserId, StartsAt, EndsAt, AutoExtendMinutes, AuctionStatus, ResultProcessed
            FROM dbo.Auctions
            WHERE ListingId = @ListingId;
            """;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@ListingId", listingId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new AuctionRecord
        {
            Id = reader.GetGuid(0),
            StartPrice = reader.GetDecimal(1),
            MinBidIncrement = reader.GetDecimal(2),
            CurrentBidAmount = reader.IsDBNull(3) ? null : reader.GetDecimal(3),
            CurrentWinnerUserId = reader.IsDBNull(4) ? null : reader.GetGuid(4),
            StartsAt = reader.GetDateTime(5),
            EndsAt = reader.GetDateTime(6),
            AutoExtendMinutes = reader.GetInt32(7),
            Status = reader.GetString(8),
            ResultProcessed = reader.GetBoolean(9)
        };
    }

    private async Task EnsureAuctionLifecycleAsync(SqlConnection connection, Guid listingId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP 1 Id, ListingId, CurrentWinnerUserId, StartsAt, EndsAt, AuctionStatus, ResultProcessed
            FROM dbo.Auctions
            WHERE ListingId = @ListingId;
            """;

        await using var loadCommand = new SqlCommand(sql, connection);
        loadCommand.Parameters.AddWithValue("@ListingId", listingId);
        await using var reader = await loadCommand.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return;
        }

        var auctionId = reader.GetGuid(0);
        var winnerUserId = reader.IsDBNull(2) ? (Guid?)null : reader.GetGuid(2);
        var startsAt = reader.GetDateTime(3);
        var endsAt = reader.GetDateTime(4);
        var status = reader.GetString(5);
        var resultProcessed = reader.GetBoolean(6);
        await reader.CloseAsync();

        var now = DateTime.UtcNow;
        if (status == "scheduled" && startsAt <= now && endsAt > now)
        {
            await using var activateCommand = new SqlCommand("""
                UPDATE dbo.Auctions
                SET AuctionStatus = N'active',
                    UpdatedAt = SYSUTCDATETIME()
                WHERE Id = @AuctionId;
                """, connection);
            activateCommand.Parameters.AddWithValue("@AuctionId", auctionId);
            await activateCommand.ExecuteNonQueryAsync(cancellationToken);
            status = "active";
        }

        if ((status == "active" || status == "scheduled") && endsAt <= now)
        {
            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                await using (var completeCommand = new SqlCommand("""
                    UPDATE dbo.Auctions
                    SET AuctionStatus = N'completed',
                        UpdatedAt = SYSUTCDATETIME()
                    WHERE Id = @AuctionId;
                    """, connection, transaction))
                {
                    completeCommand.Parameters.AddWithValue("@AuctionId", auctionId);
                    await completeCommand.ExecuteNonQueryAsync(cancellationToken);
                }

                await using (var bidStatusCommand = new SqlCommand("""
                    UPDATE dbo.AuctionBids
                    SET BidStatus = CASE WHEN Id =
                        (
                            SELECT TOP 1 Id
                            FROM dbo.AuctionBids
                            WHERE AuctionId = @AuctionId
                            ORDER BY BidAmount DESC, CreatedAt ASC
                        )
                        THEN N'winning'
                        ELSE N'losing'
                    END
                    WHERE AuctionId = @AuctionId;
                    """, connection, transaction))
                {
                    bidStatusCommand.Parameters.AddWithValue("@AuctionId", auctionId);
                    await bidStatusCommand.ExecuteNonQueryAsync(cancellationToken);
                }

                await using (var listingStatusCommand = new SqlCommand("""
                    UPDATE dbo.Listings
                    SET ListingStatus = @ListingStatus,
                        UpdatedAt = SYSUTCDATETIME()
                    WHERE Id = @ListingId;
                    """, connection, transaction))
                {
                    listingStatusCommand.Parameters.AddWithValue("@ListingId", listingId);
                    listingStatusCommand.Parameters.AddWithValue("@ListingStatus", winnerUserId.HasValue ? "sold" : "published");
                    await listingStatusCommand.ExecuteNonQueryAsync(cancellationToken);
                }

                if (!resultProcessed)
                {
                    var sellerUserId = await ResolveListingSellerUserIdAsync(connection, transaction, listingId, cancellationToken);
                    if (sellerUserId != Guid.Empty)
                    {
                        await InsertNotificationAsync(
                            connection,
                            transaction,
                            sellerUserId,
                            "auction.completed",
                            "Acik artirma tamamlandi",
                            winnerUserId.HasValue ? "Kazanan teklif belirlendi." : "Acik artirma kazanan olmadan bitti.",
                            "listing",
                            listingId,
                            cancellationToken);
                    }

                    if (winnerUserId.HasValue)
                    {
                        await InsertNotificationAsync(
                            connection,
                            transaction,
                            winnerUserId.Value,
                            "auction.won",
                            "Acik artirmayi kazandiniz",
                            "En yuksek teklif size ait ve acik artirma tamamlandi.",
                            "listing",
                            listingId,
                            cancellationToken);
                    }

                    await using var processCommand = new SqlCommand("""
                        UPDATE dbo.Auctions
                        SET ResultProcessed = 1,
                            UpdatedAt = SYSUTCDATETIME()
                        WHERE Id = @AuctionId;
                        """, connection, transaction);
                    processCommand.Parameters.AddWithValue("@AuctionId", auctionId);
                    await processCommand.ExecuteNonQueryAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split(':', 2);
        if (parts.Length != 2)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[0]);
        var expectedHash = Convert.FromBase64String(parts[1]);
        var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100000, HashAlgorithmName.SHA256, expectedHash.Length);
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    private static async Task<bool> EmailExistsAsync(SqlConnection connection, SqlTransaction transaction, string email, CancellationToken cancellationToken)
    {
        const string sql = "SELECT COUNT(*) FROM dbo.Users WHERE Email = @Email;";
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@Email", email.Trim());
        return (int)await command.ExecuteScalarAsync(cancellationToken) > 0;
    }

    private static async Task<bool> UserNameExistsAsync(SqlConnection connection, SqlTransaction transaction, string userName, CancellationToken cancellationToken)
    {
        const string sql = "SELECT COUNT(*) FROM dbo.UserProfiles WHERE UserName = @UserName;";
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@UserName", userName.Trim());
        return (int)await command.ExecuteScalarAsync(cancellationToken) > 0;
    }

    private static async Task AssignRoleAsync(SqlConnection connection, SqlTransaction transaction, Guid userId, string roleName, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.UserRoles (UserId, RoleId, AssignedAt)
            SELECT @UserId, Id, SYSUTCDATETIME()
            FROM dbo.Roles
            WHERE Name = @RoleName;
            """;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@RoleName", roleName);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<Guid> ResolveSellerUserIdAsync(SqlConnection connection, SqlTransaction transaction, string sellerName, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP 1 u.Id
            FROM dbo.Users u
            LEFT JOIN dbo.UserProfiles up ON up.UserId = u.Id
            WHERE up.UserName = @SellerName OR u.Email = @SellerName;
            """;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@SellerName", sellerName.Trim());
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is Guid id ? id : Guid.Empty;
    }

    private static async Task<Guid> ResolveCategoryIdAsync(SqlConnection connection, SqlTransaction transaction, string categorySlug, CancellationToken cancellationToken)
    {
        const string sql = "SELECT TOP 1 Id FROM dbo.Categories WHERE Slug = @Slug;";
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@Slug", categorySlug.Trim());
        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is not Guid id)
        {
            throw new InvalidOperationException("Kategori bulunamadi.");
        }

        return id;
    }

    private static async Task<Guid> ResolveSaleModeIdAsync(SqlConnection connection, SqlTransaction transaction, string saleModeKey, CancellationToken cancellationToken)
    {
        const string sql = "SELECT TOP 1 Id FROM dbo.SaleModes WHERE ModeKey = @ModeKey;";
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@ModeKey", saleModeKey.Trim());
        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is not Guid id)
        {
            throw new InvalidOperationException("Satis modu bulunamadi.");
        }

        return id;
    }

    private static async Task<bool> ListingExistsAsync(SqlConnection connection, SqlTransaction transaction, Guid listingId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT COUNT(*) FROM dbo.Listings WHERE Id = @ListingId;";
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@ListingId", listingId);
        return (int)await command.ExecuteScalarAsync(cancellationToken) > 0;
    }

    private static async Task<Guid> ResolveListingSellerUserIdAsync(SqlConnection connection, SqlTransaction transaction, Guid listingId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT TOP 1 SellerUserId FROM dbo.Listings WHERE Id = @ListingId;";
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@ListingId", listingId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is Guid id ? id : Guid.Empty;
    }

    private static async Task<Guid> FindListingConversationAsync(SqlConnection connection, SqlTransaction transaction, Guid listingId, Guid firstUserId, Guid secondUserId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP 1 c.Id
            FROM dbo.Conversations c
            INNER JOIN dbo.ConversationParticipants cp1 ON cp1.ConversationId = c.Id AND cp1.UserId = @FirstUserId
            INNER JOIN dbo.ConversationParticipants cp2 ON cp2.ConversationId = c.Id AND cp2.UserId = @SecondUserId
            WHERE c.ListingId = @ListingId
              AND c.ConversationType = N'listing';
            """;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@ListingId", listingId);
        command.Parameters.AddWithValue("@FirstUserId", firstUserId);
        command.Parameters.AddWithValue("@SecondUserId", secondUserId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is Guid id ? id : Guid.Empty;
    }

    private static async Task<bool> IsConversationParticipantAsync(SqlConnection connection, SqlTransaction transaction, Guid conversationId, Guid userId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM dbo.ConversationParticipants
            WHERE ConversationId = @ConversationId AND UserId = @UserId;
            """;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@ConversationId", conversationId);
        command.Parameters.AddWithValue("@UserId", userId);
        return (int)await command.ExecuteScalarAsync(cancellationToken) > 0;
    }

    private static async Task MarkConversationReadAsync(SqlConnection connection, Guid conversationId, string userName, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.ConversationParticipants
            SET LastReadAt = SYSUTCDATETIME()
            WHERE ConversationId = @ConversationId
              AND UserId =
              (
                  SELECT TOP 1 u.Id
                  FROM dbo.Users u
                  LEFT JOIN dbo.UserProfiles up ON up.UserId = u.Id
                  WHERE up.UserName = @UserName OR u.Email = @UserName
              );
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ConversationId", conversationId);
        command.Parameters.AddWithValue("@UserName", userName.Trim());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<Guid> ResolveOfferBuyerUserIdAsync(SqlConnection connection, SqlTransaction transaction, Guid offerId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT TOP 1 BuyerUserId FROM dbo.ListingOffers WHERE Id = @OfferId;";
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@OfferId", offerId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is Guid id ? id : Guid.Empty;
    }

    private static async Task<Guid> ResolveConversationRecipientUserIdAsync(SqlConnection connection, SqlTransaction transaction, Guid conversationId, Guid senderUserId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP 1 UserId
            FROM dbo.ConversationParticipants
            WHERE ConversationId = @ConversationId
              AND UserId <> @SenderUserId;
            """;
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@ConversationId", conversationId);
        command.Parameters.AddWithValue("@SenderUserId", senderUserId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is Guid id ? id : Guid.Empty;
    }

    private static async Task InsertNotificationAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        Guid userId,
        string notificationType,
        string title,
        string body,
        string? relatedEntityType,
        Guid? relatedEntityId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.Notifications
            (
                Id, UserId, NotificationType, Title, Body, RelatedEntityType, RelatedEntityId, IsRead, CreatedAt
            )
            VALUES
            (
                @Id, @UserId, @NotificationType, @Title, @Body, @RelatedEntityType, @RelatedEntityId, 0, SYSUTCDATETIME()
            );
            """;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@Id", Guid.NewGuid());
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@NotificationType", notificationType);
        command.Parameters.AddWithValue("@Title", title);
        command.Parameters.AddWithValue("@Body", body);
        command.Parameters.AddWithValue("@RelatedEntityType", string.IsNullOrWhiteSpace(relatedEntityType) ? DBNull.Value : relatedEntityType);
        command.Parameters.AddWithValue("@RelatedEntityId", relatedEntityId ?? (object)DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private sealed class AuctionRecord
    {
        public Guid Id { get; init; }
        public decimal StartPrice { get; init; }
        public decimal MinBidIncrement { get; init; }
        public decimal? CurrentBidAmount { get; init; }
        public Guid? CurrentWinnerUserId { get; init; }
        public DateTime StartsAt { get; init; }
        public DateTime EndsAt { get; init; }
        public int AutoExtendMinutes { get; init; }
        public string Status { get; init; } = string.Empty;
        public bool ResultProcessed { get; init; }
    }
}
