using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DiarioLivre.Desktop.Services;

/// <summary>
/// Manages Light / Dark theme switching and persists user UI theme preferences via <see cref="UserPreferencesStore"/>.
/// </summary>
public sealed class ThemeService : ObservableObject
{
    private readonly UserPreferencesStore _preferencesStore;
    private bool _isDarkTheme;

    public ThemeService(UserPreferencesStore preferencesStore)
    {
        _preferencesStore = preferencesStore;
    }

    public event EventHandler? ThemeChanged;

    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (SetProperty(ref _isDarkTheme, value))
            {
                ApplyTheme(value);
                _preferencesStore.Update(p => p with { IsDarkTheme = value });
                ThemeChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public void Initialize()
    {
        bool savedDark = _preferencesStore.Load().IsDarkTheme;
        _isDarkTheme = savedDark;
        OnPropertyChanged(nameof(IsDarkTheme));
        ApplyTheme(savedDark);
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ToggleTheme() => IsDarkTheme = !IsDarkTheme;

    private static void ApplyTheme(bool isDark)
    {
        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeVariant = isDark
                ? ThemeVariant.Dark
                : ThemeVariant.Light;
        }
    }
}
