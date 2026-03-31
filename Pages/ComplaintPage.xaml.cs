using trampbazaar.Services;
using trampbazaar.ViewModels;

namespace trampbazaar.Pages;

[QueryProperty(nameof(ListingId), "listingId")]
public partial class ComplaintPage : ContentPage
{
    private readonly ComplaintPageViewModel viewModel;
    private Guid listingId;

    public ComplaintPage() : this(ServiceHelper.GetService<ComplaintPageViewModel>())
    {
    }

    public ComplaintPage(ComplaintPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }

    public string? ListingId
    {
        get => listingId.ToString();
        set
        {
            if (Guid.TryParse(value, out var parsed))
            {
                listingId = parsed;
            }
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (listingId != Guid.Empty)
        {
            await viewModel.InitializeAsync(listingId);
        }
    }

    private async void OnSubmitClicked(object? sender, EventArgs e)
    {
        var success = await viewModel.SubmitAsync();
        if (success)
        {
            await DisplayAlert("Basarili", viewModel.StatusMessage ?? "Sikayet gonderildi.", "Tamam");
            await Shell.Current.GoToAsync("..");
        }
        else if (!string.IsNullOrWhiteSpace(viewModel.ErrorMessage))
        {
            await DisplayAlert("Bilgi", viewModel.ErrorMessage, "Tamam");
        }
    }
}
