using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using trampbazaar.Shared.Contracts;
using trampbazaar.Web.Services;

namespace trampbazaar.Web.Pages;

public sealed class NotificationsModel(MarketplaceWebApiClient apiClient) : PageModel
{
    public IReadOnlyList<NotificationDto> Notifications { get; private set; } = [];
    public string? ErrorMessage { get; private set; }

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

    public async Task<IActionResult> OnPostOpenAsync(Guid notificationId, CancellationToken cancellationToken)
    {
        var userName = HttpContext.Session.GetString("UserName");
        if (string.IsNullOrWhiteSpace(userName))
        {
            return RedirectToPage("/Login");
        }

        var notifications = await apiClient.GetNotificationsAsync(userName, cancellationToken);
        var notification = notifications.FirstOrDefault(item => item.Id == notificationId);
        if (notification is null)
        {
            return NotFound();
        }

        if (!notification.IsRead)
        {
            await apiClient.MarkNotificationReadAsync(notificationId, userName, cancellationToken);
        }

        return ResolveNotificationRedirect(notification);
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

    private IActionResult ResolveNotificationRedirect(NotificationDto notification)
    {
        if (notification.RelatedEntityId.HasValue &&
            string.Equals(notification.RelatedEntityType, "listing", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToPage("/ListingDetail", new { listingId = notification.RelatedEntityId.Value });
        }

        if (notification.RelatedEntityId.HasValue &&
            string.Equals(notification.RelatedEntityType, "conversation", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToPage("/ConversationDetail", new { conversationId = notification.RelatedEntityId.Value });
        }

        return RedirectToPage();
    }
}
