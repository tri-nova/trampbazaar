using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using trampbazaar.Shared.Contracts;
using trampbazaar.Web.Services;

namespace trampbazaar.Web.Pages;

public sealed class CreateListingModel(MarketplaceWebApiClient apiClient) : PageModel
{
    [BindProperty]
    public string Title { get; set; } = string.Empty;

    [BindProperty]
    public string Description { get; set; } = string.Empty;

    [BindProperty]
    public string CategorySlug { get; set; } = string.Empty;

    [BindProperty]
    public string SaleModeKey { get; set; } = "direct";

    [BindProperty]
    public decimal Price { get; set; }

    [BindProperty]
    public decimal? AuctionStartPrice { get; set; }

    [BindProperty]
    public decimal? AuctionMinBidIncrement { get; set; }

    [BindProperty]
    public DateTime? AuctionStartDate { get; set; }

    [BindProperty]
    public TimeSpan? AuctionStartTime { get; set; }

    [BindProperty]
    public DateTime? AuctionEndDate { get; set; }

    [BindProperty]
    public TimeSpan? AuctionEndTime { get; set; }

    [BindProperty]
    public int? AuctionAutoExtendMinutes { get; set; }

    public string? ErrorMessage { get; private set; }
    public IReadOnlyList<CategoryDto> Categories { get; private set; } = [];
    public IReadOnlyList<SaleModeDto> SaleModes { get; private set; } = [];
    public bool IsAuctionMode => string.Equals(SaleModeKey, "auction", StringComparison.OrdinalIgnoreCase);

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(HttpContext.Session.GetString("UserName")))
        {
            return RedirectToPage("/Login");
        }

        SetAuctionDefaults();
        await LoadLookupsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var userName = HttpContext.Session.GetString("UserName");
        if (string.IsNullOrWhiteSpace(userName))
        {
            return RedirectToPage("/Login");
        }

        if (string.IsNullOrWhiteSpace(Title) ||
            string.IsNullOrWhiteSpace(Description) ||
            string.IsNullOrWhiteSpace(CategorySlug) ||
            string.IsNullOrWhiteSpace(SaleModeKey))
        {
            ErrorMessage = "Tum zorunlu alanlari doldurun.";
            await LoadLookupsAsync(cancellationToken);
            return Page();
        }

        DateTimeOffset? auctionStartsAt = null;
        DateTimeOffset? auctionEndsAt = null;

        if (IsAuctionMode)
        {
            if (!AuctionStartPrice.HasValue || AuctionStartPrice <= 0 ||
                !AuctionMinBidIncrement.HasValue || AuctionMinBidIncrement <= 0 ||
                !AuctionStartDate.HasValue || !AuctionStartTime.HasValue ||
                !AuctionEndDate.HasValue || !AuctionEndTime.HasValue)
            {
                ErrorMessage = "Acik artirma icin tum alanlar zorunludur.";
                await LoadLookupsAsync(cancellationToken);
                return Page();
            }

            auctionStartsAt = new DateTimeOffset(AuctionStartDate.Value.Date + AuctionStartTime.Value, TimeSpan.Zero);
            auctionEndsAt = new DateTimeOffset(AuctionEndDate.Value.Date + AuctionEndTime.Value, TimeSpan.Zero);
            if (auctionEndsAt <= auctionStartsAt)
            {
                ErrorMessage = "Acik artirma bitisi baslangictan sonra olmali.";
                await LoadLookupsAsync(cancellationToken);
                return Page();
            }
        }

        var listing = await apiClient.CreateListingAsync(new CreateListingRequest
        {
            Title = Title,
            Description = Description,
            CategorySlug = CategorySlug,
            SaleModeKey = SaleModeKey,
            Price = Price,
            SellerName = userName,
            AuctionStartPrice = IsAuctionMode ? AuctionStartPrice : null,
            AuctionMinBidIncrement = IsAuctionMode ? AuctionMinBidIncrement : null,
            AuctionStartsAt = auctionStartsAt,
            AuctionEndsAt = auctionEndsAt,
            AuctionAutoExtendMinutes = IsAuctionMode ? AuctionAutoExtendMinutes : null
        }, cancellationToken);

        if (listing is null)
        {
            ErrorMessage = "Ilan olusturulamadi.";
            await LoadLookupsAsync(cancellationToken);
            return Page();
        }

        return RedirectToPage("/ListingDetail", new { listingId = listing.Id });
    }

    private async Task LoadLookupsAsync(CancellationToken cancellationToken)
    {
        Categories = await apiClient.GetCategoriesAsync(cancellationToken);
        SaleModes = await apiClient.GetSaleModesAsync(cancellationToken);
    }

    private void SetAuctionDefaults()
    {
        AuctionStartDate ??= DateTime.Today;
        AuctionStartTime ??= new TimeSpan(9, 0, 0);
        AuctionEndDate ??= DateTime.Today.AddDays(1);
        AuctionEndTime ??= new TimeSpan(18, 0, 0);
        AuctionMinBidIncrement ??= 1;
        AuctionAutoExtendMinutes ??= 0;
    }
}
