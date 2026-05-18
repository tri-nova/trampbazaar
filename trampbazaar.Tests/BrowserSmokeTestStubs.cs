using System.Net;
using System.Text.Json;
using trampbazaar.Shared.Contracts;

namespace trampbazaar.Tests;

internal static class BrowserSmokeTestStubs
{
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    public static StubApiServer CreateWebApiStub()
    {
        return StubApiServer.Start(request =>
        {
            var path = request.Url?.AbsolutePath ?? "/";
            return Task.FromResult((request.HttpMethod, path) switch
            {
                ("POST", "/api/auth/login") => StubApiResponse.Json(HttpStatusCode.OK, new AuthResponseDto
                {
                    IsSuccess = true,
                    UserName = "batu",
                    RoleName = "user",
                    AccessToken = "user-token"
                }),
                ("GET", "/api/dashboard") => StubApiResponse.Json(HttpStatusCode.OK, new DashboardResponse
                {
                    PlatformName = "TrampBazaar",
                    IsDataAvailable = true,
                    QuickStats =
                    [
                        new QuickStatDto { Label = "Aktif", Value = "42" }
                    ],
                    SaleModes =
                    [
                        new SaleModeDto
                        {
                            Key = "direct",
                            Name = "Dogrudan",
                            Description = "Hemen al",
                            Steps = ["Listele", "Sat"]
                        }
                    ],
                    Features =
                    [
                        new FeatureDto { Title = "Tek panel", Description = "Tum akislar tek panelde." }
                    ]
                }),
                ("GET", "/api/account") when HasBearer(request, "user-token") => StubApiResponse.Json(HttpStatusCode.OK, new UserAccountDashboardDto
                {
                    UserName = "batu",
                    AccountType = "premium",
                    ListingCount = 3,
                    ActiveListingCount = 2,
                    UnreadNotificationCount = 1,
                    PaymentCount = 1,
                    TotalPaidAmount = 499,
                    RecentPayments =
                    [
                        new UserPaymentDto
                        {
                            Id = Guid.NewGuid(),
                            PackageName = "Starter Paket",
                            PaymentType = "package",
                            PaymentStatus = "paid",
                            Amount = 499,
                            CurrencyCode = "TRY",
                            CreatedAt = DateTimeOffset.UtcNow
                        }
                    ]
                }),
                ("GET", "/api/account/profile") when HasBearer(request, "user-token") => StubApiResponse.Json(HttpStatusCode.OK, new UserAccountProfileDto
                {
                    UserName = "batu",
                    AccountType = "individual",
                    FullName = "Batu Yildiz",
                    FirstName = "Batu",
                    LastName = "Yildiz",
                    Email = "batu@example.com",
                    MobilePhone = "05050000000",
                    City = "Istanbul",
                    District = "Kadikoy",
                    EmailOptIn = true,
                    SmsOptIn = true,
                    BillingAddress = new UserBillingAddressDto
                    {
                        InvoiceType = "individual",
                        AddressTitle = "Merkez",
                        FullName = "Batu Yildiz",
                        Country = "Turkiye",
                        City = "Istanbul",
                        District = "Kadikoy",
                        PhoneNumber = "05050000000",
                        AddressLine = "Demo adres"
                    }
                }),
                _ => StubApiResponse.Json(HttpStatusCode.NotFound, new { error = "not found" })
            });
        });
    }

    public static StubApiServer CreateWebRegistrationApiStub()
    {
        return StubApiServer.Start(async request =>
        {
            var path = request.Url?.AbsolutePath ?? "/";
            return (request.HttpMethod, path) switch
            {
                ("POST", "/api/auth/register") => await ReadRegisterResponseAsync(request),
                ("GET", "/api/dashboard") => StubApiResponse.Json(HttpStatusCode.OK, new DashboardResponse
                {
                    PlatformName = "TrampBazaar",
                    IsDataAvailable = true
                }),
                _ => StubApiResponse.Json(HttpStatusCode.NotFound, new { error = "not found" })
            };
        });
    }

    public static StubApiServer CreateWebAccountProfileApiStub()
    {
        var profile = new UserAccountProfileDto
        {
            UserName = "batu",
            AccountType = "individual",
            FullName = "Batu Yildiz",
            FirstName = "Batu",
            LastName = "Yildiz",
            Email = "batu@example.com",
            MobilePhone = "05050000000",
            City = "Ankara",
            District = "Cankaya",
            BillingAddress = new UserBillingAddressDto
            {
                InvoiceType = "individual",
                AddressTitle = "Merkez",
                FullName = "Batu Yildiz",
                Country = "Turkiye",
                City = "Ankara",
                District = "Cankaya",
                PhoneNumber = "05050000000",
                AddressLine = "Demo adres"
            }
        };

        return StubApiServer.Start(async request =>
        {
            var path = request.Url?.AbsolutePath ?? "/";
            return (request.HttpMethod, path) switch
            {
                ("POST", "/api/auth/login") => StubApiResponse.Json(HttpStatusCode.OK, new AuthResponseDto
                {
                    IsSuccess = true,
                    UserName = "batu",
                    RoleName = "user",
                    AccessToken = "user-token"
                }),
                ("GET", "/api/account") when HasBearer(request, "user-token") => StubApiResponse.Json(HttpStatusCode.OK, new UserAccountDashboardDto
                {
                    UserName = "batu",
                    AccountType = "individual",
                    ListingCount = 2,
                    ActiveListingCount = 1,
                    UnreadNotificationCount = 1,
                    PaymentCount = 1,
                    TotalPaidAmount = 1499
                }),
                ("GET", "/api/account/profile") when HasBearer(request, "user-token") => StubApiResponse.Json(HttpStatusCode.OK, profile),
                ("PUT", "/api/account/profile") when HasBearer(request, "user-token") => await UpdateAccountProfileAsync(request, profile),
                _ => StubApiResponse.Json(HttpStatusCode.NotFound, new { error = "not found" })
            };
        });
    }

