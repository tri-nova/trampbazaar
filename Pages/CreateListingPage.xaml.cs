using trampbazaar.Shared.Contracts;
using trampbazaar.Services;
using trampbazaar.ViewModels;

namespace trampbazaar.Pages;

public partial class CreateListingPage : ContentPage
{
    private readonly CreateListingPageViewModel viewModel;
    private readonly SessionStateService sessionStateService;

    public CreateListingPage() : this(
        ServiceHelper.GetService<CreateListingPageViewModel>(),
        ServiceHelper.GetService<SessionStateService>())
    {
    }

    public CreateListingPage(CreateListingPageViewModel viewModel, SessionStateService sessionStateService)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
        this.sessionStateService = sessionStateService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!sessionStateService.IsAuthenticated)
        {
            await DisplayAlert("Giris Gerekli", "Ilan vermek icin once giris yapin.", "Tamam");
            await Shell.Current.GoToAsync(nameof(LoginPage));
            return;
        }

        await viewModel.InitializeAsync();
        SyncPickers();
    }

    private void OnCategoryChanged(object? sender, EventArgs e)
    {
        if (CategoryPicker.SelectedItem is CategoryDto category)
        {
            viewModel.Form.CategorySlug = category.Slug;
        }
    }

    private void OnSaleModeChanged(object? sender, EventArgs e)
    {
        if (SaleModePicker.SelectedItem is SaleModeDto saleMode)
        {
            viewModel.Form.SaleModeKey = saleMode.Key;
            if (saleMode.Key == "auction" && viewModel.Form.AuctionStartPrice <= 0)
            {
                viewModel.Form.AuctionStartPrice = viewModel.Form.Price > 0 ? viewModel.Form.Price : 1;
            }

            viewModel.NotifySaleModeChanged();
        }
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        var createdListing = await viewModel.SaveAsync();
        if (createdListing is not null)
        {
            await DisplayAlert("Basarili", viewModel.StatusMessage ?? "Ilan olusturuldu.", "Tamam");
            await Shell.Current.GoToAsync($"//Listings");
            await Shell.Current.GoToAsync($"{nameof(ListingDetailPage)}?listingId={createdListing.Id}");
        }
    }

    private void SyncPickers()
    {
        CategoryPicker.SelectedItem = viewModel.Categories.FirstOrDefault(x => x.Slug == viewModel.Form.CategorySlug);
        SaleModePicker.SelectedItem = viewModel.SaleModes.FirstOrDefault(x => x.Key == viewModel.Form.SaleModeKey);
    }
}
