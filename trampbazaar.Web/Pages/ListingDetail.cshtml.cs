using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using trampbazaar.Shared.Contracts;
using trampbazaar.Web.Services;

namespace trampbazaar.Web.Pages;

public sealed class ListingDetailModel(MarketplaceWebApiClient apiClient) : PageModel
{
    public ListingDto? Listing { get; private set; }
    public SaleModeDto? SaleMode { get; private set; }
    public AuctionDto? Auction { get; private set; }
    public IReadOnlyList<ListingOfferDto> Offers { get; private set; } = [];
    public IReadOnlyList<AuctionBidDto> AuctionBids { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public string? StatusMessage { get; private set; }

    [BindProperty]
    public decimal OfferedPrice { get; set; }

    [BindProperty]
    public string OfferNote { get; set; } = string.Empty;

    [BindProperty]
    public decimal BidAmount { get; set; }

    public bool IsAuctionListing => string.Equals(Listing?.SaleMode, "auction", StringComparison.OrdinalIgnoreCase);
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(CurrentUserName);
    public bool CanSubmitOffer => IsAuthenticated && Listing is not null && !IsAuctionListing && !IsSeller;
    public bool CanSubmitAuctionBid => IsAuthenticated && Listing is not null && IsAuctionListing && !IsSeller;
    public bool CanManageOffers => IsAuthenticated && IsSeller && !IsAuctionListing;
    public bool CanMessageSeller => IsAuthenticated && Listing is not null && !IsSeller;
    private string? CurrentUserName => HttpContext.Session.GetString("UserName");
    private bool IsSeller => Listing is not null && string.Equals(Listing.SellerName, CurrentUserName, StringComparison.OrdinalIgnoreCase);

    public async Task<IActionResult> OnGetAsync(Guid listingId, CancellationToken cancellationToken)
    {
        return await LoadPageAsync(listingId, cancellationToken) ? Page() : NotFound();
    }

    public async Task<IActionResult> OnPostOfferAsync(Guid listingId, CancellationToken cancellationToken)
    {
        if (!await LoadPageAsync(listingId, cancellationToken))
        {
            return NotFound();
        }

        if (!CanSubmitOffer)
        {
            ErrorMessage = "Teklif vermek icin uygun oturum bulunamadi.";
            return Page();
        }

        if (OfferedPrice <= 0)
        {
            ErrorMessage = "Teklif tutari sifirdan buyuk olmalidir.";
            return Page();
        }

        var result = await apiClient.CreateListingOfferAsync(listingId, new CreateListingOfferRequest
        {
            BuyerName = CurrentUserName!,
            OfferedPrice = OfferedPrice,
            OfferNote = OfferNote
        }, cancellationToken);

        if (!result.IsSuccess)
        {
            ErrorMessage = result.ErrorMessage ?? "Teklif gonderilemedi.";
            return Page();
        }

        OfferedPrice = 0;
        OfferNote = string.Empty;
        await LoadPageAsync(listingId, cancellationToken);
        StatusMessage = "Teklif gonderildi.";
        return Page();
    }

    public async Task<IActionResult> OnPostBidAsync(Guid listingId, CancellationToken cancellationToken)
    {
        if (!await LoadPageAsync(listingId, cancellationToken))
        {
            return NotFound();
        }

        if (!CanSubmitAuctionBid)
        {
            ErrorMessage = "Acik artirmaya teklif verebilmek icin giris yapmalisiniz.";
            return Page();
        }

        if (BidAmount <= 0)
        {
            ErrorMessage = "Teklif tutari sifirdan buyuk olmalidir.";
            return Page();
        }

        var result = await apiClient.CreateAuctionBidAsync(listingId, new CreateAuctionBidRequest
        {
            BidderUserName = CurrentUserName!,
            BidAmount = BidAmount
        }, cancellationToken);

        if (!result.IsSuccess)
        {
            ErrorMessage = result.ErrorMessage ?? "Acik artirma teklifi gonderilemedi.";
            return Page();
        }

        BidAmount = 0;
        await LoadPageAsync(listingId, cancellationToken);
        StatusMessage = "Acik artirma teklifi gonderildi.";
        return Page();
    }

    public async Task<IActionResult> OnPostOfferStatusAsync(Guid listingId, Guid offerId, string status, CancellationToken cancellationToken)
    {
        if (!await LoadPageAsync(listingId, cancellationToken))
        {
            return NotFound();
        }

        if (!CanManageOffers)
        {
            ErrorMessage = "Teklifleri yonetme yetkiniz yok.";
            return Page();
        }

        var result = await apiClient.UpdateListingOfferStatusAsync(listingId, offerId, new UpdateListingOfferStatusRequest
        {
            ActorUserName = CurrentUserName!,
            Status = status
        }, cancellationToken);

        if (!result.IsSuccess)
        {
            ErrorMessage = result.ErrorMessage ?? "Teklif durumu guncellenemedi.";
            return Page();
        }

        await LoadPageAsync(listingId, cancellationToken);
        StatusMessage = status == "accepted" ? "Teklif kabul edildi." : "Teklif reddedildi.";
        return Page();
    }

    public async Task<IActionResult> OnPostMessageAsync(Guid listingId, CancellationToken cancellationToken)
    {
        if (!await LoadPageAsync(listingId, cancellationToken))
        {
            return NotFound();
        }

        if (!CanMessageSeller)
        {
            ErrorMessage = "Saticiya mesaj gonderebilmek icin giris yapmalisiniz.";
            return Page();
        }

        var conversation = await apiClient.StartListingConversationAsync(listingId, new StartListingConversationRequest
        {
            UserName = CurrentUserName!
        }, cancellationToken);

        if (conversation is null)
        {
            ErrorMessage = "Konusma baslatilamadi.";
            return Page();
        }

        StatusMessage = $"Konusma hazir: {conversation.CounterpartyUserName} ile iletisim baslatildi.";
        return Page();
    }

    private async Task<bool> LoadPageAsync(Guid listingId, CancellationToken cancellationToken)
    {
        Listing = await apiClient.GetListingAsync(listingId, cancellationToken);
        if (Listing is null)
        {
            return false;
        }

        var saleModes = await apiClient.GetSaleModesAsync(cancellationToken);
        SaleMode = saleModes.FirstOrDefault(mode => string.Equals(mode.Key, Listing.SaleMode, StringComparison.OrdinalIgnoreCase));

        if (IsAuctionListing)
        {
            Auction = await apiClient.GetListingAuctionAsync(listingId, cancellationToken);
            AuctionBids = await apiClient.GetListingAuctionBidsAsync(listingId, cancellationToken);
            Offers = [];
        }
        else
        {
            Offers = await apiClient.GetListingOffersAsync(listingId, cancellationToken);
            Auction = null;
            AuctionBids = [];
        }

        return true;
    }
}
