using trampbazaar.Services;
using trampbazaar.ViewModels;

namespace trampbazaar.Pages;

public partial class ListingsPage : ContentPage
{
    private readonly ListingsPageViewModel viewModel;

    public ListingsPage() : this(ServiceHelper.GetService<ListingsPageViewModel>())
    {
    }

    public ListingsPage(ListingsPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.InitializeAsync();
        await viewModel.LoadListingsAsync();
    }

    private async void OnFilterClicked(object? sender, EventArgs e)
    {
        await viewModel.LoadListingsAsync();
    }

    private async void OnViewDetailClicked(object? sender, EventArgs e)
    {
        if (sender is Button button &&
            Guid.TryParse(button.CommandParameter?.ToString(), out var listingId))
        {
            await Shell.Current.GoToAsync($"{nameof(ListingDetailPage)}?listingId={listingId}");
        }
    }
}
