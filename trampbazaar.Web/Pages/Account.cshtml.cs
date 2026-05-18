using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using trampbazaar.Shared.Contracts;
using trampbazaar.Web.Services;

namespace trampbazaar.Web.Pages;

public sealed class AccountModel(MarketplaceWebApiClient apiClient) : PageModel
{
    [BindProperty]
    public UpdateUserAccountProfileRequest ProfileForm { get; set; } = new();

    [BindProperty]
    public UpsertUserBillingAddressRequest BillingForm { get; set; } = new();

    [BindProperty]
    public ChangePasswordRequest PasswordForm { get; set; } = new();

    public UserAccountDashboardDto Account { get; private set; } = new();

    public UserAccountProfileDto Profile { get; private set; } = new();

    [TempData]
    public string? FlashSuccess { get; set; }

    [TempData]
    public string? FlashError { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!IsAuthenticated())
        {
            return RedirectToPage("/Login");
        }

        await LoadPageStateAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostProfileAsync(CancellationToken cancellationToken)
    {
        if (!IsAuthenticated())
        {
            return RedirectToPage("/Login");
        }

        var result = await apiClient.UpdateAccountProfileAsync(ProfileForm, cancellationToken);
        if (!result.IsSuccess)
        {
            await LoadPageStateAsync(cancellationToken);
            FlashError = result.ErrorMessage ?? "Profil bilgileri guncellenemedi.";
            return Page();
        }

        FlashSuccess = "Uyelik ve iletisim bilgileri guncellendi.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostBillingAsync(CancellationToken cancellationToken)
    {
        if (!IsAuthenticated())
        {
            return RedirectToPage("/Login");
        }

        var result = await apiClient.UpdateBillingAddressAsync(BillingForm, cancellationToken);
        if (!result.IsSuccess)
        {
            await LoadPageStateAsync(cancellationToken);
            FlashError = result.ErrorMessage ?? "Fatura bilgileri guncellenemedi.";
            return Page();
        }

        FlashSuccess = "Fatura bilgileri guncellendi.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostPasswordAsync(CancellationToken cancellationToken)
    {
        if (!IsAuthenticated())
        {
            return RedirectToPage("/Login");
        }

        var result = await apiClient.ChangePasswordAsync(PasswordForm, cancellationToken);
        if (!result.IsSuccess)
        {
            await LoadPageStateAsync(cancellationToken);
            FlashError = result.ErrorMessage ?? "Sifre guncellenemedi.";
            return Page();
        }

        FlashSuccess = "Sifre guncellendi.";
        return RedirectToPage();
    }

    private bool IsAuthenticated()
        => !string.IsNullOrWhiteSpace(HttpContext.Session.GetString("UserName"));

    private async Task LoadPageStateAsync(CancellationToken cancellationToken)
    {
        Account = await apiClient.GetAccountDashboardAsync(cancellationToken) ?? new UserAccountDashboardDto();
        Profile = await apiClient.GetAccountProfileAsync(cancellationToken) ?? new UserAccountProfileDto();
        SeedFormsFromProfile();
    }

    private void SeedFormsFromProfile()
    {
        ProfileForm = new UpdateUserAccountProfileRequest
        {
            FirstName = Profile.FirstName,
            LastName = Profile.LastName,
            Email = Profile.Email,
            MobilePhone = Profile.MobilePhone,
            WorkPhone = Profile.WorkPhone,
            NationalId = Profile.NationalId,
            IsForeignCitizen = Profile.IsForeignCitizen,
            BirthDate = Profile.BirthDate,
            Gender = string.IsNullOrWhiteSpace(Profile.Gender) ? "unspecified" : Profile.Gender,
            AddressLine = Profile.AddressLine,
            PostalCode = Profile.PostalCode,
            City = Profile.City,
            District = Profile.District,
            EmailOptIn = Profile.EmailOptIn,
            SmsOptIn = Profile.SmsOptIn,
            PhoneOptIn = Profile.PhoneOptIn
        };

        BillingForm = new UpsertUserBillingAddressRequest
        {
            InvoiceType = string.IsNullOrWhiteSpace(Profile.BillingAddress.InvoiceType) ? "individual" : Profile.BillingAddress.InvoiceType,
            AddressTitle = Profile.BillingAddress.AddressTitle,
            FullName = Profile.BillingAddress.FullName,
            IdentityNumber = Profile.BillingAddress.IdentityNumber,
            TaxOffice = Profile.BillingAddress.TaxOffice,
            TaxNumber = Profile.BillingAddress.TaxNumber,
            Country = string.IsNullOrWhiteSpace(Profile.BillingAddress.Country) ? "Turkiye" : Profile.BillingAddress.Country,
            City = Profile.BillingAddress.City,
            District = Profile.BillingAddress.District,
            Neighborhood = Profile.BillingAddress.Neighborhood,
            PostalCode = Profile.BillingAddress.PostalCode,
            PhoneNumber = Profile.BillingAddress.PhoneNumber,
            AddressLine = Profile.BillingAddress.AddressLine
        };
    }
}
