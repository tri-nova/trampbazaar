using System.Collections.ObjectModel;
using trampbazaar.Models;
using trampbazaar.Services;
using trampbazaar.Shared.Contracts;

namespace trampbazaar.ViewModels;

public sealed class ConversationDetailPageViewModel(IMarketplaceDataService marketplaceDataService, SessionStateService sessionStateService) : BaseViewModel
{
    private ConversationDetailDto? conversation;
    private Guid conversationId;

    public MessageFormModel MessageForm { get; } = new();
    public ObservableCollection<MessageDto> Messages { get; } = [];

    public ConversationDetailDto? Conversation
    {
        get => conversation;
        private set => SetProperty(ref conversation, value);
    }

    public bool CanSendMessage => sessionStateService.IsAuthenticated && !string.IsNullOrWhiteSpace(MessageForm.MessageText);

    public async Task LoadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!sessionStateService.IsAuthenticated)
        {
            ErrorMessage = "Mesajlari gormek icin once giris yapin.";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            conversationId = id;
            Conversation = await marketplaceDataService.GetConversationDetailAsync(id, cancellationToken);
            Replace(Messages, Conversation?.Messages ?? []);

            if (Conversation is null)
            {
                ErrorMessage = "Konusma bulunamadi.";
            }
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

    public async Task<bool> SendAsync(CancellationToken cancellationToken = default)
    {
        if (!sessionStateService.IsAuthenticated)
        {
            ErrorMessage = "Mesaj gondermek icin once giris yapin.";
            return false;
        }

        if (conversationId == Guid.Empty)
        {
            ErrorMessage = "Konusma bulunamadi.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(MessageForm.MessageText))
        {
            ErrorMessage = "Mesaj bos olamaz.";
            return false;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            await marketplaceDataService.SendMessageAsync(conversationId, MessageForm, cancellationToken);
            MessageForm.MessageText = string.Empty;
            OnPropertyChanged(nameof(MessageForm));
            OnPropertyChanged(nameof(CanSendMessage));
            await LoadAsync(conversationId, cancellationToken);
            StatusMessage = "Mesaj gonderildi.";
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void NotifyComposerChanged()
    {
        OnPropertyChanged(nameof(CanSendMessage));
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
