using trampbazaar.Models;
using trampbazaar.Shared.Contracts;

namespace trampbazaar.Services;

public sealed class MockMarketplaceDataService(SessionStateService sessionStateService) : IMarketplaceDataService
{
    private readonly List<ListingOfferDto> offers = [];
    private readonly List<AuctionBidDto> auctionBids = [];
    private readonly List<ConversationDetailDto> conversations = [];
    private readonly List<NotificationDto> notifications = [];
    private readonly List<PackageDto> packages =
    [
        new PackageDto { Id = Guid.NewGuid(), PackageType = "listing", Name = "10 Ilan Paketi", Price = 199m, CurrencyCode = "TRY", DurationDays = 30, ListingQuota = 10 },
        new PackageDto { Id = Guid.NewGuid(), PackageType = "featured", Name = "One Cikarma", Price = 149m, CurrencyCode = "TRY", DurationDays = 7, ListingQuota = 1 },
        new PackageDto { Id = Guid.NewGuid(), PackageType = "showcase", Name = "Vitrin Plus", Price = 499m, CurrencyCode = "TRY", DurationDays = 30, ListingQuota = 25 },
        new PackageDto { Id = Guid.NewGuid(), PackageType = "corporate_membership", Name = "Kurumsal Uyelik", Price = 1999m, CurrencyCode = "TRY", DurationDays = 365, ListingQuota = 250 }
    ];

    private readonly List<CategoryDto> categories =
    [
        new CategoryDto { Id = Guid.NewGuid(), Name = "Elektronik", Slug = "elektronik" },
        new CategoryDto { Id = Guid.NewGuid(), Name = "Moda", Slug = "moda" },
        new CategoryDto { Id = Guid.NewGuid(), Name = "Ev ve Yasam", Slug = "ev-ve-yasam" },
        new CategoryDto { Id = Guid.NewGuid(), Name = "Hobi", Slug = "hobi" }
    ];

    private readonly List<SaleModeDto> saleModes =
    [
        new SaleModeDto { Key = "direct", Name = "Direkt Satis", Description = "Sabit fiyatli ilan akisi.", Steps = ["Fiyat belirle", "Yayinla", "Mesaj al", "Anlas", "Teslim et"] },
        new SaleModeDto { Key = "auction", Name = "Acik Artirma", Description = "Sureli teklif toplama akisi.", Steps = ["Baslangic fiyatini gir", "Sure ayarla", "Teklifleri topla", "Kazanan sec", "Teslim et"] },
        new SaleModeDto { Key = "trade", Name = "Takas", Description = "Karsilikli teklif ve urun degisimi.", Steps = ["Ilan ac", "Teklif al", "Kabul et", "Teslim et", "Onayla"] },
        new SaleModeDto { Key = "ad", Name = "Vitrin", Description = "One cikan reklam ve vitrin akisi.", Steps = ["Paket sec", "Yayinla", "Raporlari izle"] }
    ];

    private readonly List<ListingDto> listings =
    [
        new ListingDto { Id = Guid.NewGuid(), Title = "Vintage deri ceket", Description = "Temiz kullanildi, premium deri.", Category = "Moda", SaleMode = "direct", Price = 2450m, Currency = "TRY", SellerName = "Batu", Status = "Yayinda", CreatedAt = DateTimeOffset.UtcNow.AddDays(-2) },
        new ListingDto { Id = Guid.NewGuid(), Title = "PS5 + iki kol", Description = "Kutulu, garantili ve temiz cihaz.", Category = "Elektronik", SaleMode = "auction", Price = 9000m, Currency = "TRY", SellerName = "Merve", Status = "3 teklif var", CreatedAt = DateTimeOffset.UtcNow.AddHours(-8) },
        new ListingDto { Id = Guid.NewGuid(), Title = "Koleksiyon bisiklet", Description = "Bakimli klasik model, sehir icin uygun.", Category = "Hobi", SaleMode = "trade", Price = 0m, Currency = "TRY", SellerName = "Can", Status = "Takas acik", CreatedAt = DateTimeOffset.UtcNow.AddDays(-1) }
    ];

