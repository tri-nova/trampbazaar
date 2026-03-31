using trampbazaar.Models;
using trampbazaar.Services;

namespace trampbazaar.ViewModels;

public sealed class RegisterPageViewModel(IMarketplaceDataService marketplaceDataService) : BaseViewModel
{
    private string fullName = string.Empty;
    private string userName = string.Empty;
    private string email = string.Empty;
    private string password = string.Empty;
    private string accountType = "individual";

    public string FullName
    {
        get => fullName;
        set => SetProperty(ref fullName, value);
    }

    public string UserName
    {
        get => userName;
        set => SetProperty(ref userName, value);
    }

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

    public string AccountType
    {
        get => accountType;
        set => SetProperty(ref accountType, value);
    }

    public async Task<bool> RegisterAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(FullName) ||
            string.IsNullOrWhiteSpace(UserName) ||
            string.IsNullOrWhiteSpace(Email) ||
            string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Tum alanlari doldurun.";
            return false;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var result = await marketplaceDataService.RegisterAsync(new RegisterRequest
            {
                FullName = FullName,
                UserName = UserName,
                Email = Email,
                Password = Password,
                AccountType = AccountType
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
