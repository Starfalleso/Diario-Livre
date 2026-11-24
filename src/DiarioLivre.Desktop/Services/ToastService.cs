using System.Collections.ObjectModel;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DiarioLivre.Desktop.Services;

public enum ToastKind
{
    Success,
    Warning,
    Error,
    Info
}

public sealed partial class ToastNotification : ObservableObject
{
    private readonly Action<Guid> _dismissAction;

    public ToastNotification(Guid id, string message, ToastKind kind, Action<Guid> dismissAction)
    {
        Id = id;
        Message = message;
        Kind = kind;
        _dismissAction = dismissAction;
    }

    public Guid Id { get; }
    public string Message { get; }
    public ToastKind Kind { get; }

    public IBrush AccentBrush => Kind switch
    {
        ToastKind.Success => new SolidColorBrush(Color.Parse("#16A34A")),
        ToastKind.Warning => new SolidColorBrush(Color.Parse("#D97706")),
        ToastKind.Error => new SolidColorBrush(Color.Parse("#DC2626")),
        _ => new SolidColorBrush(Color.Parse("#4F46E5"))
    };

    [RelayCommand]
    private void Dismiss() => _dismissAction(Id);
}

/// <summary>
/// Thread-safe application notification service displaying auto-dismissing floating toast cards.
/// </summary>
public sealed class ToastService : ObservableObject
{
    public ObservableCollection<ToastNotification> Toasts { get; } = new();

    public void ShowSuccess(string message) => Show(message, ToastKind.Success);

    public void ShowWarning(string message) => Show(message, ToastKind.Warning);

    public void ShowError(string message) => Show(message, ToastKind.Error);

    public void ShowInfo(string message) => Show(message, ToastKind.Info);

    public void Dismiss(Guid toastId)
    {
        ToastNotification? target = Toasts.FirstOrDefault(t => t.Id == toastId);
        if (target is not null)
        {
            Toasts.Remove(target);
        }
    }

    private void Show(string message, ToastKind kind)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var id = Guid.NewGuid();
        var toast = new ToastNotification(id, message, kind, Dismiss);

        Dispatcher.UIThread.Post(() =>
        {
            while (Toasts.Count >= 4)
            {
                Toasts.RemoveAt(0);
            }

            Toasts.Add(toast);
        });

        _ = Task.Run(async () =>
        {
            await Task.Delay(3800);
            Dispatcher.UIThread.Post(() => Dismiss(id));
        });
    }
}
