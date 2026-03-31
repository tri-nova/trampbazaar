using Microsoft.AspNetCore.Mvc.RazorPages;
using trampbazaar.Shared.Contracts;
using trampbazaar.Web.Services;

namespace trampbazaar.Web.Pages;

public sealed class IndexModel(MarketplaceWebApiClient apiClient) : PageModel
{
    public DashboardResponse Dashboard { get; private set; } = new();
    public IReadOnlyList<ListingDto> FeaturedListings { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Dashboard = await apiClient.GetDashboardAsync(cancellationToken);
        FeaturedListings = (await apiClient.GetListingsAsync(cancellationToken: cancellationToken)).Take(6).ToList();
    }
}
