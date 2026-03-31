using Microsoft.AspNetCore.Mvc.RazorPages;
using trampbazaar.AdminWeb.Services;
using trampbazaar.Shared.Contracts;

namespace trampbazaar.AdminWeb.Pages;

public sealed class ConversationsModel(AdminApiClient apiClient) : PageModel
{
    public IReadOnlyList<ConversationSummaryDto> Conversations { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Conversations = await apiClient.GetConversationsAsync(cancellationToken);
    }
}
