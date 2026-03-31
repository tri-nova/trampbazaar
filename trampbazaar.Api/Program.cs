using Microsoft.Data.SqlClient;
using trampbazaar.Api.Services;
using trampbazaar.Shared.Api;
using trampbazaar.Shared.Contracts;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});
builder.WebHost.UseUrls(builder.Configuration["Server:BaseUrl"] ?? "http://localhost:5136");

builder.Services.AddOpenApi();
builder.Services.AddSingleton<MarketplaceRepository>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();

app.MapGet(ApiRoutes.Dashboard, async (MarketplaceRepository repository, CancellationToken cancellationToken) =>
    Results.Ok(await repository.GetDashboardAsync(cancellationToken)))
    .WithName("GetDashboard");

app.MapGet(ApiRoutes.AdminOverview, async (MarketplaceRepository repository, CancellationToken cancellationToken) =>
    Results.Ok(await repository.GetAdminOverviewAsync(cancellationToken)))
    .WithName("GetAdminOverview");

app.MapPost(ApiRoutes.AdminAuthLogin, async (LoginRequestDto request, MarketplaceRepository repository, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["request"] = ["E-posta ve sifre zorunludur."]
        });
    }

    var result = await repository.LoginAdminAsync(request, cancellationToken);
    return result.IsSuccess
        ? Results.Ok(result)
        : Results.BadRequest(result);
})
    .WithName("AdminLogin");

app.MapGet(ApiRoutes.AdminUsers, async (MarketplaceRepository repository, CancellationToken cancellationToken) =>
    Results.Ok(await repository.GetAdminUsersAsync(cancellationToken)))
    .WithName("GetAdminUsers");

app.MapPost($"{ApiRoutes.AdminUsers}/{{userId:guid}}/status", async (Guid userId, AdminUserStatusUpdateRequest request, MarketplaceRepository repository, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Status))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["status"] = ["Durum zorunludur."]
        });
    }

    var updated = await repository.UpdateAdminUserStatusAsync(userId, request.Status, cancellationToken);
    return updated ? Results.Ok() : Results.NotFound();
})
    .WithName("UpdateAdminUserStatus");

app.MapGet(ApiRoutes.AdminListings, async (MarketplaceRepository repository, CancellationToken cancellationToken) =>
    Results.Ok(await repository.GetAdminListingsAsync(cancellationToken)))
    .WithName("GetAdminListings");

app.MapPost($"{ApiRoutes.AdminListings}/{{listingId:guid}}/status", async (Guid listingId, AdminListingStatusUpdateRequest request, MarketplaceRepository repository, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Status))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["status"] = ["Durum zorunludur."]
        });
    }

    var updated = await repository.UpdateAdminListingStatusAsync(listingId, request.Status, cancellationToken);
    return updated ? Results.Ok() : Results.NotFound();
})
    .WithName("UpdateAdminListingStatus");

app.MapGet(ApiRoutes.AdminConversations, async (MarketplaceRepository repository, CancellationToken cancellationToken) =>
    Results.Ok(await repository.GetAdminConversationsAsync(cancellationToken)))
    .WithName("GetAdminConversations");

app.MapGet(ApiRoutes.AdminCategories, async (MarketplaceRepository repository, CancellationToken cancellationToken) =>
    Results.Ok(await repository.GetAdminCategoriesAsync(cancellationToken)))
    .WithName("GetAdminCategories");

app.MapPost(ApiRoutes.AdminCategories, async (AdminCategoryUpsertRequest request, MarketplaceRepository repository, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Slug))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["request"] = ["Kategori adi ve slug zorunludur."]
        });
    }

    try
    {
        var category = await repository.AddAdminCategoryAsync(request, cancellationToken);
        return Results.Created(ApiRoutes.AdminCategoryById(category.Id), category);
    }
    catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
    {
        return Results.BadRequest(new { error = "Ayni slug ile baska bir kategori var." });
    }
})
    .WithName("CreateAdminCategory");

app.MapPut($"{ApiRoutes.AdminCategories}/{{categoryId:guid}}", async (Guid categoryId, AdminCategoryUpsertRequest request, MarketplaceRepository repository, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Slug))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["request"] = ["Kategori adi ve slug zorunludur."]
        });
    }

    try
    {
        var updated = await repository.UpdateAdminCategoryAsync(categoryId, request, cancellationToken);
        return updated ? Results.Ok() : Results.NotFound();
    }
    catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
    {
        return Results.BadRequest(new { error = "Ayni slug ile baska bir kategori var." });
    }
})
    .WithName("UpdateAdminCategory");

