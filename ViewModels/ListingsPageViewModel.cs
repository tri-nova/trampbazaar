using System.Collections.ObjectModel;
using trampbazaar.Models;
using trampbazaar.Services;
using trampbazaar.Shared.Contracts;

namespace trampbazaar.ViewModels;

public sealed class ListingsPageViewModel(IMarketplaceDataService marketplaceDataService) : BaseViewModel
{
    private CategoryDto? selectedCategory;
    private SaleModeDto? selectedSaleMode;

    public ObservableCollection<CategoryDto> Categories { get; } = [];

    public ObservableCollection<SaleModeDto> SaleModes { get; } = [];

    public ObservableCollection<ListingDto> Listings { get; } = [];

    public CategoryDto? SelectedCategory
    {
        get => selectedCategory;
        set => SetProperty(ref selectedCategory, value);
    }

    public SaleModeDto? SelectedSaleMode
    {
        get => selectedSaleMode;
        set => SetProperty(ref selectedSaleMode, value);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (Categories.Count == 0)
            {
                Replace(Categories, await marketplaceDataService.GetCategoriesAsync(cancellationToken));
            }

            if (SaleModes.Count == 0)
            {
                Replace(SaleModes, await marketplaceDataService.GetSaleModesAsync(cancellationToken));
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    public async Task LoadListingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var filter = new ListingFilter
            {
                CategorySlug = SelectedCategory?.Slug,
                SaleModeKey = SelectedSaleMode?.Key
            };

            Replace(Listings, await marketplaceDataService.GetListingsAsync(filter, cancellationToken));
            StatusMessage = $"{Listings.Count} ilan bulundu.";
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

    private static void Replace<T>(ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }
}
