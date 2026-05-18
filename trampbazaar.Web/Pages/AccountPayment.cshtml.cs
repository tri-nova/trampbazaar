using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using trampbazaar.Shared.Contracts;
using trampbazaar.Web.Services;

namespace trampbazaar.Web.Pages;

public sealed class AccountPaymentModel(MarketplaceWebApiClient apiClient) : PageModel
{
    [BindProperty]
    public CreateAccountLedgerPaymentRequest PaymentForm { get; set; } = new();

    public AccountLedgerSummaryDto Ledger { get; private set; } = new();

    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(HttpContext.Session.GetString("UserName")))
        {
            return RedirectToPage("/Login");
        }

        await LoadAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(HttpContext.Session.GetString("UserName")))
        {
            return RedirectToPage("/Login");
        }

        var result = await apiClient.CreateAccountLedgerPaymentAsync(PaymentForm, cancellationToken);
        if (!result.IsSuccess || result.Result is null)
        {
            await LoadAsync(cancellationToken);
            ErrorMessage = result.ErrorMessage ?? "Odeme baslatilamadi.";
            return Page();
        }

        if (!string.IsNullOrWhiteSpace(result.Result.CheckoutUrl))
        {
            return Redirect(result.Result.CheckoutUrl);
        }

        return RedirectToPage("/Ledger");
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Ledger = await apiClient.GetAccountLedgerAsync(DateTime.Today.AddYears(-1), DateTime.Today, cancellationToken) ?? new AccountLedgerSummaryDto();
        PaymentForm.Amount = Ledger.CurrentBalance > 0 ? Ledger.CurrentBalance : 100;
        PaymentForm.Description = "Cari hesap odemesi";
    }
}
