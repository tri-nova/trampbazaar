using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using trampbazaar.AdminWeb.Services;
using trampbazaar.Shared.Contracts;

namespace trampbazaar.AdminWeb.Pages;

public sealed class ComplaintsModel(AdminApiClient apiClient) : PageModel
{
    private static readonly IReadOnlyList<SelectListItem> ComplaintStatusOptionsValue =
    [
        new("Acik", "open"),
        new("Incelemede", "in_review"),
        new("Cozuldu", "resolved"),
        new("Reddedildi", "rejected")
    ];

    public IReadOnlyList<AdminComplaintDto> Complaints { get; private set; } = [];
    public IReadOnlyList<SelectListItem> ComplaintStatusOptions => ComplaintStatusOptionsValue;

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Complaints = await apiClient.GetComplaintsAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(Guid complaintId, string status, string? assignedAdminUserName, CancellationToken cancellationToken)
    {
        try
        {
            await apiClient.UpdateComplaintStatusAsync(complaintId, new AdminComplaintStatusUpdateRequest
            {
                Status = status,
                AssignedAdminUserName = assignedAdminUserName
            }, cancellationToken);

            StatusMessage = "Sikayet durumu guncellendi.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Islem basarisiz: {ex.Message}";
        }

        return RedirectToPage();
    }
}
