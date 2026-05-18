using trampbazaar.Infrastructure;

namespace trampbazaar.Configuration;

public sealed class MarketplaceOptions
{
    public bool UseMockData { get; init; } = false;

    public string ApiBaseUrl { get; init; } = AppEnvironment.DefaultApiBaseUrl;
}
