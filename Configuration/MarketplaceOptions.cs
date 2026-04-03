namespace trampbazaar.Configuration;

public sealed class MarketplaceOptions
{
    public bool UseMockData { get; init; } = true;

    public string ApiBaseUrl { get; init; } = "http://localhost:5136/";
}
