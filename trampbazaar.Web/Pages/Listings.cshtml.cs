using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using trampbazaar.Shared.Contracts;
using trampbazaar.Web.Services;

namespace trampbazaar.Web.Pages;

public sealed class ListingsModel(MarketplaceWebApiClient apiClient) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Category { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SaleMode { get; set; }

    public IReadOnlyList<ListingDto> Listings { get; private set; } = [];
    public IReadOnlyList<CategoryDto> Categories { get; private set; } = [];
    public IReadOnlyList<SaleModeDto> SaleModes { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Categories = await apiClient.GetCategoriesAsync(cancellationToken);
        SaleModes = await apiClient.GetSaleModesAsync(cancellationToken);
        Listings = await apiClient.GetListingsAsync(Category, SaleMode, cancellationToken);
    }
}
