using trampbazaar.Services;
using trampbazaar.ViewModels;

namespace trampbazaar.Pages;

public partial class NotificationsPage : ContentPage
{
    private readonly NotificationsPageViewModel viewModel;

    public NotificationsPage() : this(ServiceHelper.GetService<NotificationsPageViewModel>())
    {
    }

    public NotificationsPage(NotificationsPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.LoadAsync();
    }

    private async void OnMarkReadClicked(object? sender, EventArgs e)
    {
        if (sender is Button button &&
            Guid.TryParse(button.CommandParameter?.ToString(), out var notificationId))
        {
            await viewModel.MarkReadAsync(notificationId);
        }
    }

    private async void OnOpenNotificationClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button ||
            Guid.TryParse(button.CommandParameter?.ToString(), out var notificationId) is false)
        {
            return;
        }

        var notification = viewModel.Notifications.FirstOrDefault(x => x.Id == notificationId);
        if (notification is null)
        {
            return;
        }

        await viewModel.MarkReadAsync(notificationId);

        if (string.Equals(notification.RelatedEntityType, "conversation", StringComparison.OrdinalIgnoreCase) &&
            notification.RelatedEntityId.HasValue)
        {
            await Shell.Current.GoToAsync($"{nameof(ConversationDetailPage)}?conversationId={notification.RelatedEntityId.Value}");
            return;
        }

        if (string.Equals(notification.RelatedEntityType, "listing", StringComparison.OrdinalIgnoreCase) &&
            notification.RelatedEntityId.HasValue)
        {
            await Shell.Current.GoToAsync($"{nameof(ListingDetailPage)}?listingId={notification.RelatedEntityId.Value}");
        }
    }
}
