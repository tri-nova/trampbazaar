using trampbazaar.Models;
using trampbazaar.Services;

namespace trampbazaar.ViewModels;

public sealed class LoginPageViewModel(IMarketplaceDataService marketplaceDataService) : BaseViewModel
{
    private string email = string.Empty;
    private string password = string.Empty;

    public string Email
    {
        get => email;
        set => SetProperty(ref email, value);
    }

    public string Password
    {
        get => password;
        set => SetProperty(ref password, value);
    }

    public async Task<bool> LoginAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "E-posta ve sifre zorunludur.";
            return false;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var result = await marketplaceDataService.LoginAsync(new LoginRequest
            {
                Email = Email,
                Password = Password
            }, cancellationToken);

            StatusMessage = result.Message;
            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
