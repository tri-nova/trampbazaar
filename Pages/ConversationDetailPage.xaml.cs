using trampbazaar.Services;
using trampbazaar.ViewModels;

namespace trampbazaar.Pages;

[QueryProperty(nameof(ConversationId), "conversationId")]
public partial class ConversationDetailPage : ContentPage
{
    private readonly ConversationDetailPageViewModel viewModel;
    private Guid conversationId;

    public ConversationDetailPage() : this(ServiceHelper.GetService<ConversationDetailPageViewModel>())
    {
    }

    public ConversationDetailPage(ConversationDetailPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }

    public string? ConversationId
    {
        get => conversationId.ToString();
        set
        {
            if (Guid.TryParse(value, out var parsed))
            {
                conversationId = parsed;
            }
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (conversationId != Guid.Empty)
        {
            await viewModel.LoadAsync(conversationId);
        }
    }

    private void OnComposerTextChanged(object? sender, TextChangedEventArgs e)
    {
        viewModel.NotifyComposerChanged();
    }

    private async void OnSendClicked(object? sender, EventArgs e)
    {
        var success = await viewModel.SendAsync();
        if (success)
        {
            await DisplayAlert("Basarili", viewModel.StatusMessage ?? "Mesaj gonderildi.", "Tamam");
        }
    }
}
