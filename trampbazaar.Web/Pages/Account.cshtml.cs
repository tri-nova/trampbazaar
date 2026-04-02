using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using trampbazaar.Shared.Contracts;
using trampbazaar.Web.Services;

namespace trampbazaar.Web.Pages;

public sealed class AccountModel(MarketplaceWebApiClient apiClient) : PageModel
{
    public UserAccountDashboardDto Account { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(HttpContext.Session.GetString("UserName")))
        {
            return RedirectToPage("/Login");
        }

        Account = await apiClient.GetAccountDashboardAsync(cancellationToken) ?? new UserAccountDashboardDto();
        return Page();
    }
}
