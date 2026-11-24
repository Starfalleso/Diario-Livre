using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiarioLivre.Desktop.Localization;

namespace DiarioLivre.Desktop.ViewModels;

/// <summary>
/// Clickable period/semester pill shared by Gradebook, Attendance, and Student Dossier views.
/// </summary>
public sealed partial class TermPillViewModel : ObservableObject, IDisposable
{
    private readonly Func<LocalizationService, string> _labelSelector;
    private readonly Action<int?> _selectAction;

    [ObservableProperty]
    private bool _isSelected;

    public TermPillViewModel(
        int? termNumber,
        Func<LocalizationService, string> labelSelector,
        bool isSelected,
        Action<int?> selectAction)
    {
        TermNumber = termNumber;
        _labelSelector = labelSelector;
        _isSelected = isSelected;
        _selectAction = selectAction;
        LocalizationService.Instance.CultureChanged += OnCultureChanged;
    }

    public int? TermNumber { get; }

    public string Label => _labelSelector(LocalizationService.Instance);

    [RelayCommand]
    private void Select() => _selectAction(TermNumber);

    public void Dispose()
    {
        LocalizationService.Instance.CultureChanged -= OnCultureChanged;
    }

    private void OnCultureChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(Label));
    }
}
