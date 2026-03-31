using trampbazaar.Services;
using trampbazaar.ViewModels;

namespace trampbazaar.Pages;

public partial class AccountPage : ContentPage
{
    private readonly AccountPageViewModel viewModel;

    public AccountPage() : this(ServiceHelper.GetService<AccountPageViewModel>())
    {
    }

    public AccountPage(AccountPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.LoadAsync();
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(LoginPage));
    }

    private async void OnRegisterClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RegisterPage));
    }

    private async void OnMessagesClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//Messages");
    }

    private async void OnNotificationsClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//Notifications");
    }

    private async void OnPackagesClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(PackagesPage));
    }

    private void OnSignOutClicked(object? sender, EventArgs e)
    {
        viewModel.SignOut();
    }
}
