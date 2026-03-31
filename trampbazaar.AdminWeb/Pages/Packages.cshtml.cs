using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using trampbazaar.AdminWeb.Services;
using trampbazaar.Shared.Contracts;

namespace trampbazaar.AdminWeb.Pages;

public sealed class PackagesModel(AdminApiClient apiClient) : PageModel
{
    private static readonly IReadOnlyList<SelectListItem> PackageTypeOptionsValue =
    [
        new("Ilan", "listing"),
        new("One Cikarma", "featured"),
        new("Vitrin", "showcase"),
        new("Kurumsal Uyelik", "corporate_membership")
    ];

    public IReadOnlyList<AdminPackageDto> Packages { get; private set; } = [];
    public IReadOnlyList<SelectListItem> PackageTypeOptions => PackageTypeOptionsValue;

    [BindProperty]
    public string PackageType { get; set; } = "listing";

    [BindProperty]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    public decimal Price { get; set; }

    [BindProperty]
    public int? DurationDays { get; set; }

    [BindProperty]
    public int? ListingQuota { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Packages = await apiClient.GetPackagesAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken cancellationToken)
    {
        try
        {
            await apiClient.CreatePackageAsync(new AdminPackageUpsertRequest
            {
                PackageType = PackageType,
                Name = Name,
                Price = Price,
                DurationDays = DurationDays,
                ListingQuota = ListingQuota
            }, cancellationToken);

            StatusMessage = "Paket olusturuldu.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Islem basarisiz: {ex.Message}";
            Packages = await apiClient.GetPackagesAsync(cancellationToken);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostUpdateAsync(Guid packageId, string packageType, string name, decimal price, int? durationDays, int? listingQuota, CancellationToken cancellationToken)
    {
        try
        {
            await apiClient.UpdatePackageAsync(packageId, new AdminPackageUpsertRequest
            {
                PackageType = packageType,
                Name = name,
                Price = price,
                DurationDays = durationDays,
                ListingQuota = listingQuota
            }, cancellationToken);

            StatusMessage = "Paket guncellendi.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Islem basarisiz: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(Guid packageId, bool nextIsActive, CancellationToken cancellationToken)
    {
        try
        {
            await apiClient.UpdatePackageStatusAsync(packageId, nextIsActive, cancellationToken);
            StatusMessage = "Paket durumu guncellendi.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Islem basarisiz: {ex.Message}";
        }

        return RedirectToPage();
    }
}
