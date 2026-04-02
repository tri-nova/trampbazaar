using System.Net.Http.Json;
using System.Net.Http.Headers;
using trampbazaar.Shared.Api;
using trampbazaar.Shared.Contracts;

namespace trampbazaar.Web.Services;

public sealed class MarketplaceWebApiClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
{
    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(ApiRoutes.AuthLogin, request, cancellationToken);
        return await response.Content.ReadFromJsonAsync<AuthResponseDto>(cancellationToken: cancellationToken)
               ?? new AuthResponseDto { IsSuccess = false, Message = "Giris basarisiz." };
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(ApiRoutes.AuthRegister, request, cancellationToken);
        return await response.Content.ReadFromJsonAsync<AuthResponseDto>(cancellationToken: cancellationToken)
               ?? new AuthResponseDto { IsSuccess = false, Message = "Kayit basarisiz." };
    }

    public async Task<DashboardResponse> GetDashboardAsync(CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<DashboardResponse>(ApiRoutes.Dashboard, cancellationToken)
           ?? new DashboardResponse();

    public async Task<IReadOnlyList<ListingDto>> GetListingsAsync(string? category = null, string? saleMode = null, CancellationToken cancellationToken = default)
    {
        var route = ApiRoutes.Listings;
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(category))
        {
            query.Add($"category={Uri.EscapeDataString(category)}");
        }

        if (!string.IsNullOrWhiteSpace(saleMode))
        {
            query.Add($"saleMode={Uri.EscapeDataString(saleMode)}");
        }

        if (query.Count > 0)
        {
            route = $"{route}?{string.Join("&", query)}";
        }

        return await httpClient.GetFromJsonAsync<List<ListingDto>>(route, cancellationToken) ?? [];
    }

    public async Task<ListingDto?> GetListingAsync(Guid listingId, CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<ListingDto>(ApiRoutes.ListingById(listingId), cancellationToken);

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<List<CategoryDto>>(ApiRoutes.Categories, cancellationToken) ?? [];

    public async Task<IReadOnlyList<SaleModeDto>> GetSaleModesAsync(CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<List<SaleModeDto>>(ApiRoutes.SaleModes, cancellationToken) ?? [];

    public async Task<IReadOnlyList<PackageDto>> GetPackagesAsync(CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<List<PackageDto>>(ApiRoutes.Packages, cancellationToken) ?? [];

    public async Task<UserAccountDashboardDto?> GetAccountDashboardAsync(CancellationToken cancellationToken = default)
        => await GetFromJsonAuthorizedAsync<UserAccountDashboardDto>(ApiRoutes.Account, cancellationToken);

    public async Task<ListingDto?> CreateListingAsync(CreateListingRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await PostAsJsonAuthorizedAsync(ApiRoutes.Listings, request, cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<ListingDto>(cancellationToken: cancellationToken)
            : null;
    }

    public async Task<PaymentResultDto?> CreatePaymentAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await PostAsJsonAuthorizedAsync(ApiRoutes.Payments, request, cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<PaymentResultDto>(cancellationToken: cancellationToken)
            : null;
    }

    public async Task<AuctionDto?> GetListingAuctionAsync(Guid listingId, CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<AuctionDto>(ApiRoutes.ListingAuction(listingId), cancellationToken);

    public async Task<IReadOnlyList<AuctionBidDto>> GetListingAuctionBidsAsync(Guid listingId, CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<List<AuctionBidDto>>(ApiRoutes.ListingAuctionBids(listingId), cancellationToken) ?? [];

    public async Task<IReadOnlyList<ListingOfferDto>> GetListingOffersAsync(Guid listingId, CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<List<ListingOfferDto>>(ApiRoutes.ListingOffers(listingId), cancellationToken) ?? [];

    public async Task<(bool IsSuccess, string? ErrorMessage)> CreateListingOfferAsync(Guid listingId, CreateListingOfferRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await PostAsJsonAuthorizedAsync(ApiRoutes.ListingOffers(listingId), request, cancellationToken);
        return (response.IsSuccessStatusCode, await TryReadErrorAsync(response, cancellationToken));
    }

    public async Task<(bool IsSuccess, string? ErrorMessage)> CreateAuctionBidAsync(Guid listingId, CreateAuctionBidRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await PostAsJsonAuthorizedAsync(ApiRoutes.ListingAuctionBids(listingId), request, cancellationToken);
        return (response.IsSuccessStatusCode, await TryReadErrorAsync(response, cancellationToken));
    }

    public async Task<(bool IsSuccess, string? ErrorMessage)> UpdateListingOfferStatusAsync(Guid listingId, Guid offerId, UpdateListingOfferStatusRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await PostAsJsonAuthorizedAsync(ApiRoutes.ListingOfferStatus(listingId, offerId), request, cancellationToken);
        return (response.IsSuccessStatusCode, await TryReadErrorAsync(response, cancellationToken));
    }

    public async Task<ConversationDetailDto?> StartListingConversationAsync(Guid listingId, StartListingConversationRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await PostAsJsonAuthorizedAsync(ApiRoutes.ListingConversations(listingId), request, cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<ConversationDetailDto>(cancellationToken: cancellationToken)
            : null;
    }

    public async Task<IReadOnlyList<ConversationSummaryDto>> GetConversationsAsync(string userName, CancellationToken cancellationToken = default)
        => await GetFromJsonAuthorizedAsync<List<ConversationSummaryDto>>(ApiRoutes.Conversations, cancellationToken) ?? [];

    public async Task<ConversationDetailDto?> GetConversationAsync(Guid conversationId, string userName, CancellationToken cancellationToken = default)
        => await GetFromJsonAuthorizedAsync<ConversationDetailDto>(ApiRoutes.ConversationById(conversationId), cancellationToken);

    public async Task<(bool IsSuccess, string? ErrorMessage)> SendMessageAsync(Guid conversationId, SendMessageRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await PostAsJsonAuthorizedAsync(ApiRoutes.ConversationMessages(conversationId), request, cancellationToken);
        return (response.IsSuccessStatusCode, await TryReadErrorAsync(response, cancellationToken));
    }

    public async Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(string userName, CancellationToken cancellationToken = default)
        => await GetFromJsonAuthorizedAsync<List<NotificationDto>>(ApiRoutes.Notifications, cancellationToken) ?? [];

    public async Task<(bool IsSuccess, string? ErrorMessage)> MarkNotificationReadAsync(Guid notificationId, string userName, CancellationToken cancellationToken = default)
    {
        using var response = await PostAuthorizedAsync($"{ApiRoutes.NotificationById(notificationId)}/read", cancellationToken);
        return (response.IsSuccessStatusCode, await TryReadErrorAsync(response, cancellationToken));
    }

    public async Task<(bool IsSuccess, string? ErrorMessage)> CreateComplaintAsync(CreateComplaintRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await PostAsJsonAuthorizedAsync(ApiRoutes.Complaints, request, cancellationToken);
        return (response.IsSuccessStatusCode, await TryReadErrorAsync(response, cancellationToken));
    }

    private async Task<T?> GetFromJsonAuthorizedAsync<T>(string route, CancellationToken cancellationToken)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, route);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
            : default;
    }

    private async Task<HttpResponseMessage> PostAsJsonAuthorizedAsync<T>(string route, T payload, CancellationToken cancellationToken)
    {
        var request = CreateAuthorizedRequest(HttpMethod.Post, route);
        request.Content = JsonContent.Create(payload);
        return await httpClient.SendAsync(request, cancellationToken);
    }

    private async Task<HttpResponseMessage> PostAuthorizedAsync(string route, CancellationToken cancellationToken)
    {
        var request = CreateAuthorizedRequest(HttpMethod.Post, route);
        return await httpClient.SendAsync(request, cancellationToken);
    }

    private HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string route)
    {
        var request = new HttpRequestMessage(method, route);
        var accessToken = httpContextAccessor.HttpContext?.Session.GetString("AccessToken");
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return request;
    }

    private static async Task<string?> TryReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return null;
        }

        try
        {
            var problem = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken: cancellationToken);
            if (problem is null || problem.Count == 0)
            {
                return "Islem basarisiz.";
            }

            if (problem.TryGetValue("error", out var error) && error is not null)
            {
                return error.ToString();
            }

            return string.Join(" ", problem.Values.Select(static value => value?.ToString()).Where(static value => !string.IsNullOrWhiteSpace(value)));
        }
        catch
        {
            return "Islem basarisiz.";
        }
    }
}
