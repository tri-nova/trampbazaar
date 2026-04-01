using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using trampbazaar.Shared.Contracts;
using trampbazaar.Web.Services;

namespace trampbazaar.Web.Pages;

public sealed class NotificationsModel(MarketplaceWebApiClient apiClient) : PageModel
{
    public IReadOnlyList<NotificationDto> Notifications { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var userName = HttpContext.Session.GetString("UserName");
        if (string.IsNullOrWhiteSpace(userName))
        {
            return RedirectToPage("/Login");
        }

        Notifications = await apiClient.GetNotificationsAsync(userName, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostReadAsync(Guid notificationId, CancellationToken cancellationToken)
    {
        var userName = HttpContext.Session.GetString("UserName");
        if (string.IsNullOrWhiteSpace(userName))
        {
            return RedirectToPage("/Login");
        }

        await apiClient.MarkNotificationReadAsync(notificationId, userName, cancellationToken);
        return RedirectToPage();
    }
}
