using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using trampbazaar.Shared.Contracts;
using trampbazaar.Web.Services;

namespace trampbazaar.Web.Pages;

public sealed class ComplaintModel(MarketplaceWebApiClient apiClient) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string TargetEntityType { get; set; } = "listing";

    [BindProperty(SupportsGet = true)]
    public Guid TargetEntityId { get; set; }

    [BindProperty]
    public string Subject { get; set; } = string.Empty;

    [BindProperty]
    public string Description { get; set; } = string.Empty;

    public string? ErrorMessage { get; private set; }

    public IActionResult OnGet()
    {
        return string.IsNullOrWhiteSpace(HttpContext.Session.GetString("UserName"))
            ? RedirectToPage("/Login")
            : Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var userName = HttpContext.Session.GetString("UserName");
        if (string.IsNullOrWhiteSpace(userName))
        {
            return RedirectToPage("/Login");
        }

        if (TargetEntityId == Guid.Empty || string.IsNullOrWhiteSpace(Subject) || string.IsNullOrWhiteSpace(Description))
        {
            ErrorMessage = "Tum sikayet alanlari zorunludur.";
            return Page();
        }

        var result = await apiClient.CreateComplaintAsync(new CreateComplaintRequest
        {
            UserName = userName,
            TargetEntityType = TargetEntityType,
            TargetEntityId = TargetEntityId,
            Subject = Subject,
            Description = Description
        }, cancellationToken);

        if (!result.IsSuccess)
        {
            ErrorMessage = result.ErrorMessage ?? "Sikayet olusturulamadi.";
            return Page();
        }

        return RedirectToPage("/Notifications");
    }
}
