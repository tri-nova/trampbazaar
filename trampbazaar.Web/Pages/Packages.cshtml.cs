using Microsoft.AspNetCore.Mvc.RazorPages;
using trampbazaar.Shared.Contracts;
using trampbazaar.Web.Services;

namespace trampbazaar.Web.Pages;

public sealed class PackagesModel(MarketplaceWebApiClient apiClient) : PageModel
{
    public IReadOnlyList<PackageDto> Packages { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Packages = await apiClient.GetPackagesAsync(cancellationToken);
    }
}
