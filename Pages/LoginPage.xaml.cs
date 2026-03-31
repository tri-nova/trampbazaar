using trampbazaar.Services;
using trampbazaar.ViewModels;

namespace trampbazaar.Pages;

public partial class LoginPage : ContentPage
{
    private readonly LoginPageViewModel viewModel;

    public LoginPage() : this(ServiceHelper.GetService<LoginPageViewModel>())
    {
    }

    public LoginPage(LoginPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        var success = await viewModel.LoginAsync();
        if (success)
        {
            await DisplayAlert("Basarili", viewModel.StatusMessage ?? "Giris basarili.", "Tamam");
            await Shell.Current.GoToAsync("//Home");
        }
    }
}
