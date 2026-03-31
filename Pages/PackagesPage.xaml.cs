using trampbazaar.Services;
using trampbazaar.ViewModels;

namespace trampbazaar.Pages;

public partial class PackagesPage : ContentPage
{
    private readonly PackagesPageViewModel viewModel;

    public PackagesPage() : this(ServiceHelper.GetService<PackagesPageViewModel>())
    {
    }

    public PackagesPage(PackagesPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.LoadAsync();
    }

    private async void OnPurchaseClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && Guid.TryParse(button.CommandParameter?.ToString(), out var packageId))
        {
            try
            {
                var result = await viewModel.PurchaseAsync(packageId);
                await DisplayAlert("Basarili", result.Message, "Tamam");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Bilgi", ex.Message, "Tamam");
            }
        }
    }
}
