using System.ComponentModel.DataAnnotations;
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

    [BindProperty]
    public string ConfirmPassword { get; set; } = string.Empty;

    [BindProperty]
    public string AccountType { get; set; } = "individual";

    public string? ErrorMessage { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(FullName) ||
            string.IsNullOrWhiteSpace(UserName) ||
            string.IsNullOrWhiteSpace(Email) ||
            string.IsNullOrWhiteSpace(Password) ||
            string.IsNullOrWhiteSpace(ConfirmPassword))
        {
            ErrorMessage = "Tum alanlar zorunludur.";
            return Page();
        }

        if (!new EmailAddressAttribute().IsValid(Email))
        {
            ErrorMessage = "Gecerli bir e-posta girin.";
            return Page();
        }

        if (Password.Length < 8)
        {
            ErrorMessage = "Sifre en az 8 karakter olmalidir.";
            return Page();
        }

        if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
        {
            ErrorMessage = "Sifre tekrar alani eslesmiyor.";
            return Page();
        }

        AccountType = string.Equals(AccountType, "corporate", StringComparison.OrdinalIgnoreCase)
            ? "corporate"
            : "individual";

        var result = await apiClient.RegisterAsync(new RegisterRequestDto
        {
            FullName = FullName.Trim(),
            UserName = UserName.Trim(),
            Email = Email.Trim(),
            Password = Password,
            AccountType = AccountType
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
