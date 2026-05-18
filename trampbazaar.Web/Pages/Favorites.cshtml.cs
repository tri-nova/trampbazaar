using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using trampbazaar.Shared.Contracts;
using trampbazaar.Web.Services;

namespace trampbazaar.Web.Pages;

public sealed class FavoritesModel(MarketplaceWebApiClient apiClient) : PageModel
{
    [BindProperty]
    public Guid ListingId { get; set; }

    public IReadOnlyList<FavoriteListingDto> Favorites { get; private set; } = [];
    public IReadOnlyList<ListingDto> Listings { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(HttpContext.Session.GetString("UserName")))
        {
            return RedirectToPage("/Login");
        }

        await LoadAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAddAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(HttpContext.Session.GetString("UserName")))
        {
            return RedirectToPage("/Login");
        }

        await apiClient.AddFavoriteAsync(ListingId, cancellationToken);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveAsync(Guid listingId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(HttpContext.Session.GetString("UserName")))
        {
            return RedirectToPage("/Login");
        }

        await apiClient.RemoveFavoriteAsync(listingId, cancellationToken);
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Favorites = await apiClient.GetFavoritesAsync(cancellationToken);
        Listings = await apiClient.GetListingsAsync(cancellationToken: cancellationToken);
    }
}
