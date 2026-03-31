using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using trampbazaar.AdminWeb.Services;
using trampbazaar.Shared.Contracts;

namespace trampbazaar.AdminWeb.Pages;

public sealed class UsersModel(AdminApiClient apiClient) : PageModel
{
    public IReadOnlyList<AdminUserDto> Users { get; private set; } = [];
    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Users = await apiClient.GetUsersAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(Guid userId, string nextStatus, CancellationToken cancellationToken)
    {
        try
        {
            await apiClient.UpdateUserStatusAsync(userId, nextStatus, cancellationToken);
            StatusMessage = "Kullanici durumu guncellendi.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Islem basarisiz: {ex.Message}";
        }

        return RedirectToPage();
    }
}
