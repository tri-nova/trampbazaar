using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using trampbazaar.Shared.Contracts;
using trampbazaar.Web.Services;

namespace trampbazaar.Web.Pages;

public sealed class PriceAlertsModel(MarketplaceWebApiClient apiClient) : PageModel
{
    [BindProperty]
    public AddPriceAlertRequest AlertForm { get; set; } = new();

    public IReadOnlyList<PriceAlertDto> Alerts { get; private set; } = [];
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

        await apiClient.AddPriceAlertAsync(AlertForm, cancellationToken);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveAsync(Guid alertId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(HttpContext.Session.GetString("UserName")))
        {
            return RedirectToPage("/Login");
        }

        await apiClient.RemovePriceAlertAsync(alertId, cancellationToken);
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Alerts = await apiClient.GetPriceAlertsAsync(cancellationToken);
        Listings = await apiClient.GetListingsAsync(cancellationToken: cancellationToken);
    }
}
