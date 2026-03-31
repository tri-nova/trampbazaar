using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using trampbazaar.Shared.Contracts;
using trampbazaar.Web.Services;

namespace trampbazaar.Web.Pages;

public sealed class CreateListingModel(MarketplaceWebApiClient apiClient) : PageModel
{
    [BindProperty]
    public string Title { get; set; } = string.Empty;

    [BindProperty]
    public string Description { get; set; } = string.Empty;

    [BindProperty]
    public string CategorySlug { get; set; } = string.Empty;

    [BindProperty]
    public string SaleModeKey { get; set; } = "direct";

    [BindProperty]
    public decimal Price { get; set; }

    public string? ErrorMessage { get; private set; }
    public IReadOnlyList<CategoryDto> Categories { get; private set; } = [];
    public IReadOnlyList<SaleModeDto> SaleModes { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(HttpContext.Session.GetString("UserName")))
        {
            return RedirectToPage("/Login");
        }

        await LoadLookupsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var userName = HttpContext.Session.GetString("UserName");
        if (string.IsNullOrWhiteSpace(userName))
        {
            return RedirectToPage("/Login");
        }

        var listing = await apiClient.CreateListingAsync(new CreateListingRequest
        {
            Title = Title,
            Description = Description,
            CategorySlug = CategorySlug,
            SaleModeKey = SaleModeKey,
            Price = Price,
            SellerName = userName
        }, cancellationToken);

        if (listing is null)
        {
            ErrorMessage = "Ilan olusturulamadi.";
            await LoadLookupsAsync(cancellationToken);
            return Page();
        }

        return RedirectToPage("/ListingDetail", new { listingId = listing.Id });
    }

    private async Task LoadLookupsAsync(CancellationToken cancellationToken)
    {
        Categories = await apiClient.GetCategoriesAsync(cancellationToken);
        SaleModes = await apiClient.GetSaleModesAsync(cancellationToken);
    }
}
