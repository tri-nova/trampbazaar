using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using trampbazaar.Shared.Contracts;
using trampbazaar.Web.Services;

namespace trampbazaar.Web.Pages;

public sealed class LedgerModel(MarketplaceWebApiClient apiClient) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public DateTime? StartDate { get; set; } = DateTime.Today.AddYears(-1);

    [BindProperty(SupportsGet = true)]
    public DateTime? EndDate { get; set; } = DateTime.Today;

    public AccountLedgerSummaryDto Ledger { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(HttpContext.Session.GetString("UserName")))
        {
            return RedirectToPage("/Login");
        }

        Ledger = await apiClient.GetAccountLedgerAsync(StartDate, EndDate, cancellationToken) ?? new AccountLedgerSummaryDto();
        return Page();
    }
}
