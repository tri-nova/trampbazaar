using System.Net.Http.Json;
using trampbazaar.Models;
using trampbazaar.Shared.Api;
using trampbazaar.Shared.Contracts;

namespace trampbazaar.Services;

public sealed class ApiMarketplaceDataService(HttpClient httpClient, SessionStateService sessionStateService) : IMarketplaceDataService
{
    public async Task<MarketplaceSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var saleModes = await GetSaleModesAsync(cancellationToken);
        var listings = await GetListingsAsync(new ListingFilter(), cancellationToken);

        return new MarketplaceSnapshot
        {
            UserName = sessionStateService.UserName,
            HeroTitle = "TrampBazaar canli API modunda calisiyor.",
            HeroSubtitle = "Kullanici ekranlari artik ASP.NET Core backend'e hazir.",
            QuickStats =
            [
                new QuickStat { Value = listings.Count.ToString(), Label = "aktif ilan" },
                new QuickStat { Value = saleModes.Count.ToString(), Label = "satis modu" },
                new QuickStat { Value = sessionStateService.IsAuthenticated ? "Acik" : "Kapali", Label = "oturum" },
                new QuickStat { Value = "SQL", Label = "veri kaynagi" }
            ],
            SaleModes = saleModes.Select(MapSaleMode).ToList(),
            SharedFlow =
            [
                new FlowStage { Title = "Login", Caption = "Gercek JWT akisi sonraki adim" },
                new FlowStage { Title = "Listings", Caption = "Canli API listeleme" },
                new FlowStage { Title = "Detail", Caption = "Ilan detay ekrani" },
                new FlowStage { Title = "Create", Caption = "Ilan olusturma" },
                new FlowStage { Title = "Messages", Caption = "Mesaj kutusu sonraki faz" },
                new FlowStage { Title = "Admin", Caption = "Web panel sonraki faz" }
            ],
            FeatureCards =
            [
                new FeatureCard { Title = "REST API", Description = "ASP.NET Core endpointleri ile veri akisi." },
                new FeatureCard { Title = "SQL Server", Description = "Kalici veri tabani altyapisi." }
            ],
            FeaturedProducts = listings.Take(5).Select(MapProductCard).ToList()
        };
    }

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<List<CategoryDto>>(ApiRoutes.Categories, cancellationToken) ?? [];

    public async Task<IReadOnlyList<SaleModeDto>> GetSaleModesAsync(CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<List<SaleModeDto>>(ApiRoutes.SaleModes, cancellationToken) ?? [];

    public async Task<IReadOnlyList<PackageDto>> GetPackagesAsync(CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<List<PackageDto>>(ApiRoutes.Packages, cancellationToken) ?? [];

    public async Task<IReadOnlyList<ListingDto>> GetListingsAsync(ListingFilter filter, CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(filter.CategorySlug))
        {
            query.Add($"category={Uri.EscapeDataString(filter.CategorySlug)}");
        }

        if (!string.IsNullOrWhiteSpace(filter.SaleModeKey))
        {
            query.Add($"saleMode={Uri.EscapeDataString(filter.SaleModeKey)}");
        }

        var route = ApiRoutes.Listings;
        if (query.Count > 0)
        {
            route = $"{route}?{string.Join("&", query)}";
        }

        return await httpClient.GetFromJsonAsync<List<ListingDto>>(route, cancellationToken) ?? [];
    }

    public async Task<ListingDetailState> GetListingDetailAsync(Guid listingId, CancellationToken cancellationToken = default)
    {
        var listing = await httpClient.GetFromJsonAsync<ListingDto>(ApiRoutes.ListingById(listingId), cancellationToken);
        var saleModes = await GetSaleModesAsync(cancellationToken);
        var offers = await GetListingOffersAsync(listingId, cancellationToken);
        AuctionDto? auction = null;
        IReadOnlyList<AuctionBidDto> auctionBids = [];
        if (string.Equals(listing?.SaleMode, "auction", StringComparison.OrdinalIgnoreCase))
        {
            auction = await httpClient.GetFromJsonAsync<AuctionDto>(ApiRoutes.ListingAuction(listingId), cancellationToken);
            auctionBids = await GetAuctionBidsAsync(listingId, cancellationToken);
        }

        return new ListingDetailState
        {
            Listing = listing,
            SaleMode = listing is null ? null : saleModes.FirstOrDefault(x => x.Key == listing.SaleMode),
            Offers = offers,
            Auction = auction,
            AuctionBids = auctionBids
        };
    }

    public async Task<IReadOnlyList<ListingOfferDto>> GetListingOffersAsync(Guid listingId, CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<List<ListingOfferDto>>(ApiRoutes.ListingOffers(listingId), cancellationToken) ?? [];

    public async Task<ListingOfferDto> CreateListingOfferAsync(Guid listingId, ListingOfferFormModel form, CancellationToken cancellationToken = default)
    {
        var request = new CreateListingOfferRequest
        {
            BuyerName = sessionStateService.UserName,
            OfferedPrice = form.OfferedPrice,
            OfferNote = form.OfferNote
        };

        using var response = await httpClient.PostAsJsonAsync(ApiRoutes.ListingOffers(listingId), request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ListingOfferDto>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Teklif olusturulamadi.");
    }

    public async Task<IReadOnlyList<AuctionBidDto>> GetAuctionBidsAsync(Guid listingId, CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<List<AuctionBidDto>>(ApiRoutes.ListingAuctionBids(listingId), cancellationToken) ?? [];

    public async Task<AuctionBidDto> CreateAuctionBidAsync(Guid listingId, AuctionBidFormModel form, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(ApiRoutes.ListingAuctionBids(listingId), new CreateAuctionBidRequest
        {
            BidderUserName = sessionStateService.UserName,
            BidAmount = form.BidAmount
        }, cancellationToken);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AuctionBidDto>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Acik artirma teklifi olusturulamadi.");
    }

    public async Task<OfferDecisionResult> UpdateListingOfferStatusAsync(Guid listingId, Guid offerId, string status, CancellationToken cancellationToken = default)
    {
        var request = new UpdateListingOfferStatusRequest
        {
            ActorUserName = sessionStateService.UserName,
            Status = status
        };

        using var response = await httpClient.PostAsJsonAsync(ApiRoutes.ListingOfferStatus(listingId, offerId), request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            return new OfferDecisionResult
            {
                IsSuccess = false,
                Message = string.IsNullOrWhiteSpace(payload) ? "Teklif durumu guncellenemedi." : payload
            };
        }

        return new OfferDecisionResult
        {
            IsSuccess = true,
            Message = status == "accepted" ? "Teklif kabul edildi." : "Teklif reddedildi."
        };
    }

    public async Task<IReadOnlyList<ConversationSummaryDto>> GetConversationsAsync(CancellationToken cancellationToken = default)
    {
        var route = $"{ApiRoutes.Conversations}?userName={Uri.EscapeDataString(sessionStateService.UserName)}";
        return await httpClient.GetFromJsonAsync<List<ConversationSummaryDto>>(route, cancellationToken) ?? [];
    }

    public async Task<ConversationDetailDto?> GetConversationDetailAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        var route = $"{ApiRoutes.ConversationById(conversationId)}?userName={Uri.EscapeDataString(sessionStateService.UserName)}";
        return await httpClient.GetFromJsonAsync<ConversationDetailDto>(route, cancellationToken);
    }

    public async Task<ConversationDetailDto> StartListingConversationAsync(Guid listingId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(ApiRoutes.ListingConversations(listingId), new StartListingConversationRequest
        {
            UserName = sessionStateService.UserName
        }, cancellationToken);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ConversationDetailDto>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Konusma baslatilamadi.");
    }

    public async Task<MessageDto> SendMessageAsync(Guid conversationId, MessageFormModel form, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(ApiRoutes.ConversationMessages(conversationId), new SendMessageRequest
        {
            SenderUserName = sessionStateService.UserName,
            MessageText = form.MessageText
        }, cancellationToken);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MessageDto>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Mesaj gonderilemedi.");
    }

    public async Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var route = $"{ApiRoutes.Notifications}?userName={Uri.EscapeDataString(sessionStateService.UserName)}";
        return await httpClient.GetFromJsonAsync<List<NotificationDto>>(route, cancellationToken) ?? [];
    }

    public async Task<bool> MarkNotificationReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsync($"{ApiRoutes.NotificationById(notificationId)}/read?userName={Uri.EscapeDataString(sessionStateService.UserName)}", null, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<PaymentResultDto> PurchasePackageAsync(Guid packageId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(ApiRoutes.Payments, new CreatePaymentRequest
        {
            UserName = sessionStateService.UserName,
            PackageId = packageId
        }, cancellationToken);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PaymentResultDto>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Paket satin alma basarisiz.");
    }

    public async Task<ComplaintResultDto> SubmitComplaintAsync(ComplaintFormModel form, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(ApiRoutes.Complaints, new CreateComplaintRequest
        {
            UserName = sessionStateService.UserName,
            TargetEntityType = form.TargetEntityType,
            TargetEntityId = form.TargetEntityId,
            Subject = form.Subject,
            Description = form.Description
        }, cancellationToken);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ComplaintResultDto>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Sikayet gonderilemedi.");
    }

    public async Task<ListingDto> CreateListingAsync(ListingFormModel form, CancellationToken cancellationToken = default)
    {
        var request = new CreateListingRequest
        {
            Title = form.Title,
            Description = form.Description,
            CategorySlug = form.CategorySlug,
            SaleModeKey = form.SaleModeKey,
            Price = form.SaleModeKey == "auction" && form.Price <= 0 ? form.AuctionStartPrice : form.Price,
            SellerName = form.SellerName,
            AuctionStartPrice = form.SaleModeKey == "auction" ? form.AuctionStartPrice : null,
            AuctionMinBidIncrement = form.SaleModeKey == "auction" ? form.AuctionMinBidIncrement : null,
            AuctionStartsAt = form.SaleModeKey == "auction" ? new DateTimeOffset(form.AuctionStartDate.Date + form.AuctionStartTime, TimeSpan.Zero) : null,
            AuctionEndsAt = form.SaleModeKey == "auction" ? new DateTimeOffset(form.AuctionEndDate.Date + form.AuctionEndTime, TimeSpan.Zero) : null,
            AuctionAutoExtendMinutes = form.SaleModeKey == "auction" ? form.AuctionAutoExtendMinutes : null
        };

        using var response = await httpClient.PostAsJsonAsync(ApiRoutes.Listings, request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ListingDto>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Ilan olusturulamadi.");
    }

    public Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        return LoginInternalAsync(request, cancellationToken);
    }

    public Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        return RegisterInternalAsync(request, cancellationToken);
    }

    private async Task<AuthResult> LoginInternalAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(ApiRoutes.AuthLogin, new LoginRequestDto
        {
            Email = request.Email,
            Password = request.Password
        }, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<AuthResponseDto>(cancellationToken: cancellationToken);
        if (!response.IsSuccessStatusCode || payload is null)
        {
            return new AuthResult
            {
                IsSuccess = false,
                Message = payload?.Message ?? "Giris basarisiz."
            };
        }

        sessionStateService.SignIn(payload.UserName);
        return new AuthResult
        {
            IsSuccess = payload.IsSuccess,
            Message = payload.Message,
            UserName = payload.UserName
        };
    }

    private async Task<AuthResult> RegisterInternalAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(ApiRoutes.AuthRegister, new RegisterRequestDto
        {
            FullName = request.FullName,
            UserName = request.UserName,
            Email = request.Email,
            Password = request.Password,
            AccountType = request.AccountType
        }, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<AuthResponseDto>(cancellationToken: cancellationToken);
        if (!response.IsSuccessStatusCode || payload is null)
        {
            return new AuthResult
            {
                IsSuccess = false,
                Message = payload?.Message ?? "Kayit basarisiz."
            };
        }

        sessionStateService.SignIn(payload.UserName);
        return new AuthResult
        {
            IsSuccess = payload.IsSuccess,
            Message = payload.Message,
            UserName = payload.UserName
        };
    }

    private static SaleModeSummary MapSaleMode(SaleModeDto saleMode)
    {
        return new SaleModeSummary
        {
            Key = saleMode.Key,
            Name = saleMode.Name,
            AccentColor = saleMode.Key switch
            {
                "direct" => "#2F855A",
                "auction" => "#DD6B20",
                "trade" => "#6B46C1",
                _ => "#4A5568"
            },
            ShortDescription = saleMode.Description,
            Steps = saleMode.Steps
        };
    }

    private static ProductCard MapProductCard(ListingDto listing)
    {
        return new ProductCard
        {
            Title = listing.Title,
            Category = listing.Category,
            PriceLabel = listing.Price > 0 ? $"{listing.Price:N0} {listing.Currency}" : "Teklif usulu",
            Status = listing.Status
        };
    }
}