    public static StubApiServer CreateWebCustomerModulesApiStub()
    {
        var listingOneId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var listingTwoId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        var stockAlertId = Guid.Parse("21212121-2121-2121-2121-212121212121");
        var priceAlertId = Guid.Parse("23232323-2323-2323-2323-232323232323");

        var listings = new List<ListingDto>
        {
            new()
            {
                Id = listingOneId,
                Title = "Retro Kamera",
                Price = 3250,
                Currency = "TRY",
                SellerName = "Ayse Demir",
                Category = "Koleksiyon",
                SaleMode = "Dogrudan",
                Status = "published"
            },
            new()
            {
                Id = listingTwoId,
                Title = "Vintage Pikap",
                Price = 1500,
                Currency = "TRY",
                SellerName = "Ayse Demir",
                Category = "Hobi",
                SaleMode = "Acik Artirma",
                Status = "published"
            }
        };

        var favorites = new List<FavoriteListingDto>
        {
            new()
            {
                ListingId = listingOneId,
                Title = "Retro Kamera",
                Category = "Koleksiyon",
                SellerName = "Ayse Demir",
                Price = 3250,
                CurrencyCode = "TRY"
            }
        };

        var stockAlerts = new List<StockAlertDto>();
        var priceAlerts = new List<PriceAlertDto>();

        return StubApiServer.Start(request =>
        {
            var path = request.Url?.AbsolutePath ?? "/";
            return (request.HttpMethod, path) switch
            {
                ("POST", "/api/auth/login") => Task.FromResult(StubApiResponse.Json(HttpStatusCode.OK, new AuthResponseDto
                {
                    IsSuccess = true,
                    UserName = "batu",
                    RoleName = "user",
                    AccessToken = "user-token"
                })),
                ("GET", "/api/account") when HasBearer(request, "user-token") => Task.FromResult(StubApiResponse.Json(HttpStatusCode.OK, new UserAccountDashboardDto
                {
                    UserName = "batu",
                    AccountType = "individual"
                })),
                ("GET", "/api/account/orders") when HasBearer(request, "user-token") => Task.FromResult(StubApiResponse.Json(HttpStatusCode.OK, new[]
                {
                    new CustomerOrderDto
                    {
                        Id = Guid.NewGuid(),
                        OrderNumber = "TS0906263",
                        OrderStatus = "Teslim Edildi",
                        PaymentMethod = "Kredi Karti",
                        InstallmentCount = 3,
                        TotalAmount = 25000,
                        CurrencyCode = "TRY",
                        ItemCount = 1,
                        SummaryText = "Retro Kamera siparisi tamamlandi.",
                        OrderedAt = DateTimeOffset.UtcNow.AddDays(-14)
                    }
                })),
                ("GET", "/api/account/ledger") when HasBearer(request, "user-token") => Task.FromResult(StubApiResponse.Json(HttpStatusCode.OK, new AccountLedgerSummaryDto
                {
                    CurrentBalance = 91583.10m,
                    TotalDebit = 91733m,
                    TotalCredit = 149.90m,
                    Entries =
                    [
                        new AccountLedgerEntryDto
                        {
                            EntryDate = DateTimeOffset.UtcNow.AddDays(-14),
                            OrderNumber = "TS0906263",
                            Description = "Retro Kamera siparis borcu",
                            PaymentMethod = "Kredi Karti",
                            DebitAmount = 25000,
                            CreditAmount = 0,
                            BalanceAfter = 25000
                        }
                    ]
                })),
                ("POST", "/api/account/ledger/payments") when HasBearer(request, "user-token") => Task.FromResult(StubApiResponse.Json(HttpStatusCode.OK, new PaymentResultDto
                {
                    PaymentId = Guid.NewGuid(),
                    CheckoutUrl = "/PaymentSuccess?paymentId=44444444-4444-4444-4444-444444444444"
                })),
                ("GET", "/api/listings") => Task.FromResult(StubApiResponse.Json(HttpStatusCode.OK, listings)),
                ("GET", "/api/account/favorites") when HasBearer(request, "user-token") => Task.FromResult(StubApiResponse.Json(HttpStatusCode.OK, favorites)),
                ("POST", var favoriteAddPath) when favoriteAddPath.StartsWith("/api/account/favorites/", StringComparison.Ordinal) && HasBearer(request, "user-token") => AddFavoriteAsync(favoriteAddPath, listings, favorites),
                ("DELETE", var favoriteDeletePath) when favoriteDeletePath.StartsWith("/api/account/favorites/", StringComparison.Ordinal) && HasBearer(request, "user-token") => RemoveFavoriteAsync(favoriteDeletePath, favorites),
                ("GET", "/api/account/stock-alerts") when HasBearer(request, "user-token") => Task.FromResult(StubApiResponse.Json(HttpStatusCode.OK, stockAlerts)),
                ("POST", "/api/account/stock-alerts") when HasBearer(request, "user-token") => AddStockAlertAsync(request, listings, stockAlerts, stockAlertId),
                ("DELETE", var stockDeletePath) when stockDeletePath.StartsWith("/api/account/stock-alerts/", StringComparison.Ordinal) && HasBearer(request, "user-token") => RemoveStockAlertAsync(stockDeletePath, stockAlerts),
                ("GET", "/api/account/price-alerts") when HasBearer(request, "user-token") => Task.FromResult(StubApiResponse.Json(HttpStatusCode.OK, priceAlerts)),
                ("POST", "/api/account/price-alerts") when HasBearer(request, "user-token") => AddPriceAlertAsync(request, listings, priceAlerts, priceAlertId),
                ("DELETE", var priceDeletePath) when priceDeletePath.StartsWith("/api/account/price-alerts/", StringComparison.Ordinal) && HasBearer(request, "user-token") => RemovePriceAlertAsync(priceDeletePath, priceAlerts),
                _ => Task.FromResult(StubApiResponse.Json(HttpStatusCode.NotFound, new { error = "not found" }))
            };
        });
    }

