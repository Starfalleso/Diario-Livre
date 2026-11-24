using CommunityToolkit.Mvvm.ComponentModel;
using DiarioLivre.Desktop.Localization;

namespace DiarioLivre.Desktop.ViewModels;

/// <summary>
/// Base class for all ViewModels providing shared access to live localization resources.
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
    public LocalizationService Loc => LocalizationService.Instance;

    public virtual Task LoadAsync() => Task.CompletedTask;

    public virtual Task TrySaveAsync() => Task.CompletedTask;

    public virtual void RefreshThemeVisuals() { }
}
