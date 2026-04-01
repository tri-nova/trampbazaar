using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using trampbazaar.Shared.Contracts;
using trampbazaar.Web.Services;

namespace trampbazaar.Web.Pages;

public sealed class ConversationDetailModel(MarketplaceWebApiClient apiClient) : PageModel
{
    [BindProperty]
    public string MessageText { get; set; } = string.Empty;

    public ConversationDetailDto? Conversation { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        return await LoadAsync(conversationId, cancellationToken) ? Page() : NotFound();
    }

    public async Task<IActionResult> OnPostAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(conversationId, cancellationToken))
        {
            return NotFound();
        }

        var userName = HttpContext.Session.GetString("UserName");
        if (string.IsNullOrWhiteSpace(userName))
        {
            return RedirectToPage("/Login");
        }

        if (string.IsNullOrWhiteSpace(MessageText))
        {
            ErrorMessage = "Mesaj metni bos olamaz.";
            return Page();
        }

        var result = await apiClient.SendMessageAsync(conversationId, new SendMessageRequest
        {
            SenderUserName = userName,
            MessageText = MessageText
        }, cancellationToken);

        if (!result.IsSuccess)
        {
            ErrorMessage = result.ErrorMessage ?? "Mesaj gonderilemedi.";
            return Page();
        }

        MessageText = string.Empty;
        await LoadAsync(conversationId, cancellationToken);
        return Page();
    }

    private async Task<bool> LoadAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        var userName = HttpContext.Session.GetString("UserName");
        if (string.IsNullOrWhiteSpace(userName))
        {
            return false;
        }

        Conversation = await apiClient.GetConversationAsync(conversationId, userName, cancellationToken);
        return Conversation is not null;
    }
}