    public static StubApiServer CreateWebPackagesApiStub()
    {
        return StubApiServer.Start(request =>
        {
            var path = request.Url?.AbsolutePath ?? "/";
            return Task.FromResult((request.HttpMethod, path) switch
            {
                ("GET", "/api/packages") => StubApiResponse.Json(HttpStatusCode.OK, new[]
                {
                    new PackageDto
                    {
                        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                        PackageType = "showcase",
                        Name = "Kurumsal Vitrin",
                        Price = 1499,
                        CurrencyCode = "TRY",
                        DurationDays = 30,
                        ListingQuota = 10
                    }
                }),
                _ => StubApiResponse.Json(HttpStatusCode.NotFound, new { error = "not found" })
            });
        });
    }

    public static StubApiServer CreateWebPurchaseApiStub(Guid paymentId)
    {
        var packageId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        return StubApiServer.Start(async request =>
        {
            var path = request.Url?.AbsolutePath ?? "/";
            return (request.HttpMethod, path) switch
            {
                ("POST", "/api/auth/login") => StubApiResponse.Json(HttpStatusCode.OK, new AuthResponseDto
                {
                    IsSuccess = true,
                    UserName = "batu",
                    RoleName = "user",
                    AccessToken = "user-token"
                }),
                ("GET", "/api/packages") => StubApiResponse.Json(HttpStatusCode.OK, new[]
                {
                    new PackageDto
                    {
                        Id = packageId,
                        PackageType = "listing",
                        Name = "Starter Paket",
                        Price = 499,
                        CurrencyCode = "TRY",
                        DurationDays = 30,
                        ListingQuota = 3
                    }
                }),
                ("POST", "/api/payments") when HasBearer(request, "user-token") => await ReadCreatePaymentResponseAsync(request, packageId, paymentId),
                _ => StubApiResponse.Json(HttpStatusCode.NotFound, new { error = "not found" })
            };
        });
    }

    public static StubApiServer CreateAdminApiStub()
    {
        return StubApiServer.Start(request =>
        {
            var path = request.Url?.AbsolutePath ?? "/";
            return Task.FromResult((request.HttpMethod, path) switch
            {
                ("POST", "/api/admin/auth/login") => StubApiResponse.Json(HttpStatusCode.OK, new AuthResponseDto
                {
                    IsSuccess = true,
                    UserName = "admin",
                    RoleName = "superadmin",
                    AccessToken = "admin-token"
                }),
                ("GET", "/api/admin/overview") when HasBearer(request, "admin-token") => StubApiResponse.Json(HttpStatusCode.OK, new AdminOverviewDto
                {
                    IsDataAvailable = true,
                    ActiveUsers = 12,
                    PublishedListings = 7,
                    OpenConversations = 4,
                    UnreadNotifications = 3,
                    Highlights =
                    [
                        new QuickStatDto { Label = "Aktif Kullanici", Value = "12" }
                    ]
                }),
                ("GET", "/api/admin/users") when HasBearer(request, "admin-token") => StubApiResponse.Json(HttpStatusCode.OK, new[]
                {
                    new AdminUserDto
                    {
                        Id = Guid.NewGuid(),
                        UserName = "batu",
                        Email = "batu@example.com",
                        AccountType = "individual",
                        Status = "active",
                        ListingCount = 3,
                        CreatedAt = DateTimeOffset.UtcNow
                    }
                }),
                _ => StubApiResponse.Json(HttpStatusCode.NotFound, new { error = "not found" })
            });
        });
    }

    public static StubApiServer CreateAdminModerationApiStub()
    {
        var user = new AdminUserDto
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            UserName = "batu",
            Email = "batu@example.com",
            AccountType = "individual",
            Status = "active",
            ListingCount = 3,
            CreatedAt = DateTimeOffset.UtcNow
        };

