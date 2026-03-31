using System.Net.Http.Json;
using trampbazaar.Shared.Api;
using trampbazaar.Shared.Contracts;

namespace trampbazaar.Web.Services;

public sealed class MarketplaceWebApiClient(HttpClient httpClient)
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

    public async Task<ListingDto?> CreateListingAsync(CreateListingRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(ApiRoutes.Listings, request, cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<ListingDto>(cancellationToken: cancellationToken)
            : null;
    }
}
