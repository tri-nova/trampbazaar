using trampbazaar.ViewModels;

namespace trampbazaar;

public partial class MainPage : ContentPage
{
    private readonly MainPageViewModel viewModel;

    public MainPage(MainPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.InitializeAsync();
    }

    private void OnSaleModeClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        viewModel.SelectSaleMode(button.CommandParameter?.ToString());
    }

    private async void OnBrowseListingsClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//Listings");
    }

    private async void OnCreateListingClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//CreateListing");
    }
}
