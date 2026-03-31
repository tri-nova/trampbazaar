using System.Collections.ObjectModel;
using trampbazaar.Services;
using trampbazaar.Shared.Contracts;

namespace trampbazaar.ViewModels;

public sealed class NotificationsPageViewModel(IMarketplaceDataService marketplaceDataService, SessionStateService sessionStateService) : BaseViewModel
{
    public ObservableCollection<NotificationDto> Notifications { get; } = [];

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!sessionStateService.IsAuthenticated)
        {
            Notifications.Clear();
            ErrorMessage = "Bildirimleri gormek icin once giris yapin.";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            var items = await marketplaceDataService.GetNotificationsAsync(cancellationToken);
            Replace(Notifications, items);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<bool> MarkReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var success = await marketplaceDataService.MarkNotificationReadAsync(notificationId, cancellationToken);
        if (success)
        {
            await LoadAsync(cancellationToken);
        }

        return success;
    }

    private static void Replace<T>(ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }
}
