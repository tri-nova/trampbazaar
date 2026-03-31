using trampbazaar.Pages;
using trampbazaar.Services;

namespace trampbazaar;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Items.Add(new TabBar
        {
            Items =
            {
                new ShellContent
                {
                    Title = "Home",
                    Route = "Home",
                    ContentTemplate = new DataTemplate(() => ServiceHelper.GetService<MainPage>())
                },
                new ShellContent
                {
                    Title = "Listings",
                    Route = "Listings",
                    ContentTemplate = new DataTemplate(() => ServiceHelper.GetService<ListingsPage>())
                },
                new ShellContent
                {
                    Title = "Create",
                    Route = "CreateListing",
                    ContentTemplate = new DataTemplate(() => ServiceHelper.GetService<CreateListingPage>())
                },
                new ShellContent
                {
                    Title = "Account",
                    Route = "Account",
                    ContentTemplate = new DataTemplate(() => ServiceHelper.GetService<AccountPage>())
                },
                new ShellContent
                {
                    Title = "Messages",
                    Route = "Messages",
                    ContentTemplate = new DataTemplate(() => ServiceHelper.GetService<ConversationsPage>())
                },
                new ShellContent
                {
                    Title = "Notifications",
                    Route = "Notifications",
                    ContentTemplate = new DataTemplate(() => ServiceHelper.GetService<NotificationsPage>())
                }
            }
        });

        Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
        Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));
        Routing.RegisterRoute(nameof(ListingDetailPage), typeof(ListingDetailPage));
        Routing.RegisterRoute(nameof(ConversationDetailPage), typeof(ConversationDetailPage));
        Routing.RegisterRoute(nameof(PackagesPage), typeof(PackagesPage));
        Routing.RegisterRoute(nameof(ComplaintPage), typeof(ComplaintPage));
    }
}
