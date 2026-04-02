using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using trampbazaar.AdminWeb.Services;

namespace trampbazaar.AdminWeb.Pages;

public sealed class LoginModel(AdminApiClient apiClient) : PageModel
{
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
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "E-posta ve sifre zorunludur.";
            return Page();
        }

        var result = await apiClient.LoginAsync(Email, Password, cancellationToken);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Message;
            return Page();
        }

        HttpContext.Session.SetString("AdminUserName", result.UserName);
        HttpContext.Session.SetString("AdminRoleName", result.RoleName);
        HttpContext.Session.SetString("AccessToken", result.AccessToken);
        return RedirectToPage("/Index");
    }
}