app.MapPost($"{ApiRoutes.AdminCategories}/{{categoryId:guid}}/status", async (Guid categoryId, AdminCategoryStatusUpdateRequest request, MarketplaceRepository repository, CancellationToken cancellationToken) =>
{
    var updated = await repository.UpdateAdminCategoryStatusAsync(categoryId, request.IsActive, cancellationToken);
    return updated ? Results.Ok() : Results.NotFound();
})
    .WithName("UpdateAdminCategoryStatus");

app.MapGet(ApiRoutes.AdminPackages, async (MarketplaceRepository repository, CancellationToken cancellationToken) =>
    Results.Ok(await repository.GetAdminPackagesAsync(cancellationToken)))
    .WithName("GetAdminPackages");

app.MapPost(ApiRoutes.AdminPackages, async (AdminPackageUpsertRequest request, MarketplaceRepository repository, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.PackageType) || string.IsNullOrWhiteSpace(request.Name) || request.Price < 0)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["request"] = ["Paket tipi, ad ve gecerli fiyat zorunludur."]
        });
    }

    try
    {
        var package = await repository.AddAdminPackageAsync(request, cancellationToken);
        return Results.Created(ApiRoutes.AdminPackageById(package.Id), package);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
    .WithName("CreateAdminPackage");

app.MapPut($"{ApiRoutes.AdminPackages}/{{packageId:guid}}", async (Guid packageId, AdminPackageUpsertRequest request, MarketplaceRepository repository, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.PackageType) || string.IsNullOrWhiteSpace(request.Name) || request.Price < 0)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["request"] = ["Paket tipi, ad ve gecerli fiyat zorunludur."]
        });
    }

    try
    {
        var updated = await repository.UpdateAdminPackageAsync(packageId, request, cancellationToken);
        return updated ? Results.Ok() : Results.NotFound();
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
    .WithName("UpdateAdminPackage");

app.MapPost($"{ApiRoutes.AdminPackages}/{{packageId:guid}}/status", async (Guid packageId, AdminPackageStatusUpdateRequest request, MarketplaceRepository repository, CancellationToken cancellationToken) =>
{
    var updated = await repository.UpdateAdminPackageStatusAsync(packageId, request.IsActive, cancellationToken);
    return updated ? Results.Ok() : Results.NotFound();
})
    .WithName("UpdateAdminPackageStatus");

app.MapGet(ApiRoutes.AdminPayments, async (MarketplaceRepository repository, CancellationToken cancellationToken) =>
    Results.Ok(await repository.GetAdminPaymentsAsync(cancellationToken)))
    .WithName("GetAdminPayments");

app.MapGet(ApiRoutes.AdminComplaints, async (MarketplaceRepository repository, CancellationToken cancellationToken) =>
    Results.Ok(await repository.GetAdminComplaintsAsync(cancellationToken)))
    .WithName("GetAdminComplaints");

app.MapPost($"{ApiRoutes.AdminComplaints}/{{complaintId:guid}}/status", async (Guid complaintId, AdminComplaintStatusUpdateRequest request, MarketplaceRepository repository, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Status))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["status"] = ["Sikayet durumu zorunludur."]
        });
    }

    try
    {
        var updated = await repository.UpdateAdminComplaintStatusAsync(complaintId, request, cancellationToken);
        return updated ? Results.Ok() : Results.NotFound();
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
    .WithName("UpdateAdminComplaintStatus");

app.MapGet(ApiRoutes.AdminNotifications, async (MarketplaceRepository repository, CancellationToken cancellationToken) =>
    Results.Ok(await repository.GetAdminNotificationsAsync(cancellationToken)))
    .WithName("GetAdminNotifications");

app.MapGet(ApiRoutes.Categories, async (MarketplaceRepository repository, CancellationToken cancellationToken) =>
    Results.Ok(await repository.GetCategoriesAsync(cancellationToken)))
    .WithName("GetCategories");

app.MapGet(ApiRoutes.SaleModes, async (MarketplaceRepository repository, CancellationToken cancellationToken) =>
    Results.Ok(await repository.GetSaleModesAsync(cancellationToken)))
    .WithName("GetSaleModes");

app.MapGet(ApiRoutes.Packages, async (MarketplaceRepository repository, CancellationToken cancellationToken) =>
    Results.Ok(await repository.GetPackagesAsync(cancellationToken)))
    .WithName("GetPackages");

app.MapGet(ApiRoutes.Listings, async (string? category, string? saleMode, MarketplaceRepository repository, CancellationToken cancellationToken) =>
    Results.Ok(await repository.GetListingsAsync(category, saleMode, cancellationToken)))
    .WithName("GetListings");

app.MapGet($"{ApiRoutes.Listings}/{{listingId:guid}}", async (Guid listingId, MarketplaceRepository repository, CancellationToken cancellationToken) =>
{
    var listing = await repository.GetListingByIdAsync(listingId, cancellationToken);
    return listing is null ? Results.NotFound() : Results.Ok(listing);
})
    .WithName("GetListingById");

app.MapGet($"{ApiRoutes.Listings}/{{listingId:guid}}/auction", async (Guid listingId, MarketplaceRepository repository, CancellationToken cancellationToken) =>
{
    var auction = await repository.GetAuctionByListingIdAsync(listingId, cancellationToken);
    return auction is null ? Results.NotFound() : Results.Ok(auction);
})
    .WithName("GetListingAuction");

app.MapGet($"{ApiRoutes.Listings}/{{listingId:guid}}/auction/bids", async (Guid listingId, MarketplaceRepository repository, CancellationToken cancellationToken) =>
    Results.Ok(await repository.GetAuctionBidsByListingIdAsync(listingId, cancellationToken)))
    .WithName("GetListingAuctionBids");

app.MapPost($"{ApiRoutes.Listings}/{{listingId:guid}}/auction/bids", async (Guid listingId, CreateAuctionBidRequest request, MarketplaceRepository repository, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.BidderUserName))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["bidderUserName"] = ["Teklif veren kullanici zorunludur."]
        });
    }

    if (request.BidAmount <= 0)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["bidAmount"] = ["Teklif tutari sifirdan buyuk olmalidir."]
        });
    }

    try
    {
        var bid = await repository.AddAuctionBidAsync(listingId, request, cancellationToken);
        return Results.Created($"{ApiRoutes.ListingAuctionBids(listingId)}/{bid.Id}", bid);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
    .WithName("CreateListingAuctionBid");

app.MapGet($"{ApiRoutes.Listings}/{{listingId:guid}}/offers", async (Guid listingId, MarketplaceRepository repository, CancellationToken cancellationToken) =>
    Results.Ok(await repository.GetListingOffersAsync(listingId, cancellationToken)))
    .WithName("GetListingOffers");

app.MapPost($"{ApiRoutes.Listings}/{{listingId:guid}}/offers", async (Guid listingId, CreateListingOfferRequest request, MarketplaceRepository repository, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.BuyerName))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["buyerName"] = ["Teklif veren kullanici zorunludur."]
        });
    }

    if (request.OfferedPrice <= 0)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["offeredPrice"] = ["Teklif tutari sifirdan buyuk olmalidir."]
        });
    }

    try
    {
        var offer = await repository.AddListingOfferAsync(listingId, request, cancellationToken);
        return Results.Created($"{ApiRoutes.ListingOffers(listingId)}/{offer.Id}", offer);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
    .WithName("CreateListingOffer");

app.MapPost($"{ApiRoutes.Listings}/{{listingId:guid}}/offers/{{offerId:guid}}/status", async (Guid listingId, Guid offerId, UpdateListingOfferStatusRequest request, MarketplaceRepository repository, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.ActorUserName))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["actorUserName"] = ["Islemi yapan kullanici zorunludur."]
        });
    }

    try
    {
        var offer = await repository.UpdateListingOfferStatusAsync(listingId, offerId, request, cancellationToken);
        return Results.Ok(offer);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
    .WithName("UpdateListingOfferStatus");

app.MapPost($"{ApiRoutes.Listings}/{{listingId:guid}}/conversations", async (Guid listingId, StartListingConversationRequest request, MarketplaceRepository repository, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.UserName))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["userName"] = ["Kullanici zorunludur."]
        });
    }

    try
    {
        var conversation = await repository.StartListingConversationAsync(listingId, request, cancellationToken);
        return Results.Ok(conversation);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
    .WithName("StartListingConversation");

app.MapGet(ApiRoutes.Conversations, async (string userName, MarketplaceRepository repository, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(userName))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["userName"] = ["Kullanici zorunludur."]
        });
    }

    return Results.Ok(await repository.GetConversationsAsync(userName, cancellationToken));
})
    .WithName("GetConversations");

