using System.Collections.ObjectModel;
using trampbazaar.Models;
using trampbazaar.Services;

namespace trampbazaar.ViewModels;

public sealed class MainPageViewModel : BaseViewModel
{
    private readonly IMarketplaceDataService marketplaceDataService;
    private MarketplaceSnapshot snapshot;
    private SaleModeSummary? selectedSaleMode;

    public MainPageViewModel(IMarketplaceDataService marketplaceDataService)
    {
        this.marketplaceDataService = marketplaceDataService;
        snapshot = new MarketplaceSnapshot
        {
            UserName = string.Empty,
            HeroTitle = string.Empty,
            HeroSubtitle = string.Empty,
            QuickStats = Array.Empty<QuickStat>(),
            SaleModes = Array.Empty<SaleModeSummary>(),
            SharedFlow = Array.Empty<FlowStage>(),
            FeatureCards = Array.Empty<FeatureCard>(),
            FeaturedProducts = Array.Empty<ProductCard>()
        };

        QuickStats = [];
        SaleModes = [];
        SharedFlow = [];
        FeatureCards = [];
        FeaturedProducts = [];
    }

    public MarketplaceSnapshot Snapshot
    {
        get => snapshot;
        private set => SetProperty(ref snapshot, value);
    }

    public ObservableCollection<QuickStat> QuickStats { get; }

    public ObservableCollection<SaleModeSummary> SaleModes { get; }

    public ObservableCollection<FlowStage> SharedFlow { get; }

    public ObservableCollection<FeatureCard> FeatureCards { get; }

    public ObservableCollection<ProductCard> FeaturedProducts { get; }

    public SaleModeSummary? SelectedSaleMode
    {
        get => selectedSaleMode;
        private set
        {
            if (SetProperty(ref selectedSaleMode, value))
            {
                OnPropertyChanged(nameof(SelectedSaleModeSteps));
            }
        }
    }

    public IReadOnlyList<string> SelectedSaleModeSteps => SelectedSaleMode?.Steps ?? Array.Empty<string>();

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            Snapshot = await marketplaceDataService.GetSnapshotAsync(cancellationToken);

            Replace(QuickStats, Snapshot.QuickStats);
            Replace(SaleModes, Snapshot.SaleModes);
            Replace(SharedFlow, Snapshot.SharedFlow);
            Replace(FeatureCards, Snapshot.FeatureCards);
            Replace(FeaturedProducts, Snapshot.FeaturedProducts);

            SelectedSaleMode = SaleModes.FirstOrDefault();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void SelectSaleMode(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        SelectedSaleMode = SaleModes.FirstOrDefault(mode => mode.Key == key) ?? SelectedSaleMode;
    }

    private static void Replace<T>(ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }
}
