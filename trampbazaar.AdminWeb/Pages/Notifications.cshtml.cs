using Microsoft.AspNetCore.Mvc.RazorPages;
using trampbazaar.AdminWeb.Services;
using trampbazaar.Shared.Contracts;

namespace trampbazaar.AdminWeb.Pages;

public sealed class NotificationsModel(AdminApiClient apiClient) : PageModel
{
    public IReadOnlyList<NotificationDto> Notifications { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Notifications = await apiClient.GetNotificationsAsync(cancellationToken);
    }
}
