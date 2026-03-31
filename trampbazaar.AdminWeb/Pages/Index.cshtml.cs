using Microsoft.AspNetCore.Mvc.RazorPages;
using trampbazaar.AdminWeb.Services;
using trampbazaar.Shared.Contracts;

namespace trampbazaar.AdminWeb.Pages;

public sealed class IndexModel(AdminApiClient apiClient) : PageModel
{
    public AdminOverviewDto Overview { get; private set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Overview = await apiClient.GetOverviewAsync(cancellationToken);
    }
}
