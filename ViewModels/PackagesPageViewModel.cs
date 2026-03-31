using System.Collections.ObjectModel;
using trampbazaar.Services;
using trampbazaar.Shared.Contracts;

namespace trampbazaar.ViewModels;

public sealed class PackagesPageViewModel(IMarketplaceDataService marketplaceDataService, SessionStateService sessionStateService) : BaseViewModel
{
    public ObservableCollection<PackageDto> Packages { get; } = [];

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            Replace(Packages, await marketplaceDataService.GetPackagesAsync(cancellationToken));
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

    public async Task<PaymentResultDto> PurchaseAsync(Guid packageId, CancellationToken cancellationToken = default)
    {
        if (!sessionStateService.IsAuthenticated)
        {
            throw new InvalidOperationException("Paket satin almak icin once giris yapin.");
        }

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var result = await marketplaceDataService.PurchasePackageAsync(packageId, cancellationToken);
            StatusMessage = result.Message;
            return result;
        }
        finally
        {
            IsBusy = false;
        }
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
