using Microsoft.Maui.Storage;

namespace trampbazaar.Services;

public sealed class SessionStateService
{
    private const string UserNameKey = "session.userName";
    private const string IsAuthenticatedKey = "session.isAuthenticated";
    private const string AccessTokenKey = "session.accessToken";

    public event EventHandler? SessionChanged;

    public string UserName { get; private set; }

    public bool IsAuthenticated { get; private set; }

    public string AccessToken { get; private set; }

    public SessionStateService()
    {
        UserName = Preferences.Default.Get(UserNameKey, "Misafir");
        IsAuthenticated = Preferences.Default.Get(IsAuthenticatedKey, false);
        AccessToken = Preferences.Default.Get(AccessTokenKey, string.Empty);

        if (!IsAuthenticated)
        {
            UserName = "Misafir";
            AccessToken = string.Empty;
        }
    }

    public void SignIn(string userName, string? accessToken = null)
    {
        UserName = string.IsNullOrWhiteSpace(userName) ? "Kullanici" : userName;
        IsAuthenticated = true;
        AccessToken = accessToken?.Trim() ?? string.Empty;
        Persist();
    }

    public void SignOut()
    {
        UserName = "Misafir";
        IsAuthenticated = false;
        AccessToken = string.Empty;
        Persist();
    }

    private void Persist()
    {
        Preferences.Default.Set(UserNameKey, UserName);
        Preferences.Default.Set(IsAuthenticatedKey, IsAuthenticated);
        Preferences.Default.Set(AccessTokenKey, AccessToken);
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }
}
