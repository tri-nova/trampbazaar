using System.Collections.ObjectModel;
using trampbazaar.Services;
using trampbazaar.Shared.Contracts;

namespace trampbazaar.ViewModels;

public sealed class ConversationsPageViewModel(IMarketplaceDataService marketplaceDataService, SessionStateService sessionStateService) : BaseViewModel
{
    public ObservableCollection<ConversationSummaryDto> Conversations { get; } = [];

    public bool IsAuthenticated => sessionStateService.IsAuthenticated;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!sessionStateService.IsAuthenticated)
        {
            Conversations.Clear();
            ErrorMessage = "Mesajlari gormek icin once giris yapin.";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            var items = await marketplaceDataService.GetConversationsAsync(cancellationToken);
            Replace(Conversations, items);
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
