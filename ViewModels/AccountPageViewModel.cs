using trampbazaar.Services;

namespace trampbazaar.ViewModels;

public sealed class AccountPageViewModel : BaseViewModel
{
    private readonly IMarketplaceDataService marketplaceDataService;
    private readonly SessionStateService sessionStateService;
    private string userName;
    private bool isAuthenticated;
    private int unreadNotificationCount;

    public AccountPageViewModel(SessionStateService sessionStateService, IMarketplaceDataService marketplaceDataService)
    {
        this.sessionStateService = sessionStateService;
        this.marketplaceDataService = marketplaceDataService;
        userName = sessionStateService.UserName;
        isAuthenticated = sessionStateService.IsAuthenticated;

        sessionStateService.SessionChanged += (_, _) => Refresh();
    }

    public string UserName
    {
        get => userName;
        private set => SetProperty(ref userName, value);
    }

    public bool IsAuthenticated
    {
        get => isAuthenticated;
        private set => SetProperty(ref isAuthenticated, value);
    }

    public int UnreadNotificationCount
    {
        get => unreadNotificationCount;
        private set => SetProperty(ref unreadNotificationCount, value);
    }

    public void Refresh()
    {
        UserName = sessionStateService.UserName;
        IsAuthenticated = sessionStateService.IsAuthenticated;
        StatusMessage = sessionStateService.IsAuthenticated
            ? "Oturum acik."
            : "Misafir modunda geziyorsunuz.";
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        Refresh();

        if (!sessionStateService.IsAuthenticated)
        {
            UnreadNotificationCount = 0;
            return;
        }

        try
        {
            var notifications = await marketplaceDataService.GetNotificationsAsync(cancellationToken);
            UnreadNotificationCount = notifications.Count(x => !x.IsRead);
        }
        catch
        {
            UnreadNotificationCount = 0;
        }
    }

    public void SignOut()
    {
        sessionStateService.SignOut();
        Refresh();
    }
}