    private readonly List<FeatureDto> features =
    [
        new FeatureDto { Title = "Mesajlasma", Description = "Ilan ve teklif bazli yazisma kutusu." },
        new FeatureDto { Title = "Bildirimler", Description = "Teklif ve durum degisimi uyarilari." },
        new FeatureDto { Title = "Admin Onayi", Description = "Opsiyonel moderasyon altyapisi." },
        new FeatureDto { Title = "Paketler", Description = "Vitrin ve one cikarma modelleri." }
    ];

    public Task<MarketplaceSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = new MarketplaceSnapshot
        {
            UserName = sessionStateService.UserName,
            HeroTitle = "TrampBazaar kullanici uygulamasi MVP akisina gecti.",
            HeroSubtitle = "Giris, ilanlar, ilan detayi ve ilan olusturma ekranlari ayni mock/live servisle calisacak.",
            QuickStats =
            [
                new QuickStat { Value = listings.Count.ToString(), Label = "aktif ilan" },
                new QuickStat { Value = saleModes.Count.ToString(), Label = "satis modu" },
                new QuickStat { Value = categories.Count.ToString(), Label = "kategori" },
                new QuickStat { Value = sessionStateService.IsAuthenticated ? "Acik" : "Kapali", Label = "oturum" }
            ],
            SaleModes = saleModes.Select(MapSaleMode).ToList(),
            SharedFlow =
            [
                new FlowStage { Title = "Login", Caption = "E-posta ve sifre ile giris" },
                new FlowStage { Title = "Register", Caption = "Bireysel veya kurumsal hesap" },
                new FlowStage { Title = "Listings", Caption = "Kategori ve satis moduna gore filtreleme" },
                new FlowStage { Title = "Detail", Caption = "Ilan detay ve teklif akisi" },
                new FlowStage { Title = "Create", Caption = "Yeni ilan olusturma formu" },
                new FlowStage { Title = "API/SQL", Caption = "Bir sonraki baglanti adimi" }
            ],
            FeatureCards = features.Select(x => new FeatureCard { Title = x.Title, Description = x.Description }).ToList(),
            FeaturedProducts = listings.Select(MapProductCard).ToList()
        };

