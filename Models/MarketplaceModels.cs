namespace trampbazaar.Models;

public sealed class MarketplaceSnapshot
{
    public required string UserName { get; init; }
    public required string HeroTitle { get; init; }
    public required string HeroSubtitle { get; init; }
    public required IReadOnlyList<QuickStat> QuickStats { get; init; }
    public required IReadOnlyList<SaleModeSummary> SaleModes { get; init; }
    public required IReadOnlyList<FlowStage> SharedFlow { get; init; }
    public required IReadOnlyList<FeatureCard> FeatureCards { get; init; }
    public required IReadOnlyList<ProductCard> FeaturedProducts { get; init; }
}

public sealed class QuickStat
{
    public required string Value { get; init; }
    public required string Label { get; init; }
}

public sealed class SaleModeSummary
{
    public required string Key { get; init; }
    public required string Name { get; init; }
    public required string AccentColor { get; init; }
    public required string ShortDescription { get; init; }
    public required IReadOnlyList<string> Steps { get; init; }
}

public sealed class FlowStage
{
    public required string Title { get; init; }
    public required string Caption { get; init; }
}

public sealed class FeatureCard
{
    public required string Title { get; init; }
    public required string Description { get; init; }
}

public sealed class ProductCard
{
    public required string Title { get; init; }
    public required string Category { get; init; }
    public required string PriceLabel { get; init; }
    public required string Status { get; init; }
}
