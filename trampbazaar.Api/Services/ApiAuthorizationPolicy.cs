using Microsoft.AspNetCore.Http;

namespace trampbazaar.Api.Services;

public static class ApiAuthorizationPolicy
{
    public static bool RequiresAuthentication(PathString path, string method)
    {
        if (path.StartsWithSegments("/api/admin/auth/login", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/api/auth/login", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/api/auth/register", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/api/dashboard", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/api/categories", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/api/sale-modes", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/api/packages", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (HttpMethods.IsGet(method) && path.StartsWithSegments("/api/listings", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (path.StartsWithSegments("/api/admin", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return path.StartsWithSegments("/api/account", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWithSegments("/api/conversations", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWithSegments("/api/notifications", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWithSegments("/api/payments", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWithSegments("/api/complaints", StringComparison.OrdinalIgnoreCase) ||
               (path.StartsWithSegments("/api/listings", StringComparison.OrdinalIgnoreCase) && !HttpMethods.IsGet(method));
    }

    public static bool IsAdminRoute(PathString path)
        => path.StartsWithSegments("/api/admin", StringComparison.OrdinalIgnoreCase) &&
           !path.StartsWithSegments("/api/admin/auth/login", StringComparison.OrdinalIgnoreCase);

    public static bool TryGetBearerToken(string? authorizationHeader, out string token)
    {
        token = string.Empty;
        if (string.IsNullOrWhiteSpace(authorizationHeader) ||
            !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        token = authorizationHeader["Bearer ".Length..].Trim();
        return !string.IsNullOrWhiteSpace(token);
    }
}
