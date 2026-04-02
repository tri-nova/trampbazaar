namespace trampbazaar.Shared.Api;

public static class ApiRoutes
{
    public const string Dashboard = "api/dashboard";
    public const string Categories = "api/categories";
    public const string SaleModes = "api/sale-modes";
    public const string Listings = "api/listings";
    public const string Packages = "api/packages";
    public const string Account = "api/account";
    public const string Payments = "api/payments";
    public const string Conversations = "api/conversations";
    public const string Complaints = "api/complaints";
    public const string Notifications = "api/notifications";
    public const string AdminOverview = "api/admin/overview";
    public const string AdminUsers = "api/admin/users";
    public const string AdminListings = "api/admin/listings";
    public const string AdminCategories = "api/admin/categories";
    public const string AdminPackages = "api/admin/packages";
    public const string AdminPayments = "api/admin/payments";
    public const string AdminComplaints = "api/admin/complaints";
    public const string AdminConversations = "api/admin/conversations";
    public const string AdminNotifications = "api/admin/notifications";
    public const string AdminAuthLogin = "api/admin/auth/login";
    public const string AuthLogin = "api/auth/login";
    public const string AuthRegister = "api/auth/register";

    public static string ListingById(Guid listingId) => $"{Listings}/{listingId}";
    public static string ListingOffers(Guid listingId) => $"{ListingById(listingId)}/offers";
    public static string ListingOfferStatus(Guid listingId, Guid offerId) => $"{ListingOffers(listingId)}/{offerId}/status";
    public static string ListingAuction(Guid listingId) => $"{ListingById(listingId)}/auction";
    public static string ListingAuctionBids(Guid listingId) => $"{ListingAuction(listingId)}/bids";
    public static string ListingConversations(Guid listingId) => $"{ListingById(listingId)}/conversations";
    public static string ConversationById(Guid conversationId) => $"{Conversations}/{conversationId}";
    public static string ConversationMessages(Guid conversationId) => $"{ConversationById(conversationId)}/messages";
    public static string NotificationById(Guid notificationId) => $"{Notifications}/{notificationId}";
    public static string PackageById(Guid packageId) => $"{Packages}/{packageId}";
    public static string AdminUserStatus(Guid userId) => $"{AdminUsers}/{userId}/status";
    public static string AdminListingStatus(Guid listingId) => $"{AdminListings}/{listingId}/status";
    public static string AdminCategoryById(Guid categoryId) => $"{AdminCategories}/{categoryId}";
    public static string AdminCategoryStatus(Guid categoryId) => $"{AdminCategories}/{categoryId}/status";
    public static string AdminPackageById(Guid packageId) => $"{AdminPackages}/{packageId}";
    public static string AdminPackageStatus(Guid packageId) => $"{AdminPackages}/{packageId}/status";
    public static string AdminComplaintStatus(Guid complaintId) => $"{AdminComplaints}/{complaintId}/status";
}
