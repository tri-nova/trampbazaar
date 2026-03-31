using trampbazaar.Models;
using trampbazaar.Services;
using trampbazaar.Shared.Contracts;

namespace trampbazaar.ViewModels;

public sealed class CreateListingPageViewModel(IMarketplaceDataService marketplaceDataService, SessionStateService sessionStateService) : BaseViewModel
{
    public ListingFormModel Form { get; } = new()
    {
        SellerName = sessionStateService.UserName
    };

    public IList<CategoryDto> Categories { get; private set; } = [];

    public IList<SaleModeDto> SaleModes { get; private set; } = [];

    public bool IsAuctionMode => string.Equals(Form.SaleModeKey, "auction", StringComparison.OrdinalIgnoreCase);

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            Form.SellerName = sessionStateService.IsAuthenticated
                ? sessionStateService.UserName
                : string.Empty;

            Categories = (await marketplaceDataService.GetCategoriesAsync(cancellationToken)).ToList();
            SaleModes = (await marketplaceDataService.GetSaleModesAsync(cancellationToken)).ToList();
            if (string.IsNullOrWhiteSpace(Form.CategorySlug) && Categories.Count > 0)
            {
                Form.CategorySlug = Categories[0].Slug;
            }

            if (string.IsNullOrWhiteSpace(Form.SaleModeKey) && SaleModes.Count > 0)
            {
                Form.SaleModeKey = SaleModes[0].Key;
            }

            OnPropertyChanged(nameof(Categories));
            OnPropertyChanged(nameof(SaleModes));
            OnPropertyChanged(nameof(Form));
            OnPropertyChanged(nameof(IsAuctionMode));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<ListingDto?> SaveAsync(CancellationToken cancellationToken = default)
    {
        if (!sessionStateService.IsAuthenticated)
        {
            ErrorMessage = "Ilan vermek icin once giris yapin.";
            return null;
        }

        if (string.IsNullOrWhiteSpace(Form.Title) ||
            string.IsNullOrWhiteSpace(Form.Description) ||
            string.IsNullOrWhiteSpace(Form.CategorySlug) ||
            string.IsNullOrWhiteSpace(Form.SaleModeKey))
        {
            ErrorMessage = "Tum zorunlu alanlari doldurun.";
            return null;
        }

        if (IsAuctionMode)
        {
            if (Form.AuctionStartPrice <= 0 || Form.AuctionMinBidIncrement <= 0)
            {
                ErrorMessage = "Acik artirma icin baslangic fiyati ve minimum artirim zorunludur.";
                return null;
            }

            var startsAt = Form.AuctionStartDate.Date + Form.AuctionStartTime;
            var endsAt = Form.AuctionEndDate.Date + Form.AuctionEndTime;
            if (endsAt <= startsAt)
            {
                ErrorMessage = "Bitis tarihi baslangictan sonra olmali.";
                return null;
            }
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var listing = await marketplaceDataService.CreateListingAsync(Form, cancellationToken);
            StatusMessage = $"Ilan olusturuldu: {listing.Title}";
            ResetForm();
            return listing;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void NotifySaleModeChanged()
    {
        OnPropertyChanged(nameof(Form));
        OnPropertyChanged(nameof(IsAuctionMode));
    }

    private void ResetForm()
    {
        Form.Title = string.Empty;
        Form.Description = string.Empty;
        Form.Price = 0;
        Form.AuctionStartPrice = 0;
        Form.AuctionMinBidIncrement = 1;
        Form.AuctionStartDate = DateTime.Today;
        Form.AuctionStartTime = new TimeSpan(9, 0, 0);
        Form.AuctionEndDate = DateTime.Today.AddDays(1);
        Form.AuctionEndTime = new TimeSpan(18, 0, 0);
        Form.AuctionAutoExtendMinutes = 0;
        Form.SellerName = sessionStateService.UserName;
        OnPropertyChanged(nameof(Form));
        OnPropertyChanged(nameof(IsAuctionMode));
    }
}
