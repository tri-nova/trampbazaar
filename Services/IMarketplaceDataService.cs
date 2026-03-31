using trampbazaar.Models;
using trampbazaar.Shared.Contracts;

namespace trampbazaar.Services;

public interface IMarketplaceDataService
{
    Task<MarketplaceSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SaleModeDto>> GetSaleModesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PackageDto>> GetPackagesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ListingDto>> GetListingsAsync(ListingFilter filter, CancellationToken cancellationToken = default);

    Task<ListingDetailState> GetListingDetailAsync(Guid listingId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ListingOfferDto>> GetListingOffersAsync(Guid listingId, CancellationToken cancellationToken = default);

    Task<ListingOfferDto> CreateListingOfferAsync(Guid listingId, ListingOfferFormModel form, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuctionBidDto>> GetAuctionBidsAsync(Guid listingId, CancellationToken cancellationToken = default);

    Task<AuctionBidDto> CreateAuctionBidAsync(Guid listingId, AuctionBidFormModel form, CancellationToken cancellationToken = default);

    Task<OfferDecisionResult> UpdateListingOfferStatusAsync(Guid listingId, Guid offerId, string status, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConversationSummaryDto>> GetConversationsAsync(CancellationToken cancellationToken = default);

    Task<ConversationDetailDto?> GetConversationDetailAsync(Guid conversationId, CancellationToken cancellationToken = default);

    Task<ConversationDetailDto> StartListingConversationAsync(Guid listingId, CancellationToken cancellationToken = default);

    Task<MessageDto> SendMessageAsync(Guid conversationId, MessageFormModel form, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(CancellationToken cancellationToken = default);

    Task<bool> MarkNotificationReadAsync(Guid notificationId, CancellationToken cancellationToken = default);

    Task<PaymentResultDto> PurchasePackageAsync(Guid packageId, CancellationToken cancellationToken = default);

    Task<ComplaintResultDto> SubmitComplaintAsync(ComplaintFormModel form, CancellationToken cancellationToken = default);

    Task<ListingDto> CreateListingAsync(ListingFormModel form, CancellationToken cancellationToken = default);

    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
}
