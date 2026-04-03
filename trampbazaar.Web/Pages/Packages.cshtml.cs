using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using trampbazaar.Shared.Contracts;
using trampbazaar.Web.Services;

namespace trampbazaar.Web.Pages;

public sealed class PackagesModel(MarketplaceWebApiClient apiClient) : PageModel
{
    public IReadOnlyList<PackageDto> Packages { get; private set; } = [];
    [TempData]
    public string? ErrorMessage { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(HttpContext.Session.GetString("UserName"));

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Packages = await apiClient.GetPackagesAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostPurchaseAsync(Guid packageId, CancellationToken cancellationToken)
    {
        var userName = HttpContext.Session.GetString("UserName");
        if (string.IsNullOrWhiteSpace(userName))
        {
            return RedirectToPage("/Login");
        }

        Packages = await apiClient.GetPackagesAsync(cancellationToken);
        if (Packages.All(packageItem => packageItem.Id != packageId))
        {
            ErrorMessage = "Secilen paket bulunamadi.";
            return RedirectToPage();
        }

        var payment = await apiClient.CreatePaymentAsync(new CreatePaymentRequest
        {
            UserName = userName,
            PackageId = packageId,
            SuccessUrl = Url.Page("/PaymentSuccess", pageHandler: null, values: null, protocol: Request.Scheme),
            CancelUrl = Url.Page("/PaymentCancel", pageHandler: null, values: null, protocol: Request.Scheme)
        }, cancellationToken);

        if (payment is null)
        {
            ErrorMessage = "Odeme kaydi olusturulamadi.";
            return RedirectToPage();
        }

        if (!string.IsNullOrWhiteSpace(payment.CheckoutUrl))
        {
            return Redirect(payment.CheckoutUrl);
        }

        StatusMessage = $"{payment.Amount:N0} {payment.CurrencyCode} tutarinda satin alma tamamlandi.";
        return RedirectToPage();
    }
}
