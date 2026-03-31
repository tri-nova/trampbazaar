using trampbazaar.Models;
using trampbazaar.Services;

namespace trampbazaar.ViewModels;

public sealed class ComplaintPageViewModel(IMarketplaceDataService marketplaceDataService, SessionStateService sessionStateService) : BaseViewModel
{
    private string listingTitle = string.Empty;

    public ComplaintFormModel Form { get; } = new();

    public string ListingTitle
    {
        get => listingTitle;
        private set => SetProperty(ref listingTitle, value);
    }

    public async Task InitializeAsync(Guid listingId, CancellationToken cancellationToken = default)
    {
        Form.TargetEntityType = "listing";
        Form.TargetEntityId = listingId;

        var detail = await marketplaceDataService.GetListingDetailAsync(listingId, cancellationToken);
        ListingTitle = detail.Listing?.Title ?? "Ilan";
        if (string.IsNullOrWhiteSpace(Form.Subject))
        {
            Form.Subject = $"{ListingTitle} ilan sikayeti";
            OnPropertyChanged(nameof(Form));
        }
    }

    public async Task<bool> SubmitAsync(CancellationToken cancellationToken = default)
    {
        if (!sessionStateService.IsAuthenticated)
        {
            ErrorMessage = "Sikayet gondermek icin once giris yapin.";
            return false;
        }

        if (Form.TargetEntityId == Guid.Empty || string.IsNullOrWhiteSpace(Form.Subject) || string.IsNullOrWhiteSpace(Form.Description))
        {
            ErrorMessage = "Tum alanlari doldurun.";
            return false;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            var result = await marketplaceDataService.SubmitComplaintAsync(Form, cancellationToken);
            StatusMessage = result.Message;
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
