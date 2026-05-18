using System.Reflection;

namespace trampbazaar.Infrastructure;

public static class AppEnvironment
{
    public static readonly string DefaultApiBaseUrl =
        typeof(AppEnvironment).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == "TrampBazaarApiBaseUrl")
            ?.Value
        ?? "https://api.example.com/";
}
