using trampbazaar.Services;
using trampbazaar.ViewModels;

namespace trampbazaar.Pages;

public partial class RegisterPage : ContentPage
{
    private readonly RegisterPageViewModel viewModel;

    public RegisterPage() : this(ServiceHelper.GetService<RegisterPageViewModel>())
    {
    }

    public RegisterPage(RegisterPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
        AccountTypePicker.SelectedIndex = 0;
    }

    private void OnAccountTypeChanged(object? sender, EventArgs e)
    {
        viewModel.AccountType = AccountTypePicker.SelectedItem?.ToString() ?? "individual";
    }

    private async void OnRegisterClicked(object? sender, EventArgs e)
    {
        var success = await viewModel.RegisterAsync();
        if (success)
        {
            await DisplayAlert("Basarili", viewModel.StatusMessage ?? "Kayit olusturuldu.", "Tamam");
            await Shell.Current.GoToAsync("//Home");
        }
    }
}
