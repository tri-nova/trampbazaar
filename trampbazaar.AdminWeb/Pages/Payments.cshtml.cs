using Microsoft.AspNetCore.Mvc.RazorPages;
using trampbazaar.AdminWeb.Services;
using trampbazaar.Shared.Contracts;

namespace trampbazaar.AdminWeb.Pages;

public sealed class PaymentsModel(AdminApiClient apiClient) : PageModel
{
    public AdminPaymentsDashboardDto Dashboard { get; private set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Dashboard = await apiClient.GetPaymentsAsync(cancellationToken);
    }
}
