using CommunityToolkit.Mvvm.ComponentModel;
using DiarioLivre.Desktop.Localization;

namespace DiarioLivre.Desktop.ViewModels;

/// <summary>
/// Non-generic base class so Avalonia compiled XAML templates can bind cleanly to <see cref="Label"/>.
/// </summary>
public abstract class LocalizedChoice : ObservableObject
{
    public abstract string Label { get; }
}

/// <summary>
/// Wraps an enum or typed value with a dynamically localized display label.
/// </summary>
public sealed class LocalizedOption<T> : LocalizedChoice
{
    private readonly Func<LocalizationService, string> _labelSelector;

    public LocalizedOption(T value, Func<LocalizationService, string> labelSelector)
    {
        Value = value;
        _labelSelector = labelSelector;
        LocalizationService.Instance.CultureChanged += (_, _) => OnPropertyChanged(nameof(Label));
    }

    public T Value { get; }

    public override string Label => _labelSelector(LocalizationService.Instance);

    public override string ToString() => Label;
}
