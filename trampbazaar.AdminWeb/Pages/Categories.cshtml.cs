using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using trampbazaar.AdminWeb.Services;
using trampbazaar.Shared.Contracts;

namespace trampbazaar.AdminWeb.Pages;

public sealed class CategoriesModel(AdminApiClient apiClient) : PageModel
{
    public IReadOnlyList<AdminCategoryDto> Categories { get; private set; } = [];
    public IReadOnlyList<SelectListItem> ParentOptions { get; private set; } = [];

    [BindProperty]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    public string Slug { get; set; } = string.Empty;

    [BindProperty]
    public int SortOrder { get; set; }

    [BindProperty]
    public Guid? ParentCategoryId { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken cancellationToken)
    {
        try
        {
            await apiClient.CreateCategoryAsync(new AdminCategoryUpsertRequest
            {
                Name = Name,
                Slug = Slug,
                SortOrder = SortOrder,
                ParentCategoryId = ParentCategoryId
            }, cancellationToken);

            StatusMessage = "Kategori olusturuldu.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Islem basarisiz: {ex.Message}";
            await LoadAsync(cancellationToken);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostUpdateAsync(Guid categoryId, string name, string slug, int sortOrder, Guid? parentCategoryId, CancellationToken cancellationToken)
    {
        try
        {
            await apiClient.UpdateCategoryAsync(categoryId, new AdminCategoryUpsertRequest
            {
                Name = name,
                Slug = slug,
                SortOrder = sortOrder,
                ParentCategoryId = parentCategoryId
            }, cancellationToken);

            StatusMessage = "Kategori guncellendi.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Islem basarisiz: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(Guid categoryId, bool nextIsActive, CancellationToken cancellationToken)
    {
        try
        {
            await apiClient.UpdateCategoryStatusAsync(categoryId, nextIsActive, cancellationToken);
            StatusMessage = "Kategori durumu guncellendi.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Islem basarisiz: {ex.Message}";
        }

        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Categories = await apiClient.GetCategoriesAsync(cancellationToken);
        ParentOptions = Categories
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .Prepend(new SelectListItem("Ana kategori", string.Empty))
            .ToList();
    }
}
