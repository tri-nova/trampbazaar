using trampbazaar.Services;
using trampbazaar.ViewModels;

namespace trampbazaar.Pages;

public partial class ConversationsPage : ContentPage
{
    private readonly ConversationsPageViewModel viewModel;

    public ConversationsPage() : this(ServiceHelper.GetService<ConversationsPageViewModel>())
    {
    }

    public ConversationsPage(ConversationsPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.LoadAsync();
    }

    private async void OnOpenConversationClicked(object? sender, EventArgs e)
    {
        if (sender is Button button &&
            Guid.TryParse(button.CommandParameter?.ToString(), out var conversationId))
        {
            await Shell.Current.GoToAsync($"{nameof(ConversationDetailPage)}?conversationId={conversationId}");
        }
    }
}