app.MapGet($"{ApiRoutes.Conversations}/{{conversationId:guid}}", async (Guid conversationId, string userName, MarketplaceRepository repository, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(userName))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["userName"] = ["Kullanici zorunludur."]
        });
    }

    var conversation = await repository.GetConversationByIdAsync(conversationId, userName, cancellationToken);
    return conversation is null ? Results.NotFound() : Results.Ok(conversation);
})
    .WithName("GetConversationById");

app.MapPost($"{ApiRoutes.Conversations}/{{conversationId:guid}}/messages", async (Guid conversationId, SendMessageRequest request, MarketplaceRepository repository, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.SenderUserName) || string.IsNullOrWhiteSpace(request.MessageText))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["request"] = ["Gonderen ve mesaj metni zorunludur."]
        });
    }

    try
    {
        var message = await repository.AddMessageAsync(conversationId, request, cancellationToken);
        return Results.Ok(message);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
    .WithName("SendMessage");

app.MapPost(ApiRoutes.Payments, async (CreatePaymentRequest request, MarketplaceRepository repository, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.UserName) || request.PackageId == Guid.Empty)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["request"] = ["Kullanici ve paket zorunludur."]
        });
    }

    try
    {
        return Results.Ok(await repository.CreatePaymentAsync(request, cancellationToken));
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
    .WithName("CreatePayment");

app.MapPost(ApiRoutes.Complaints, async (CreateComplaintRequest request, MarketplaceRepository repository, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.UserName) ||
        string.IsNullOrWhiteSpace(request.TargetEntityType) ||
        request.TargetEntityId == Guid.Empty ||
        string.IsNullOrWhiteSpace(request.Subject) ||
        string.IsNullOrWhiteSpace(request.Description))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["request"] = ["Tum sikayet alanlari zorunludur."]
        });
    }

    try
    {
        return Results.Ok(await repository.CreateComplaintAsync(request, cancellationToken));
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
    .WithName("CreateComplaint");

app.MapGet(ApiRoutes.Notifications, async (string userName, MarketplaceRepository repository, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(userName))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["userName"] = ["Kullanici zorunludur."]
        });
    }

    return Results.Ok(await repository.GetNotificationsAsync(userName, cancellationToken));
})
    .WithName("GetNotifications");

