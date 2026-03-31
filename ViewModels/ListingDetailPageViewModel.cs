using trampbazaar.Models;
using trampbazaar.Services;
using trampbazaar.Shared.Contracts;
using System.Collections.ObjectModel;

namespace trampbazaar.ViewModels;

public sealed class ListingDetailPageViewModel(IMarketplaceDataService marketplaceDataService, SessionStateService sessionStateService) : BaseViewModel
{
    private ListingDto? listing;
    private SaleModeDto? saleMode;
    private AuctionDto? auction;
    private Guid listingId;

    public ListingOfferFormModel OfferForm { get; } = new();
    public AuctionBidFormModel AuctionBidForm { get; } = new();

    public ObservableCollection<ListingOfferDto> Offers { get; } = [];
    public ObservableCollection<AuctionBidDto> AuctionBids { get; } = [];

    public bool IsAuctionListing => string.Equals(Listing?.SaleMode, "auction", StringComparison.OrdinalIgnoreCase);
    public bool CanSubmitOffer => sessionStateService.IsAuthenticated && Listing is not null && !IsAuctionListing;
    public bool CanSubmitAuctionBid => sessionStateService.IsAuthenticated && Listing is not null && IsAuctionListing && Listing.SellerName != sessionStateService.UserName;
    public bool CanManageOffers => sessionStateService.IsAuthenticated && Listing?.SellerName == sessionStateService.UserName;
    public bool CanMessageSeller => sessionStateService.IsAuthenticated && Listing is not null && Listing.SellerName != sessionStateService.UserName;
    public bool CanReportListing => sessionStateService.IsAuthenticated && Listing is not null && Listing.SellerName != sessionStateService.UserName;

    public ListingDto? Listing
    {
        get => listing;
        private set
        {
            if (SetProperty(ref listing, value))
            {
                OnPropertyChanged(nameof(IsAuctionListing));
                OnPropertyChanged(nameof(CanSubmitOffer));
                OnPropertyChanged(nameof(CanSubmitAuctionBid));
                OnPropertyChanged(nameof(CanManageOffers));
                OnPropertyChanged(nameof(CanMessageSeller));
                OnPropertyChanged(nameof(CanReportListing));
            }
        }
    }

    public SaleModeDto? SaleMode
    {
        get => saleMode;
        private set => SetProperty(ref saleMode, value);
    }

    public AuctionDto? Auction
    {
        get => auction;
        private set => SetProperty(ref auction, value);
    }

    public async Task LoadAsync(Guid listingId, CancellationToken cancellationToken = default)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            this.listingId = listingId;

            var state = await marketplaceDataService.GetListingDetailAsync(listingId, cancellationToken);
            Listing = state.Listing;
            SaleMode = state.SaleMode;
            Auction = state.Auction;
            Replace(Offers, state.Offers);
            Replace(AuctionBids, state.AuctionBids);

            if (Listing is null)
            {
                ErrorMessage = "Ilan bulunamadi.";
            }
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

    public async Task<bool> SubmitOfferAsync(CancellationToken cancellationToken = default)
    {
        if (!sessionStateService.IsAuthenticated)
        {
            ErrorMessage = "Teklif vermek icin once giris yapin.";
            return false;
        }

        if (listingId == Guid.Empty || Listing is null)
        {
            ErrorMessage = "Teklif verilecek ilan bulunamadi.";
            return false;
        }

        if (OfferForm.OfferedPrice <= 0)
        {
            ErrorMessage = "Teklif tutari sifirdan buyuk olmalidir.";
            return false;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            await marketplaceDataService.CreateListingOfferAsync(listingId, OfferForm, cancellationToken);
            var offers = await marketplaceDataService.GetListingOffersAsync(listingId, cancellationToken);
            Replace(Offers, offers);
            OfferForm.OfferedPrice = 0;
            OfferForm.OfferNote = string.Empty;
            OnPropertyChanged(nameof(OfferForm));
            StatusMessage = "Teklif gonderildi.";
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

    public async Task<bool> SubmitAuctionBidAsync(CancellationToken cancellationToken = default)
    {
        if (!sessionStateService.IsAuthenticated)
        {
            ErrorMessage = "Teklif vermek icin once giris yapin.";
            return false;
        }

        if (listingId == Guid.Empty || Listing is null || Auction is null)
        {
            ErrorMessage = "Acik artirma bulunamadi.";
            return false;
        }

        if (AuctionBidForm.BidAmount <= 0)
        {
            ErrorMessage = "Teklif tutari sifirdan buyuk olmalidir.";
            return false;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            await marketplaceDataService.CreateAuctionBidAsync(listingId, AuctionBidForm, cancellationToken);
            var state = await marketplaceDataService.GetListingDetailAsync(listingId, cancellationToken);
            Listing = state.Listing;
            Auction = state.Auction;
            Replace(AuctionBids, state.AuctionBids);
            AuctionBidForm.BidAmount = 0;
            OnPropertyChanged(nameof(AuctionBidForm));
            StatusMessage = "Acik artirma teklifi gonderildi.";
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

    public async Task<bool> UpdateOfferStatusAsync(Guid offerId, string status, CancellationToken cancellationToken = default)
    {
        if (!CanManageOffers)
        {
            ErrorMessage = "Teklifleri yonetme yetkiniz yok.";
            return false;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var result = await marketplaceDataService.UpdateListingOfferStatusAsync(listingId, offerId, status, cancellationToken);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Message;
                return false;
            }

            Replace(Offers, await marketplaceDataService.GetListingOffersAsync(listingId, cancellationToken));
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

    public async Task<ConversationDetailDto> StartConversationAsync(CancellationToken cancellationToken = default)
    {
        if (!sessionStateService.IsAuthenticated)
        {
            throw new InvalidOperationException("Mesaj gondermek icin once giris yapin.");
        }

        if (listingId == Guid.Empty || Listing is null)
        {
            throw new InvalidOperationException("Ilan bulunamadi.");
        }

        return await marketplaceDataService.StartListingConversationAsync(listingId, cancellationToken);
    }

    private static void Replace<T>(ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }
}
