using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace trampbazaar.ViewModels;

public abstract class BaseViewModel : INotifyPropertyChanged
{
    private bool isBusy;
    private string? errorMessage;
    private string? statusMessage;

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsBusy
    {
        get => isBusy;
        protected set => SetProperty(ref isBusy, value);
    }

    public string? ErrorMessage
    {
        get => errorMessage;
        protected set => SetProperty(ref errorMessage, value);
    }

    public string? StatusMessage
    {
        get => statusMessage;
        protected set => SetProperty(ref statusMessage, value);
    }

    protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
        {
            return false;
        }

        backingStore = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