        return StubApiServer.Start(async request =>
        {
            var path = request.Url?.AbsolutePath ?? "/";
            return (request.HttpMethod, path) switch
            {
                ("POST", "/api/admin/auth/login") => StubApiResponse.Json(HttpStatusCode.OK, new AuthResponseDto
                {
                    IsSuccess = true,
                    UserName = "admin",
                    RoleName = "superadmin",
                    AccessToken = "admin-token"
                }),
                ("GET", "/api/admin/overview") when HasBearer(request, "admin-token") => StubApiResponse.Json(HttpStatusCode.OK, new AdminOverviewDto
                {
                    IsDataAvailable = true
                }),
                ("GET", "/api/admin/users") when HasBearer(request, "admin-token") => StubApiResponse.Json(HttpStatusCode.OK, new[] { user }),
                ("POST", var statusPath) when statusPath == $"/api/admin/users/{user.Id}/status" && HasBearer(request, "admin-token") => await UpdateUserStatusAsync(request, user),
                _ => StubApiResponse.Json(HttpStatusCode.NotFound, new { error = "not found" })
            };
        });
    }

    public static StubApiServer CreateAdminCategoryApiStub()
    {
        var categories = new List<AdminCategoryDto>
        {
            new()
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Name = "Elektronik",
                Slug = "elektronik",
                SortOrder = 1,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        return StubApiServer.Start(async request =>
        {
            var path = request.Url?.AbsolutePath ?? "/";
            return (request.HttpMethod, path) switch
            {
                ("POST", "/api/admin/auth/login") => StubApiResponse.Json(HttpStatusCode.OK, new AuthResponseDto
                {
                    IsSuccess = true,
                    UserName = "admin",
                    RoleName = "superadmin",
                    AccessToken = "admin-token"
                }),
                ("GET", "/api/admin/overview") when HasBearer(request, "admin-token") => StubApiResponse.Json(HttpStatusCode.OK, new AdminOverviewDto
                {
                    IsDataAvailable = true
                }),
                ("GET", "/api/admin/categories") when HasBearer(request, "admin-token") => StubApiResponse.Json(HttpStatusCode.OK, categories),
                ("POST", "/api/admin/categories") when HasBearer(request, "admin-token") => await CreateCategoryAsync(request, categories),
                _ => StubApiResponse.Json(HttpStatusCode.NotFound, new { error = "not found" })
            };
        });
    }

    public static StubApiServer CreateWebListingCreateApiStub(Guid listingId)
    {
        var categories = new[]
        {
            new CategoryDto
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Name = "Elektronik",
                Slug = "elektronik"
            }
        };

        var saleModes = new[]
        {
            new SaleModeDto
            {
                Key = "direct",
                Name = "Dogrudan",
                Description = "Aninda al",
                Steps = ["Yayinla", "Sat"]
            },
            new SaleModeDto
            {
                Key = "auction",
                Name = "Acik artirma",
                Description = "Teklif topla",
                Steps = ["Yayinla", "Teklif al", "Bitir"]
            }
        };

        ListingDto? createdListing = null;

        return StubApiServer.Start(async request =>
        {
            var path = request.Url?.AbsolutePath ?? "/";
            return (request.HttpMethod, path) switch
            {
                ("POST", "/api/auth/login") => StubApiResponse.Json(HttpStatusCode.OK, new AuthResponseDto
                {
                    IsSuccess = true,
                    UserName = "batu",
                    RoleName = "user",
                    AccessToken = "user-token"
                }),
                ("GET", "/api/categories") => StubApiResponse.Json(HttpStatusCode.OK, categories),
                ("GET", "/api/sale-modes") => StubApiResponse.Json(HttpStatusCode.OK, saleModes),
                ("POST", "/api/listings") when HasBearer(request, "user-token") => await CreateListingAsync(request, listingId, created => createdListing = created),
                ("GET", var listingPath) when listingPath == $"/api/listings/{listingId}" && createdListing is not null => StubApiResponse.Json(HttpStatusCode.OK, createdListing),
                ("GET", var offerPath) when offerPath == $"/api/listings/{listingId}/offers" => StubApiResponse.Json(HttpStatusCode.OK, Array.Empty<ListingOfferDto>()),
                _ => StubApiResponse.Json(HttpStatusCode.NotFound, new { error = "not found" })
            };
        });
    }

    public static StubApiServer CreateWebMarketplaceWorkflowApiStub(Guid listingId, Guid conversationId)
    {
        var listing = new ListingDto
        {
            Id = listingId,
            Title = "Retro Kamera",
            Description = "Temiz urun, sorunsuz.",
            Category = "Elektronik",
            SaleMode = "direct",
            Price = 3200,
            Currency = "TRY",
            SellerName = "ayse",
            Status = "published",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2)
        };

        var saleModes = new[]
        {
            new SaleModeDto
            {
                Key = "direct",
                Name = "Dogrudan",
                Description = "Hemen satin al veya teklif ver.",
                Steps = ["Incele", "Teklif ver", "Tamamla"]
            }
        };

        var offers = new List<ListingOfferDto>();
        var notifications = new List<NotificationDto>
        {
            new()
            {
                Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                NotificationType = "offer",
                Title = "Yeni teklif",
                Body = "Ilaniniz icin yeni teklif var.",
                IsRead = false,
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10)
            }
        };

        var messages = new List<MessageDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                SenderUserName = "ayse",
                MessageText = "Merhaba, urun hala satista mi?",
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-20),
                IsMine = false
            }
        };

        return StubApiServer.Start(async request =>
        {
            var path = request.Url?.AbsolutePath ?? "/";
            return (request.HttpMethod, path) switch
            {
                ("POST", "/api/auth/login") => StubApiResponse.Json(HttpStatusCode.OK, new AuthResponseDto
                {
                    IsSuccess = true,
                    UserName = "batu",
                    RoleName = "user",
                    AccessToken = "user-token"
                }),
                ("GET", var listingPath) when listingPath == $"/api/listings/{listingId}" => StubApiResponse.Json(HttpStatusCode.OK, listing),
                ("GET", "/api/sale-modes") => StubApiResponse.Json(HttpStatusCode.OK, saleModes),
                ("GET", var offerPath) when offerPath == $"/api/listings/{listingId}/offers" => StubApiResponse.Json(HttpStatusCode.OK, offers),
                ("POST", var offerPath) when offerPath == $"/api/listings/{listingId}/offers" && HasBearer(request, "user-token") => await CreateOfferAsync(request, listingId, offers),
                ("POST", var conversationPath) when conversationPath == $"/api/listings/{listingId}/conversations" && HasBearer(request, "user-token") => StubApiResponse.Json(HttpStatusCode.OK, BuildConversationDetail(conversationId, listingId, messages)),
                ("GET", "/api/conversations") when HasBearer(request, "user-token") => StubApiResponse.Json(HttpStatusCode.OK, new[]
                {
                    new ConversationSummaryDto
                    {
                        Id = conversationId,
                        ListingId = listingId,
                        ConversationType = "listing",
                        Title = "Retro Kamera",
                        CounterpartyUserName = "ayse",
                        LastMessagePreview = messages.Last().MessageText,
                        LastMessageAt = messages.Last().CreatedAt,
                        UnreadCount = 1
                    }
                }),
                ("GET", var detailPath) when detailPath == $"/api/conversations/{conversationId}" && HasBearer(request, "user-token") => StubApiResponse.Json(HttpStatusCode.OK, BuildConversationDetail(conversationId, listingId, messages)),
                ("POST", var messagePath) when messagePath == $"/api/conversations/{conversationId}/messages" && HasBearer(request, "user-token") => await SendMessageAsync(request, conversationId, messages),
                ("GET", "/api/notifications") when HasBearer(request, "user-token") => StubApiResponse.Json(HttpStatusCode.OK, notifications),
                ("POST", var readPath) when readPath == $"/api/notifications/{notifications[0].Id}/read" && HasBearer(request, "user-token") => MarkNotificationRead(notifications[0]),
                ("POST", "/api/complaints") when HasBearer(request, "user-token") => await SubmitComplaintAsync(request, notifications),
                _ => StubApiResponse.Json(HttpStatusCode.NotFound, new { error = "not found" })
            };
        });
    }

    public static StubApiServer CreateWebAuctionWorkflowApiStub(Guid listingId)
    {
        var auctionId = Guid.Parse("88888888-8888-8888-8888-888888888888");
        var listing = new ListingDto
        {
            Id = listingId,
            Title = "Vintage Pikap",
            Description = "Calisir durumda.",
            Category = "Elektronik",
            SaleMode = "auction",
            Price = 1500,
            Currency = "TRY",
            SellerName = "ayse",
            Status = "published",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        var saleModes = new[]
        {
            new SaleModeDto
            {
                Key = "auction",
                Name = "Acik artirma",
                Description = "Teklif topla",
                Steps = ["Baslat", "Teklif al", "Kapat"]
            }
        };

        var auction = new AuctionDto
        {
            Id = auctionId,
            ListingId = listingId,
            StartPrice = 1500,
            MinBidIncrement = 100,
            CurrentBidAmount = 1600,
            CurrentWinnerUserName = "mehmet",
            StartsAt = DateTimeOffset.UtcNow.AddHours(-2),
            EndsAt = DateTimeOffset.UtcNow.AddHours(5),
            AutoExtendMinutes = 5,
            AuctionStatus = "active",
            ResultProcessed = false
        };

        var bids = new List<AuctionBidDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                AuctionId = auctionId,
                BidderUserName = "mehmet",
                BidAmount = 1600,
                BidStatus = "leading",
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-30)
            }
        };

        return StubApiServer.Start(async request =>
        {
            var path = request.Url?.AbsolutePath ?? "/";
            return (request.HttpMethod, path) switch
            {
                ("POST", "/api/auth/login") => StubApiResponse.Json(HttpStatusCode.OK, new AuthResponseDto
                {
                    IsSuccess = true,
                    UserName = "batu",
                    RoleName = "user",
                    AccessToken = "user-token"
                }),
                ("GET", var listingPath) when listingPath == $"/api/listings/{listingId}" => StubApiResponse.Json(HttpStatusCode.OK, listing),
                ("GET", "/api/sale-modes") => StubApiResponse.Json(HttpStatusCode.OK, saleModes),
                ("GET", var auctionPath) when auctionPath == $"/api/listings/{listingId}/auction" => StubApiResponse.Json(HttpStatusCode.OK, auction),
                ("GET", var bidsPath) when bidsPath == $"/api/listings/{listingId}/auction/bids" => StubApiResponse.Json(HttpStatusCode.OK, bids),
                ("POST", var bidsPath) when bidsPath == $"/api/listings/{listingId}/auction/bids" && HasBearer(request, "user-token") => await CreateBidAsync(request, auctionId, bids),
                _ => StubApiResponse.Json(HttpStatusCode.NotFound, new { error = "not found" })
            };
        });
    }

    public static StubApiServer CreateAdminOperationsApiStub()
    {
        var listing = new AdminListingDto
        {
            Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            Title = "Retro Kamera",
            SellerUserName = "batu",
            Category = "Elektronik",
            SaleMode = "direct",
            Price = 3200,
            Currency = "TRY",
            Status = "published",
            IsFeatured = false,
            ViewCount = 42,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-5)
        };

        var packages = new List<AdminPackageDto>
        {
            new()
            {
                Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                PackageType = "listing",
                Name = "Starter Paket",
                Price = 499,
                CurrencyCode = "TRY",
                DurationDays = 30,
                ListingQuota = 3,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-10)
            }
        };

        var complaints = new List<AdminComplaintDto>
        {
            new()
            {
                Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                ReporterUserName = "batu",
                TargetEntityType = "listing",
                TargetEntityId = Guid.NewGuid(),
                Subject = "Yaniltici ilan",
                Description = "Aciklama ile urun farkli.",
                ComplaintStatus = "open",
                AssignedAdminUserName = "admin",
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
                UpdatedAt = DateTimeOffset.UtcNow.AddHours(-4)
            }
        };

        var payments = new AdminPaymentsDashboardDto
        {
            TotalPaidAmount = 1499,
            PaidCount = 2,
            PendingCount = 1,
            Payments =
            [
                new AdminPaymentDto
                {
                    Id = Guid.NewGuid(),
                    UserName = "batu",
                    PaymentType = "package",
                    PaymentStatus = "paid",
                    Amount = 499,
                    CurrencyCode = "TRY",
                    PackageName = "Starter Paket",
                    ProviderName = "stripe",
                    PaidAt = DateTimeOffset.UtcNow.AddHours(-6),
                    CreatedAt = DateTimeOffset.UtcNow.AddHours(-7)
                }
            ]
        };

        var notifications = new[]
        {
            new NotificationDto
            {
                Id = Guid.NewGuid(),
                NotificationType = "moderation",
                Title = "Yeni sikayet",
                Body = "Incelenmesi gereken yeni sikayet var.",
                IsRead = false,
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-30)
            }
        };

        return StubApiServer.Start(async request =>
        {
            var path = request.Url?.AbsolutePath ?? "/";
            return (request.HttpMethod, path) switch
            {
                ("POST", "/api/admin/auth/login") => StubApiResponse.Json(HttpStatusCode.OK, new AuthResponseDto
                {
                    IsSuccess = true,
                    UserName = "admin",
                    RoleName = "superadmin",
                    AccessToken = "admin-token"
                }),
                ("GET", "/api/admin/overview") when HasBearer(request, "admin-token") => StubApiResponse.Json(HttpStatusCode.OK, new AdminOverviewDto
                {
                    IsDataAvailable = true,
                    ActiveUsers = 8,
                    PublishedListings = 4,
                    OpenConversations = 2,
                    UnreadNotifications = notifications.Length
                }),
                ("GET", "/api/admin/listings") when HasBearer(request, "admin-token") => StubApiResponse.Json(HttpStatusCode.OK, new[] { listing }),
                ("POST", var listingStatusPath) when listingStatusPath == $"/api/admin/listings/{listing.Id}/status" && HasBearer(request, "admin-token") => await UpdateListingStatusAsync(request, listing),
                ("GET", "/api/admin/packages") when HasBearer(request, "admin-token") => StubApiResponse.Json(HttpStatusCode.OK, packages),
                ("POST", "/api/admin/packages") when HasBearer(request, "admin-token") => await CreatePackageAsync(request, packages),
                ("POST", var packageStatusPath) when packageStatusPath == $"/api/admin/packages/{packages[0].Id}/status" && HasBearer(request, "admin-token") => await UpdatePackageStatusAsync(request, packages[0]),
                ("GET", "/api/admin/complaints") when HasBearer(request, "admin-token") => StubApiResponse.Json(HttpStatusCode.OK, complaints),
                ("POST", var complaintStatusPath) when complaintStatusPath == $"/api/admin/complaints/{complaints[0].Id}/status" && HasBearer(request, "admin-token") => await UpdateComplaintStatusAsync(request, complaints[0]),
                ("GET", "/api/admin/payments") when HasBearer(request, "admin-token") => StubApiResponse.Json(HttpStatusCode.OK, payments),
                ("GET", "/api/admin/notifications") when HasBearer(request, "admin-token") => StubApiResponse.Json(HttpStatusCode.OK, notifications),
                _ => StubApiResponse.Json(HttpStatusCode.NotFound, new { error = "not found" })
            };
        });
    }

    private static async Task<StubApiResponse> ReadCreatePaymentResponseAsync(HttpListenerRequest request, Guid expectedPackageId, Guid paymentId)
    {
        var payload = await JsonSerializer.DeserializeAsync<CreatePaymentRequest>(request.InputStream, WebJsonOptions, CancellationToken.None);
        if (payload is null || payload.PackageId != expectedPackageId)
        {
            return StubApiResponse.Json(HttpStatusCode.BadRequest, new { error = "invalid package" });
        }

        return StubApiResponse.Json(HttpStatusCode.OK, new PaymentResultDto
        {
            PaymentId = paymentId,
            PaymentStatus = "pending",
            Amount = 499,
            CurrencyCode = "TRY",
            Message = "checkout created",
            ProviderName = "stripe",
            CheckoutUrl = $"/PaymentSuccess?paymentId={paymentId}"
        });
    }

    private static async Task<StubApiResponse> CreateListingAsync(HttpListenerRequest request, Guid listingId, Action<ListingDto> setCreatedListing)
    {
        var payload = await JsonSerializer.DeserializeAsync<CreateListingRequest>(request.InputStream, WebJsonOptions, CancellationToken.None);
        if (payload is null || string.IsNullOrWhiteSpace(payload.Title))
        {
            return StubApiResponse.Json(HttpStatusCode.BadRequest, new { error = "invalid listing" });
        }

        var listing = new ListingDto
        {
            Id = listingId,
            Title = payload.Title,
            Description = payload.Description,
            Category = payload.CategorySlug,
            SaleMode = payload.SaleModeKey,
            Price = payload.Price,
            Currency = "TRY",
            SellerName = payload.SellerName,
            Status = "published",
            CreatedAt = DateTimeOffset.UtcNow
        };

        setCreatedListing(listing);
        return StubApiResponse.Json(HttpStatusCode.Created, listing);
    }

    private static async Task<StubApiResponse> CreateOfferAsync(HttpListenerRequest request, Guid listingId, List<ListingOfferDto> offers)
    {
        var payload = await JsonSerializer.DeserializeAsync<CreateListingOfferRequest>(request.InputStream, WebJsonOptions, CancellationToken.None);
        if (payload is null || payload.OfferedPrice <= 0)
        {
            return StubApiResponse.Json(HttpStatusCode.BadRequest, new { error = "invalid offer" });
        }

        var offer = new ListingOfferDto
        {
            Id = Guid.NewGuid(),
            ListingId = listingId,
            BuyerName = payload.BuyerName,
            OfferedPrice = payload.OfferedPrice,
            Currency = "TRY",
            OfferNote = payload.OfferNote,
            Status = "pending",
            CreatedAt = DateTimeOffset.UtcNow
        };

        offers.Add(offer);
        return StubApiResponse.Json(HttpStatusCode.Created, offer);
    }

    private static async Task<StubApiResponse> CreateBidAsync(HttpListenerRequest request, Guid auctionId, List<AuctionBidDto> bids)
    {
        var payload = await JsonSerializer.DeserializeAsync<CreateAuctionBidRequest>(request.InputStream, WebJsonOptions, CancellationToken.None);
        if (payload is null || payload.BidAmount <= 0)
        {
            return StubApiResponse.Json(HttpStatusCode.BadRequest, new { error = "invalid bid" });
        }

        var bid = new AuctionBidDto
        {
            Id = Guid.NewGuid(),
            AuctionId = auctionId,
            BidderUserName = payload.BidderUserName,
            BidAmount = payload.BidAmount,
            BidStatus = "leading",
            CreatedAt = DateTimeOffset.UtcNow
        };

        bids.Add(bid);
        return StubApiResponse.Json(HttpStatusCode.Created, bid);
    }

    private static async Task<StubApiResponse> SendMessageAsync(HttpListenerRequest request, Guid conversationId, List<MessageDto> messages)
    {
        var payload = await JsonSerializer.DeserializeAsync<SendMessageRequest>(request.InputStream, WebJsonOptions, CancellationToken.None);
        if (payload is null || string.IsNullOrWhiteSpace(payload.MessageText))
        {
            return StubApiResponse.Json(HttpStatusCode.BadRequest, new { error = "invalid message" });
        }

        var message = new MessageDto
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderUserName = payload.SenderUserName,
            MessageText = payload.MessageText,
            CreatedAt = DateTimeOffset.UtcNow,
            IsMine = true
        };

        messages.Add(message);
        return StubApiResponse.Json(HttpStatusCode.OK, message);
    }

    private static StubApiResponse MarkNotificationRead(NotificationDto notification)
    {
        notification.IsRead = true;
        return StubApiResponse.Json(HttpStatusCode.OK, new { ok = true });
    }

    private static async Task<StubApiResponse> SubmitComplaintAsync(HttpListenerRequest request, List<NotificationDto> notifications)
    {
        var payload = await JsonSerializer.DeserializeAsync<CreateComplaintRequest>(request.InputStream, WebJsonOptions, CancellationToken.None);
        if (payload is null || payload.TargetEntityId == Guid.Empty || string.IsNullOrWhiteSpace(payload.Subject))
        {
            return StubApiResponse.Json(HttpStatusCode.BadRequest, new { error = "invalid complaint" });
        }

        notifications.Add(new NotificationDto
        {
            Id = Guid.NewGuid(),
            NotificationType = "complaint",
            Title = "Sikayet alindi",
            Body = payload.Subject,
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        });

        return StubApiResponse.Json(HttpStatusCode.OK, new ComplaintResultDto
        {
            ComplaintId = Guid.NewGuid(),
            ComplaintStatus = "open",
            Message = "Sikayet kaydedildi."
        });
    }

    private static ConversationDetailDto BuildConversationDetail(Guid conversationId, Guid listingId, IReadOnlyList<MessageDto> messages)
    {
        return new ConversationDetailDto
        {
            Id = conversationId,
            ListingId = listingId,
            ConversationType = "listing",
            Title = "Retro Kamera",
            CounterpartyUserName = "ayse",
            Messages = messages.ToArray()
        };
    }

    private static async Task<StubApiResponse> UpdateUserStatusAsync(HttpListenerRequest request, AdminUserDto user)
    {
        var payload = await JsonSerializer.DeserializeAsync<AdminUserStatusUpdateRequest>(request.InputStream, WebJsonOptions, CancellationToken.None);
        if (payload is null || string.IsNullOrWhiteSpace(payload.Status))
        {
            return StubApiResponse.Json(HttpStatusCode.BadRequest, new { error = "status required" });
        }

        user.Status = payload.Status;
        return StubApiResponse.Json(HttpStatusCode.OK, new { ok = true });
    }

    private static async Task<StubApiResponse> CreateCategoryAsync(HttpListenerRequest request, List<AdminCategoryDto> categories)
    {
        var payload = await JsonSerializer.DeserializeAsync<AdminCategoryUpsertRequest>(request.InputStream, WebJsonOptions, CancellationToken.None);
        if (payload is null || string.IsNullOrWhiteSpace(payload.Name) || string.IsNullOrWhiteSpace(payload.Slug))
        {
            return StubApiResponse.Json(HttpStatusCode.BadRequest, new { error = "invalid category" });
        }

        var category = new AdminCategoryDto
        {
            Id = Guid.NewGuid(),
            ParentCategoryId = payload.ParentCategoryId,
            Name = payload.Name,
            Slug = payload.Slug,
            SortOrder = payload.SortOrder,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        categories.Add(category);
        return StubApiResponse.Json(HttpStatusCode.Created, category);
    }

    private static async Task<StubApiResponse> UpdateListingStatusAsync(HttpListenerRequest request, AdminListingDto listing)
    {
        var payload = await JsonSerializer.DeserializeAsync<AdminListingStatusUpdateRequest>(request.InputStream, WebJsonOptions, CancellationToken.None);
        if (payload is null || string.IsNullOrWhiteSpace(payload.Status))
        {
            return StubApiResponse.Json(HttpStatusCode.BadRequest, new { error = "status required" });
        }

        listing.Status = payload.Status;
        return StubApiResponse.Json(HttpStatusCode.OK, new { ok = true });
    }

    private static async Task<StubApiResponse> CreatePackageAsync(HttpListenerRequest request, List<AdminPackageDto> packages)
    {
        var payload = await JsonSerializer.DeserializeAsync<AdminPackageUpsertRequest>(request.InputStream, WebJsonOptions, CancellationToken.None);
        if (payload is null || string.IsNullOrWhiteSpace(payload.Name) || string.IsNullOrWhiteSpace(payload.PackageType))
        {
            return StubApiResponse.Json(HttpStatusCode.BadRequest, new { error = "invalid package" });
        }

        packages.Add(new AdminPackageDto
        {
            Id = Guid.NewGuid(),
            PackageType = payload.PackageType,
            Name = payload.Name,
            Price = payload.Price,
            CurrencyCode = "TRY",
            DurationDays = payload.DurationDays,
            ListingQuota = payload.ListingQuota,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        });

        return StubApiResponse.Json(HttpStatusCode.Created, packages.Last());
    }

    private static async Task<StubApiResponse> UpdatePackageStatusAsync(HttpListenerRequest request, AdminPackageDto package)
    {
        var payload = await JsonSerializer.DeserializeAsync<AdminPackageStatusUpdateRequest>(request.InputStream, WebJsonOptions, CancellationToken.None);
        package.IsActive = payload?.IsActive ?? package.IsActive;
        return StubApiResponse.Json(HttpStatusCode.OK, new { ok = true });
    }

    private static async Task<StubApiResponse> UpdateComplaintStatusAsync(HttpListenerRequest request, AdminComplaintDto complaint)
    {
        var payload = await JsonSerializer.DeserializeAsync<AdminComplaintStatusUpdateRequest>(request.InputStream, WebJsonOptions, CancellationToken.None);
        if (payload is null || string.IsNullOrWhiteSpace(payload.Status))
        {
            return StubApiResponse.Json(HttpStatusCode.BadRequest, new { error = "status required" });
        }

        complaint.ComplaintStatus = payload.Status;
        complaint.AssignedAdminUserName = payload.AssignedAdminUserName ?? complaint.AssignedAdminUserName;
        complaint.UpdatedAt = DateTimeOffset.UtcNow;
        return StubApiResponse.Json(HttpStatusCode.OK, new { ok = true });
    }

    private static async Task<StubApiResponse> ReadRegisterResponseAsync(HttpListenerRequest request)
    {
        var payload = await JsonSerializer.DeserializeAsync<RegisterRequestDto>(request.InputStream, WebJsonOptions, CancellationToken.None);
        if (payload is null ||
            string.IsNullOrWhiteSpace(payload.FullName) ||
            string.IsNullOrWhiteSpace(payload.UserName) ||
            string.IsNullOrWhiteSpace(payload.Email) ||
            string.IsNullOrWhiteSpace(payload.Password))
        {
            return StubApiResponse.Json(HttpStatusCode.BadRequest, new AuthResponseDto
            {
                IsSuccess = false,
                Message = "Tum alanlar zorunludur."
            });
        }

        return StubApiResponse.Json(HttpStatusCode.OK, new AuthResponseDto
        {
            IsSuccess = true,
            Message = "Kayit basarili.",
            UserName = payload.UserName,
            RoleName = payload.AccountType == "corporate" ? "CorporateUser" : "User",
            AccessToken = "new-user-token"
        });
    }

    private static async Task<StubApiResponse> UpdateAccountProfileAsync(HttpListenerRequest request, UserAccountProfileDto profile)
    {
        var payload = await JsonSerializer.DeserializeAsync<UpdateUserAccountProfileRequest>(request.InputStream, WebJsonOptions, CancellationToken.None);
        if (payload is null)
        {
            return StubApiResponse.Json(HttpStatusCode.BadRequest, new { error = "invalid profile" });
        }

        profile.FirstName = payload.FirstName;
        profile.LastName = payload.LastName;
        profile.FullName = $"{payload.FirstName} {payload.LastName}".Trim();
        profile.Email = payload.Email;
        profile.MobilePhone = payload.MobilePhone;
        profile.WorkPhone = payload.WorkPhone;
        profile.NationalId = payload.NationalId;
        profile.IsForeignCitizen = payload.IsForeignCitizen;
        profile.BirthDate = payload.BirthDate;
        profile.Gender = payload.Gender;
        profile.AddressLine = payload.AddressLine;
        profile.PostalCode = payload.PostalCode;
        profile.City = payload.City;
        profile.District = payload.District;
        profile.EmailOptIn = payload.EmailOptIn;
        profile.SmsOptIn = payload.SmsOptIn;
        profile.PhoneOptIn = payload.PhoneOptIn;

        return StubApiResponse.Json(HttpStatusCode.OK, profile);
    }

    private static Task<StubApiResponse> AddFavoriteAsync(string path, IReadOnlyList<ListingDto> listings, List<FavoriteListingDto> favorites)
    {
        var listingId = Guid.Parse(path.Split('/')[^1]);
        var listing = listings.Single(x => x.Id == listingId);
        if (favorites.All(x => x.ListingId != listingId))
        {
            favorites.Add(new FavoriteListingDto
            {
                ListingId = listingId,
                Title = listing.Title,
                Category = listing.Category,
                SellerName = listing.SellerName,
                SaleMode = listing.SaleMode,
                ListingStatus = listing.Status,
                FavoritedAt = DateTimeOffset.UtcNow,
                Price = listing.Price,
                CurrencyCode = listing.Currency
            });
        }

        return Task.FromResult(StubApiResponse.Json(HttpStatusCode.OK, favorites.Single(x => x.ListingId == listingId)));
    }

    private static Task<StubApiResponse> RemoveFavoriteAsync(string path, List<FavoriteListingDto> favorites)
    {
        var listingId = Guid.Parse(path.Split('/')[^1]);
        favorites.RemoveAll(x => x.ListingId == listingId);
        return Task.FromResult(StubApiResponse.Json(HttpStatusCode.NoContent, new { }));
    }

    private static async Task<StubApiResponse> AddStockAlertAsync(HttpListenerRequest request, IReadOnlyList<ListingDto> listings, List<StockAlertDto> stockAlerts, Guid fallbackId)
    {
        var payload = await JsonSerializer.DeserializeAsync<AddStockAlertRequest>(request.InputStream, WebJsonOptions);
        var listing = listings.Single(x => x.Id == payload!.ListingId);
        var created = new StockAlertDto
        {
            Id = fallbackId == Guid.Empty ? Guid.NewGuid() : fallbackId,
            ListingId = listing.Id,
            ListingTitle = listing.Title,
            SellerName = listing.SellerName,
            Note = payload.Note ?? string.Empty,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        stockAlerts.Add(created);
        return StubApiResponse.Json(HttpStatusCode.OK, created);
    }

    private static Task<StubApiResponse> RemoveStockAlertAsync(string path, List<StockAlertDto> stockAlerts)
    {
        var alertId = Guid.Parse(path.Split('/')[^1]);
        stockAlerts.RemoveAll(x => x.Id == alertId);
        return Task.FromResult(StubApiResponse.Json(HttpStatusCode.NoContent, new { }));
    }

    private static async Task<StubApiResponse> AddPriceAlertAsync(HttpListenerRequest request, IReadOnlyList<ListingDto> listings, List<PriceAlertDto> priceAlerts, Guid fallbackId)
    {
        var payload = await JsonSerializer.DeserializeAsync<AddPriceAlertRequest>(request.InputStream, WebJsonOptions);
        var listing = listings.Single(x => x.Id == payload!.ListingId);
        var created = new PriceAlertDto
        {
            Id = fallbackId == Guid.Empty ? Guid.NewGuid() : fallbackId,
            ListingId = listing.Id,
            ListingTitle = listing.Title,
            SellerName = listing.SellerName,
            TargetPrice = payload.TargetPrice,
            CurrentPrice = listing.Price,
            CurrencyCode = listing.Currency,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        priceAlerts.Add(created);
        return StubApiResponse.Json(HttpStatusCode.OK, created);
    }

    private static Task<StubApiResponse> RemovePriceAlertAsync(string path, List<PriceAlertDto> priceAlerts)
    {
        var alertId = Guid.Parse(path.Split('/')[^1]);
        priceAlerts.RemoveAll(x => x.Id == alertId);
        return Task.FromResult(StubApiResponse.Json(HttpStatusCode.NoContent, new { }));
    }

    private static bool HasBearer(HttpListenerRequest request, string token)
        => string.Equals(request.Headers["Authorization"], $"Bearer {token}", StringComparison.Ordinal);
}