app.MapPost($"{ApiRoutes.Notifications}/{{notificationId:guid}}/read", async (Guid notificationId, string userName, MarketplaceRepository repository, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(userName))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["userName"] = ["Kullanici zorunludur."]
        });
    }

    var updated = await repository.MarkNotificationReadAsync(notificationId, userName, cancellationToken);
    return updated ? Results.Ok() : Results.NotFound();
})
    .WithName("MarkNotificationRead");

app.MapPost(ApiRoutes.Listings, async (CreateListingRequest request, MarketplaceRepository repository, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Title) ||
        string.IsNullOrWhiteSpace(request.Description) ||
        string.IsNullOrWhiteSpace(request.CategorySlug) ||
        string.IsNullOrWhiteSpace(request.SaleModeKey) ||
        string.IsNullOrWhiteSpace(request.SellerName))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["request"] = ["Tum zorunlu alanlar doldurulmalidir."]
        });
    }

    if (request.Price < 0)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["price"] = ["Fiyat sifirdan kucuk olamaz."]
        });
    }

    if (string.Equals(request.SaleModeKey, "auction", StringComparison.OrdinalIgnoreCase))
    {
        if (!request.AuctionStartPrice.HasValue || !request.AuctionMinBidIncrement.HasValue ||
            !request.AuctionStartsAt.HasValue || !request.AuctionEndsAt.HasValue)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["auction"] = ["Acik artirma alanlari zorunludur."]
            });
        }
    }

    try
    {
        var listing = await repository.AddListingAsync(request, cancellationToken);
        return Results.Created($"{ApiRoutes.Listings}/{listing.Id}", listing);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("CreateListing");

app.MapPost(ApiRoutes.AuthRegister, async (RegisterRequestDto request, MarketplaceRepository repository, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.FullName) ||
        string.IsNullOrWhiteSpace(request.UserName) ||
        string.IsNullOrWhiteSpace(request.Email) ||
        string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["request"] = ["Tum alanlar zorunludur."]
        });
    }

    var result = await repository.RegisterAsync(request, cancellationToken);
    return result.IsSuccess
        ? Results.Ok(result)
        : Results.BadRequest(result);
})
.WithName("Register");

app.MapPost(ApiRoutes.AuthLogin, async (LoginRequestDto request, MarketplaceRepository repository, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["request"] = ["E-posta ve sifre zorunludur."]
        });
    }

    var result = await repository.LoginAsync(request, cancellationToken);
    return result.IsSuccess
        ? Results.Ok(result)
        : Results.BadRequest(result);
})
.WithName("Login");

app.Run();
