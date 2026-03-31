using Microsoft.Maui.Storage;

namespace trampbazaar.Services;

public sealed class SessionStateService
{
    private const string UserNameKey = "session.userName";
    private const string IsAuthenticatedKey = "session.isAuthenticated";

    public event EventHandler? SessionChanged;

    public string UserName { get; private set; }

    public bool IsAuthenticated { get; private set; }

    public SessionStateService()
    {
        UserName = Preferences.Default.Get(UserNameKey, "Misafir");
        IsAuthenticated = Preferences.Default.Get(IsAuthenticatedKey, false);

        if (!IsAuthenticated)
        {
            UserName = "Misafir";
        }
    }

    public void SignIn(string userName)
    {
        UserName = string.IsNullOrWhiteSpace(userName) ? "Kullanici" : userName;
        IsAuthenticated = true;
        Persist();
    }

    public void SignOut()
    {
        UserName = "Misafir";
        IsAuthenticated = false;
        Persist();
    }

    private void Persist()
    {
        Preferences.Default.Set(UserNameKey, UserName);
        Preferences.Default.Set(IsAuthenticatedKey, IsAuthenticated);
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }
}
