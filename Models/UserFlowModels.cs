using trampbazaar.Shared.Contracts;

namespace trampbazaar.Models;

public sealed class AuthResult
{
    public bool IsSuccess { get; init; }
    public string Message { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
}

public sealed class LoginRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public sealed class RegisterRequest
{
    public string FullName { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string AccountType { get; init; } = "individual";
}

public sealed class ListingFormModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SellerName { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;
    public string SaleModeKey { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal AuctionStartPrice { get; set; }
    public decimal AuctionMinBidIncrement { get; set; } = 1;
    public DateTime AuctionStartDate { get; set; } = DateTime.Today;
    public TimeSpan AuctionStartTime { get; set; } = new(9, 0, 0);
    public DateTime AuctionEndDate { get; set; } = DateTime.Today.AddDays(1);
    public TimeSpan AuctionEndTime { get; set; } = new(18, 0, 0);
    public int AuctionAutoExtendMinutes { get; set; }
}

public sealed class ListingFilter
{
    public string? CategorySlug { get; set; }
    public string? SaleModeKey { get; set; }
}

public sealed class ListingDetailState
{
    public ListingDto? Listing { get; init; }
    public SaleModeDto? SaleMode { get; init; }
    public IReadOnlyList<ListingOfferDto> Offers { get; init; } = Array.Empty<ListingOfferDto>();
    public AuctionDto? Auction { get; init; }
    public IReadOnlyList<AuctionBidDto> AuctionBids { get; init; } = Array.Empty<AuctionBidDto>();
}

public sealed class ListingOfferFormModel
{
    public decimal OfferedPrice { get; set; }
    public string OfferNote { get; set; } = string.Empty;
}

public sealed class AuctionBidFormModel
{
    public decimal BidAmount { get; set; }
}

public sealed class OfferDecisionResult
{
    public bool IsSuccess { get; init; }
    public string Message { get; init; } = string.Empty;
}

public sealed class ConversationListState
{
    public IReadOnlyList<ConversationSummaryDto> Conversations { get; init; } = Array.Empty<ConversationSummaryDto>();
}

public sealed class ConversationThreadState
{
    public ConversationDetailDto? Conversation { get; init; }
}

public sealed class MessageFormModel
{
    public string MessageText { get; set; } = string.Empty;
}

public sealed class NotificationsState
{
    public IReadOnlyList<NotificationDto> Notifications { get; init; } = Array.Empty<NotificationDto>();
}

public sealed class ComplaintFormModel
{
    public string TargetEntityType { get; set; } = "listing";
    public Guid TargetEntityId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