        return Task.FromResult(snapshot);
    }

    public Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<CategoryDto>>(categories);

    public Task<IReadOnlyList<SaleModeDto>> GetSaleModesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<SaleModeDto>>(saleModes);

    public Task<IReadOnlyList<PackageDto>> GetPackagesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<PackageDto>>(packages);

    public Task<IReadOnlyList<ListingDto>> GetListingsAsync(ListingFilter filter, CancellationToken cancellationToken = default)
    {
        var query = listings.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filter.CategorySlug))
        {
            var categoryName = categories.FirstOrDefault(x => x.Slug == filter.CategorySlug)?.Name;
            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                query = query.Where(x => x.Category == categoryName);
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.SaleModeKey))
        {
            query = query.Where(x => x.SaleMode == filter.SaleModeKey);
        }

        return Task.FromResult<IReadOnlyList<ListingDto>>(query.OrderByDescending(x => x.CreatedAt).ToList());
    }

    public Task<ListingDetailState> GetListingDetailAsync(Guid listingId, CancellationToken cancellationToken = default)
    {
        var listing = listings.FirstOrDefault(x => x.Id == listingId);
        var saleMode = listing is null ? null : saleModes.FirstOrDefault(x => x.Key == listing.SaleMode);
        var auction = listing is not null && listing.SaleMode == "auction"
            ? BuildMockAuction(listing)
            : null;

        return Task.FromResult(new ListingDetailState
        {
            Listing = listing,
            SaleMode = saleMode,
            Offers = offers.Where(x => x.ListingId == listingId).OrderByDescending(x => x.CreatedAt).ToList(),
            Auction = auction,
            AuctionBids = auction is null
                ? []
                : auctionBids.Where(x => x.AuctionId == auction.Id).OrderByDescending(x => x.CreatedAt).ToList()
        });
    }

    public Task<IReadOnlyList<ListingOfferDto>> GetListingOffersAsync(Guid listingId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ListingOfferDto>>(offers.Where(x => x.ListingId == listingId).OrderByDescending(x => x.CreatedAt).ToList());

    public Task<ListingOfferDto> CreateListingOfferAsync(Guid listingId, ListingOfferFormModel form, CancellationToken cancellationToken = default)
    {
        var offer = new ListingOfferDto
        {
            Id = Guid.NewGuid(),
            ListingId = listingId,
            BuyerName = sessionStateService.UserName,
            OfferedPrice = form.OfferedPrice,
            Currency = "TRY",
            OfferNote = form.OfferNote,
            Status = "pending",
            CreatedAt = DateTimeOffset.UtcNow
        };

        offers.Insert(0, offer);
        return Task.FromResult(offer);
    }

    public Task<IReadOnlyList<AuctionBidDto>> GetAuctionBidsAsync(Guid listingId, CancellationToken cancellationToken = default)
    {
        var listing = listings.FirstOrDefault(x => x.Id == listingId);
        var auction = listing is null || listing.SaleMode != "auction" ? null : BuildMockAuction(listing);
        var items = auction is null
            ? new List<AuctionBidDto>()
            : auctionBids.Where(x => x.AuctionId == auction.Id).OrderByDescending(x => x.CreatedAt).ToList();
        return Task.FromResult<IReadOnlyList<AuctionBidDto>>(items);
    }

    public Task<AuctionBidDto> CreateAuctionBidAsync(Guid listingId, AuctionBidFormModel form, CancellationToken cancellationToken = default)
    {
        var listing = listings.FirstOrDefault(x => x.Id == listingId) ?? throw new InvalidOperationException("Ilan bulunamadi.");
        if (listing.SaleMode != "auction")
        {
            throw new InvalidOperationException("Bu ilan acik artirma ilani degil.");
        }

        var auction = BuildMockAuction(listing);
        var minimumBid = Math.Max(auction.StartPrice, (auction.CurrentBidAmount ?? 0m) + auction.MinBidIncrement);
        if (form.BidAmount < minimumBid)
        {
            throw new InvalidOperationException($"Minimum teklif {minimumBid:N0} TRY olmali.");
        }

        foreach (var item in auctionBids.Where(x => x.AuctionId == auction.Id))
        {
            item.BidStatus = "losing";
        }

        var bid = new AuctionBidDto
        {
            Id = Guid.NewGuid(),
            AuctionId = auction.Id,
            BidderUserName = sessionStateService.UserName,
            BidAmount = form.BidAmount,
            BidStatus = "winning",
            CreatedAt = DateTimeOffset.UtcNow
        };

        auctionBids.Insert(0, bid);
        listing.Price = form.BidAmount;
        listing.Status = $"{auctionBids.Count(x => x.AuctionId == auction.Id)} teklif var";

        notifications.Insert(0, new NotificationDto
        {
            Id = Guid.NewGuid(),
            NotificationType = "auction.bid.placed",
            Title = "Acik artirma teklifiniz alindi",
            Body = $"{listing.Title} icin {form.BidAmount:N0} TRY teklif verdiniz.",
            RelatedEntityType = "listing",
            RelatedEntityId = listingId,
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        });

        return Task.FromResult(bid);
    }

    public Task<OfferDecisionResult> UpdateListingOfferStatusAsync(Guid listingId, Guid offerId, string status, CancellationToken cancellationToken = default)
    {
        var offer = offers.FirstOrDefault(x => x.Id == offerId && x.ListingId == listingId);
        if (offer is null)
        {
            return Task.FromResult(new OfferDecisionResult
            {
                IsSuccess = false,
                Message = "Teklif bulunamadi."
            });
        }

        offer.Status = status;

        if (status == "accepted")
        {
            foreach (var otherOffer in offers.Where(x => x.ListingId == listingId && x.Id != offerId && x.Status == "pending"))
            {
                otherOffer.Status = "rejected";
            }
        }

        return Task.FromResult(new OfferDecisionResult
        {
            IsSuccess = true,
            Message = status == "accepted" ? "Teklif kabul edildi." : "Teklif reddedildi."
        });
    }

    public Task<IReadOnlyList<ConversationSummaryDto>> GetConversationsAsync(CancellationToken cancellationToken = default)
    {
        var items = conversations
            .Where(x => x.Messages.Any(m => m.IsMine) || string.Equals(x.CounterpartyUserName, sessionStateService.UserName, StringComparison.OrdinalIgnoreCase) == false)
            .Select(x => new ConversationSummaryDto
            {
                Id = x.Id,
                ListingId = x.ListingId,
                ConversationType = x.ConversationType,
                Title = x.Title,
                CounterpartyUserName = x.CounterpartyUserName,
                LastMessagePreview = x.Messages.LastOrDefault()?.MessageText ?? string.Empty,
                LastMessageAt = x.Messages.LastOrDefault()?.CreatedAt,
                UnreadCount = x.Messages.Count(m => !m.IsMine)
            })
            .OrderByDescending(x => x.LastMessageAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<ConversationSummaryDto>>(items);
    }

    public Task<ConversationDetailDto?> GetConversationDetailAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        var conversation = conversations.FirstOrDefault(x => x.Id == conversationId);
        if (conversation is not null)
        {
            conversation.Messages = conversation.Messages
                .Select(m => new MessageDto
                {
                    Id = m.Id,
                    ConversationId = m.ConversationId,
                    SenderUserName = m.SenderUserName,
                    MessageText = m.MessageText,
                    CreatedAt = m.CreatedAt,
                    IsMine = true
                })
                .ToList();
        }
        return Task.FromResult(conversation);
    }

    public Task<ConversationDetailDto> StartListingConversationAsync(Guid listingId, CancellationToken cancellationToken = default)
    {
        var listing = listings.FirstOrDefault(x => x.Id == listingId) ?? throw new InvalidOperationException("Ilan bulunamadi.");
        if (string.Equals(listing.SellerName, sessionStateService.UserName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Kendi ilaniniza mesaj gonderemezsiniz.");
        }

        var existing = conversations.FirstOrDefault(x => x.ListingId == listingId && string.Equals(x.CounterpartyUserName, listing.SellerName, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return Task.FromResult(existing);
        }

        var created = new ConversationDetailDto
        {
            Id = Guid.NewGuid(),
            ListingId = listingId,
            ConversationType = "listing",
            Title = listing.Title,
            CounterpartyUserName = listing.SellerName,
            Messages =
            [
                new MessageDto
                {
                    Id = Guid.NewGuid(),
                    ConversationId = Guid.Empty,
                    SenderUserName = listing.SellerName,
                    MessageText = "Merhaba, bu ilan icin sorularinizi yazabilirsiniz.",
                    CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
                    IsMine = false
                }
            ]
        };

        created.Messages[0].ConversationId = created.Id;
        conversations.Insert(0, created);
        return Task.FromResult(created);
    }

    public Task<MessageDto> SendMessageAsync(Guid conversationId, MessageFormModel form, CancellationToken cancellationToken = default)
    {
        var conversation = conversations.FirstOrDefault(x => x.Id == conversationId) ?? throw new InvalidOperationException("Konusma bulunamadi.");
        var messages = conversation.Messages.ToList();
        var message = new MessageDto
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderUserName = sessionStateService.UserName,
            MessageText = form.MessageText.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            IsMine = true
        };

        messages.Add(message);
        conversation.Messages = messages;
        notifications.Insert(0, new NotificationDto
        {
            Id = Guid.NewGuid(),
            NotificationType = "message.received",
            Title = "Yeni mesaj geldi",
            Body = $"{sessionStateService.UserName}: {form.MessageText.Trim()}",
            RelatedEntityType = "conversation",
            RelatedEntityId = conversationId,
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        });
        return Task.FromResult(message);
    }

    public Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<NotificationDto>>(notifications.OrderByDescending(x => x.CreatedAt).ToList());

    public Task<bool> MarkNotificationReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var item = notifications.FirstOrDefault(x => x.Id == notificationId);
        if (item is not null)
        {
            item.IsRead = true;
        }

        return Task.FromResult(item is not null);
    }

    public Task<PaymentResultDto> PurchasePackageAsync(Guid packageId, CancellationToken cancellationToken = default)
    {
        var package = packages.FirstOrDefault(x => x.Id == packageId) ?? throw new InvalidOperationException("Paket bulunamadi.");
        notifications.Insert(0, new NotificationDto
        {
            Id = Guid.NewGuid(),
            NotificationType = "payment.completed",
            Title = "Paket satin alindi",
            Body = $"{package.Name} paketi aktif edildi.",
            RelatedEntityType = "package",
            RelatedEntityId = packageId,
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        });

        return Task.FromResult(new PaymentResultDto
        {
            PaymentId = Guid.NewGuid(),
            PaymentStatus = "paid",
            Amount = package.Price,
            CurrencyCode = package.CurrencyCode,
            Message = "Demo satin alma tamamlandi."
        });
    }

    public Task<ComplaintResultDto> SubmitComplaintAsync(ComplaintFormModel form, CancellationToken cancellationToken = default)
    {
        notifications.Insert(0, new NotificationDto
        {
            Id = Guid.NewGuid(),
            NotificationType = "complaint.created",
            Title = "Sikayet kayda alindi",
            Body = form.Subject,
            RelatedEntityType = form.TargetEntityType,
            RelatedEntityId = form.TargetEntityId,
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        });

        return Task.FromResult(new ComplaintResultDto
        {
            ComplaintId = Guid.NewGuid(),
            ComplaintStatus = "open",
            Message = "Demo sikayet kaydi olusturuldu."
        });
    }

    public Task<ListingDto> CreateListingAsync(ListingFormModel form, CancellationToken cancellationToken = default)
    {
        var category = categories.FirstOrDefault(x => x.Slug == form.CategorySlug) ?? categories[0];
        var listing = new ListingDto
        {
            Id = Guid.NewGuid(),
            Title = form.Title.Trim(),
            Description = form.Description.Trim(),
            Category = category.Name,
            SaleMode = form.SaleModeKey,
            Price = form.SaleModeKey == "auction" && form.Price <= 0 ? form.AuctionStartPrice : form.Price,
            Currency = "TRY",
            SellerName = string.IsNullOrWhiteSpace(form.SellerName) ? sessionStateService.UserName : form.SellerName.Trim(),
            Status = form.SaleModeKey == "auction" ? "Acik artirma yayinda" : "Taslak olusturuldu",
            CreatedAt = DateTimeOffset.UtcNow
        };

        listings.Insert(0, listing);
        return Task.FromResult(listing);
    }

    public Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var userName = string.IsNullOrWhiteSpace(request.Email)
            ? "Kullanici"
            : request.Email.Split('@')[0];

        sessionStateService.SignIn(userName);

        return Task.FromResult(new AuthResult
        {
            IsSuccess = true,
            Message = "Giris basarili.",
            UserName = userName
        });
    }

    public Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var userName = string.IsNullOrWhiteSpace(request.UserName) ? request.Email.Split('@')[0] : request.UserName;
        sessionStateService.SignIn(userName);

        return Task.FromResult(new AuthResult
        {
            IsSuccess = true,
            Message = "Kayit olusturuldu.",
            UserName = userName
        });
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

    private AuctionDto BuildMockAuction(ListingDto listing)
    {
        var auctionId = listing.Id;
        var bids = auctionBids.Where(x => x.AuctionId == auctionId).OrderByDescending(x => x.BidAmount).ToList();
        if (bids.Count == 0)
        {
            bids =
            [
                new AuctionBidDto
                {
                    Id = Guid.NewGuid(),
                    AuctionId = auctionId,
                    BidderUserName = "Ayse",
                    BidAmount = 8500m,
                    BidStatus = "losing",
                    CreatedAt = DateTimeOffset.UtcNow.AddHours(-5)
                },
                new AuctionBidDto
                {
                    Id = Guid.NewGuid(),
                    AuctionId = auctionId,
                    BidderUserName = "Murat",
                    BidAmount = 9000m,
                    BidStatus = "winning",
                    CreatedAt = DateTimeOffset.UtcNow.AddHours(-3)
                }
            ];

            auctionBids.AddRange(bids);
        }

        return new AuctionDto
        {
            Id = auctionId,
            ListingId = listing.Id,
            StartPrice = 8000m,
            MinBidIncrement = 250m,
            CurrentBidAmount = bids.Max(x => x.BidAmount),
            CurrentWinnerUserName = bids.OrderByDescending(x => x.BidAmount).First().BidderUserName,
            StartsAt = DateTimeOffset.UtcNow.AddHours(-6),
            EndsAt = DateTimeOffset.UtcNow.AddHours(18),
            AutoExtendMinutes = 10,
            AuctionStatus = "active",
            ResultProcessed = false
        };
    }
}
