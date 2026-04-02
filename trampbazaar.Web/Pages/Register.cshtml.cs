using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using trampbazaar.Shared.Contracts;
using trampbazaar.Web.Services;

namespace trampbazaar.Web.Pages;

public sealed class RegisterModel(MarketplaceWebApiClient apiClient) : PageModel
{
    [BindProperty]
    public string FullName { get; set; } = string.Empty;

    [BindProperty]
    public string UserName { get; set; } = string.Empty;

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public string? ErrorMessage { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var result = await apiClient.RegisterAsync(new RegisterRequestDto
        {
            FullName = FullName,
            UserName = UserName,
            Email = Email,
            Password = Password
        }, cancellationToken);

        if (!result.IsSuccess)
        {
            ErrorMessage = result.Message;
            return Page();
        }

        HttpContext.Session.SetString("UserName", result.UserName);
        HttpContext.Session.SetString("AccessToken", result.AccessToken);
        return RedirectToPage("/Index");
    }
}
