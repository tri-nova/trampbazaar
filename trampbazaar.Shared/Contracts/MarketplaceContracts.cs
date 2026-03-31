namespace trampbazaar.Shared.Contracts;

public sealed class DashboardResponse
{
    public string PlatformName { get; set; } = string.Empty;
    public IReadOnlyList<QuickStatDto> QuickStats { get; set; } = Array.Empty<QuickStatDto>();
    public IReadOnlyList<SaleModeDto> SaleModes { get; set; } = Array.Empty<SaleModeDto>();
    public IReadOnlyList<FeatureDto> Features { get; set; } = Array.Empty<FeatureDto>();
}

public sealed class QuickStatDto
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public sealed class SaleModeDto
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IReadOnlyList<string> Steps { get; set; } = Array.Empty<string>();
}

public sealed class FeatureDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

public sealed class PackageDto
{
    public Guid Id { get; set; }
    public string PackageType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public int? DurationDays { get; set; }
    public int? ListingQuota { get; set; }
}

public sealed class ListingDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SaleMode { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string SellerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class CreateListingRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;
    public string SaleModeKey { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public decimal? AuctionStartPrice { get; set; }
    public decimal? AuctionMinBidIncrement { get; set; }
    public DateTimeOffset? AuctionStartsAt { get; set; }
    public DateTimeOffset? AuctionEndsAt { get; set; }
    public int? AuctionAutoExtendMinutes { get; set; }
}

public sealed class AuctionDto
{
    public Guid Id { get; set; }
    public Guid ListingId { get; set; }
    public decimal StartPrice { get; set; }
    public decimal MinBidIncrement { get; set; }
    public decimal? CurrentBidAmount { get; set; }
    public string CurrentWinnerUserName { get; set; } = string.Empty;
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public int AutoExtendMinutes { get; set; }
    public string AuctionStatus { get; set; } = string.Empty;
    public bool ResultProcessed { get; set; }
}

public sealed class AuctionBidDto
{
    public Guid Id { get; set; }
    public Guid AuctionId { get; set; }
    public string BidderUserName { get; set; } = string.Empty;
    public decimal BidAmount { get; set; }
    public string BidStatus { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class CreateAuctionBidRequest
{
    public string BidderUserName { get; set; } = string.Empty;
    public decimal BidAmount { get; set; }
}

public sealed class ListingOfferDto
{
    public Guid Id { get; set; }
    public Guid ListingId { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public decimal OfferedPrice { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string OfferNote { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class CreateListingOfferRequest
{
    public string BuyerName { get; set; } = string.Empty;
    public decimal OfferedPrice { get; set; }
    public string OfferNote { get; set; } = string.Empty;
}

public sealed class UpdateListingOfferStatusRequest
{
    public string ActorUserName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public sealed class ConversationSummaryDto
{
    public Guid Id { get; set; }
    public Guid? ListingId { get; set; }
    public string ConversationType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string CounterpartyUserName { get; set; } = string.Empty;
    public string LastMessagePreview { get; set; } = string.Empty;
    public DateTimeOffset? LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
}

public sealed class MessageDto
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public string SenderUserName { get; set; } = string.Empty;
    public string MessageText { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsMine { get; set; }
}

public sealed class ConversationDetailDto
{
    public Guid Id { get; set; }
    public Guid? ListingId { get; set; }
    public string ConversationType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string CounterpartyUserName { get; set; } = string.Empty;
    public IReadOnlyList<MessageDto> Messages { get; set; } = Array.Empty<MessageDto>();
}

public sealed class StartListingConversationRequest
{
    public string UserName { get; set; } = string.Empty;
}

public sealed class SendMessageRequest
{
    public string SenderUserName { get; set; } = string.Empty;
    public string MessageText { get; set; } = string.Empty;
}

public sealed class NotificationDto
{
    public Guid Id { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class CreatePaymentRequest
{
    public string UserName { get; set; } = string.Empty;
    public Guid PackageId { get; set; }
}

public sealed class PaymentResultDto
{
    public Guid PaymentId { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class CreateComplaintRequest
{
    public string UserName { get; set; } = string.Empty;
    public string TargetEntityType { get; set; } = string.Empty;
    public Guid TargetEntityId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class ComplaintResultDto
{
    public Guid ComplaintId { get; set; }
    public string ComplaintStatus { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class AdminOverviewDto
{
    public int ActiveUsers { get; set; }
    public int PublishedListings { get; set; }
    public int OpenConversations { get; set; }
    public int UnreadNotifications { get; set; }
    public IReadOnlyList<QuickStatDto> Highlights { get; set; } = Array.Empty<QuickStatDto>();
}

public sealed class AdminUserDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ListingCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class AdminListingDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string SellerUserName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SaleMode { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsFeatured { get; set; }
    public int ViewCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class AdminCategoryDto
{
    public Guid Id { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public string? ParentCategoryName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class AdminPackageDto
{
    public Guid Id { get; set; }
    public string PackageType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public int? DurationDays { get; set; }
    public int? ListingQuota { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class AdminPaymentDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string PaymentType { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public string ListingTitle { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class AdminPaymentsDashboardDto
{
    public decimal TotalPaidAmount { get; set; }
    public int PaidCount { get; set; }
    public int PendingCount { get; set; }
    public IReadOnlyList<AdminPaymentDto> Payments { get; set; } = Array.Empty<AdminPaymentDto>();
}

public sealed class AdminComplaintDto
{
    public Guid Id { get; set; }
    public string ReporterUserName { get; set; } = string.Empty;
    public string TargetEntityType { get; set; } = string.Empty;
    public Guid TargetEntityId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ComplaintStatus { get; set; } = string.Empty;
    public string AssignedAdminUserName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class AdminCategoryUpsertRequest
{
    public Guid? ParentCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public sealed class AdminPackageUpsertRequest
{
    public string PackageType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int? DurationDays { get; set; }
    public int? ListingQuota { get; set; }
}

public sealed class AdminUserStatusUpdateRequest
{
    public string Status { get; set; } = string.Empty;
}

public sealed class AdminListingStatusUpdateRequest
{
    public string Status { get; set; } = string.Empty;
}

public sealed class AdminCategoryStatusUpdateRequest
{
    public bool IsActive { get; set; }
}

public sealed class AdminPackageStatusUpdateRequest
{
    public bool IsActive { get; set; }
}

public sealed class AdminComplaintStatusUpdateRequest
{
    public string Status { get; set; } = string.Empty;
    public string? AssignedAdminUserName { get; set; }
}

public sealed class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class RegisterRequestDto
{
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string AccountType { get; set; } = "individual";
}

public sealed class AuthResponseDto
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
}
