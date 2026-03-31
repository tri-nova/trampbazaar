using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using trampbazaar.Shared.Contracts;
using trampbazaar.Web.Services;

namespace trampbazaar.Web.Pages;

public sealed class ListingDetailModel(MarketplaceWebApiClient apiClient) : PageModel
{
    public ListingDto? Listing { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid listingId, CancellationToken cancellationToken)
    {
        Listing = await apiClient.GetListingAsync(listingId, cancellationToken);
        return Listing is null ? NotFound() : Page();
    }
}
