using Microsoft.Extensions.Logging;
using trampbazaar.Configuration;
using trampbazaar.Pages;
using trampbazaar.Services;
using trampbazaar.ViewModels;

namespace trampbazaar;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        var options = new MarketplaceOptions();
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<SessionStateService>();

        if (options.UseMockData)
        {
            builder.Services.AddSingleton<IMarketplaceDataService, MockMarketplaceDataService>();
        }
        else
        {
            builder.Services.AddSingleton(new HttpClient
            {
                BaseAddress = new Uri(options.ApiBaseUrl)
            });
            builder.Services.AddSingleton<IMarketplaceDataService, ApiMarketplaceDataService>();
        }

        builder.Services.AddSingleton<MainPageViewModel>();
        builder.Services.AddTransient<ListingsPageViewModel>();
        builder.Services.AddTransient<ListingDetailPageViewModel>();
        builder.Services.AddTransient<ConversationsPageViewModel>();
        builder.Services.AddTransient<ConversationDetailPageViewModel>();
        builder.Services.AddTransient<NotificationsPageViewModel>();
        builder.Services.AddTransient<CreateListingPageViewModel>();
        builder.Services.AddTransient<PackagesPageViewModel>();
        builder.Services.AddTransient<ComplaintPageViewModel>();
        builder.Services.AddTransient<LoginPageViewModel>();
        builder.Services.AddTransient<RegisterPageViewModel>();
        builder.Services.AddSingleton<AccountPageViewModel>();

        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<ListingsPage>();
        builder.Services.AddTransient<CreateListingPage>();
        builder.Services.AddTransient<AccountPage>();
        builder.Services.AddTransient<ListingDetailPage>();
        builder.Services.AddTransient<ConversationsPage>();
        builder.Services.AddTransient<ConversationDetailPage>();
        builder.Services.AddTransient<NotificationsPage>();
        builder.Services.AddTransient<PackagesPage>();
        builder.Services.AddTransient<ComplaintPage>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddSingleton<AppShell>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();
        ServiceHelper.Initialize(app.Services);
        return app;
    }
}
