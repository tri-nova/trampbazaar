using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using trampbazaar.Shared.Contracts;
using trampbazaar.Web.Services;

namespace trampbazaar.Web.Pages;

public sealed class OrdersModel(MarketplaceWebApiClient apiClient) : PageModel
{
    public IReadOnlyList<CustomerOrderDto> Orders { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(HttpContext.Session.GetString("UserName")))
        {
            return RedirectToPage("/Login");
        }

        Orders = await apiClient.GetAccountOrdersAsync(cancellationToken);
        return Page();
    }
}
