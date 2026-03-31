using trampbazaar.Services;
using trampbazaar.ViewModels;

namespace trampbazaar.Pages;

[QueryProperty(nameof(ListingId), "listingId")]
public partial class ListingDetailPage : ContentPage
{
    private readonly ListingDetailPageViewModel viewModel;
    private Guid listingId;

    public ListingDetailPage() : this(ServiceHelper.GetService<ListingDetailPageViewModel>())
    {
    }

    public ListingDetailPage(ListingDetailPageViewModel viewModel)
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
            await viewModel.LoadAsync(listingId);
        }
    }

    private async void OnMessageClicked(object? sender, EventArgs e)
    {
        try
        {
            var conversation = await viewModel.StartConversationAsync();
            await Shell.Current.GoToAsync($"{nameof(ConversationDetailPage)}?conversationId={conversation.Id}");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Bilgi", ex.Message, "Tamam");
        }
    }

    private async void OnReportClicked(object? sender, EventArgs e)
    {
        if (listingId != Guid.Empty)
        {
            await Shell.Current.GoToAsync($"{nameof(ComplaintPage)}?listingId={listingId}");
        }
    }

    private async void OnSubmitOfferClicked(object? sender, EventArgs e)
    {
        var success = await viewModel.SubmitOfferAsync();
        if (success)
        {
            await DisplayAlert("Basarili", viewModel.StatusMessage ?? "Teklif gonderildi.", "Tamam");
        }
    }

    private async void OnSubmitAuctionBidClicked(object? sender, EventArgs e)
    {
        var success = await viewModel.SubmitAuctionBidAsync();
        if (success)
        {
            await DisplayAlert("Basarili", viewModel.StatusMessage ?? "Acik artirma teklifi gonderildi.", "Tamam");
        }
    }

    private async void OnAcceptOfferClicked(object? sender, EventArgs e)
    {
        if (sender is Button button &&
            Guid.TryParse(button.CommandParameter?.ToString(), out var offerId))
        {
            var success = await viewModel.UpdateOfferStatusAsync(offerId, "accepted");
            if (success)
            {
                await DisplayAlert("Basarili", viewModel.StatusMessage ?? "Teklif kabul edildi.", "Tamam");
            }
        }
    }

    private async void OnRejectOfferClicked(object? sender, EventArgs e)
    {
        if (sender is Button button &&
            Guid.TryParse(button.CommandParameter?.ToString(), out var offerId))
        {
            var success = await viewModel.UpdateOfferStatusAsync(offerId, "rejected");
            if (success)
            {
                await DisplayAlert("Basarili", viewModel.StatusMessage ?? "Teklif reddedildi.", "Tamam");
            }
        }
    }
}
