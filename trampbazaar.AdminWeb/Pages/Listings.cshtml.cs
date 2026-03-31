using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using trampbazaar.AdminWeb.Services;
using trampbazaar.Shared.Contracts;

namespace trampbazaar.AdminWeb.Pages;

public sealed class ListingsModel(AdminApiClient apiClient) : PageModel
{
    public IReadOnlyList<AdminListingDto> Listings { get; private set; } = [];
    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Listings = await apiClient.GetListingsAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(Guid listingId, string nextStatus, CancellationToken cancellationToken)
    {
        try
        {
            await apiClient.UpdateListingStatusAsync(listingId, nextStatus, cancellationToken);
            StatusMessage = "Ilan durumu guncellendi.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Islem basarisiz: {ex.Message}";
        }

        return RedirectToPage();
    }
}
