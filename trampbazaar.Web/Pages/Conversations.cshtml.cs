using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using trampbazaar.Shared.Contracts;
using trampbazaar.Web.Services;

namespace trampbazaar.Web.Pages;

public sealed class ConversationsModel(MarketplaceWebApiClient apiClient) : PageModel
{
    public IReadOnlyList<ConversationSummaryDto> Conversations { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var userName = HttpContext.Session.GetString("UserName");
        if (string.IsNullOrWhiteSpace(userName))
        {
            return RedirectToPage("/Login");
        }

        Conversations = await apiClient.GetConversationsAsync(userName, cancellationToken);
        return Page();
    }
}
