using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiarioLivre.Core.Services;
using DiarioLivre.Desktop.Localization;
using DiarioLivre.Desktop.Services;

namespace DiarioLivre.Desktop.ViewModels;

/// <summary>
/// Root ViewModel managing sidebar navigation, active section state, theme toggling, language switching, PIN lock, and toasts.
/// </summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly DashboardViewModel _dashboardViewModel;
    private readonly GradebookViewModel _gradebookViewModel;
    private readonly AttendanceViewModel _attendanceViewModel;
    private readonly StudentDossierViewModel _studentDossierViewModel;
    private readonly PlannerViewModel _plannerViewModel;
    private readonly CalendarViewModel _calendarViewModel;
    private readonly SettingsViewModel _settingsViewModel;
    private readonly NavigationService _navigationService;
    private readonly UserPreferencesStore _preferencesStore;

    [ObservableProperty]
    private ViewModelBase _currentViewModel;

    [ObservableProperty]
    private AppSection _currentSection = AppSection.Dashboard;

    [ObservableProperty]
    private bool _isLocked;

    [ObservableProperty]
    private bool _hasPinConfigured;

    [ObservableProperty]
    private string _pinInput = string.Empty;

    [ObservableProperty]
    private string? _pinErrorText;

    public MainWindowViewModel(
        DashboardViewModel dashboardViewModel,
        GradebookViewModel gradebookViewModel,
        AttendanceViewModel attendanceViewModel,
        StudentDossierViewModel studentDossierViewModel,
        PlannerViewModel plannerViewModel,
        CalendarViewModel calendarViewModel,
        SettingsViewModel settingsViewModel,
        AppStateService appStateService,
        ThemeService themeService,
        NavigationService navigationService,
        ToastService toastService,
        UserPreferencesStore preferencesStore)
    {
        _dashboardViewModel = dashboardViewModel;
        _gradebookViewModel = gradebookViewModel;
        _attendanceViewModel = attendanceViewModel;
        _studentDossierViewModel = studentDossierViewModel;
        _plannerViewModel = plannerViewModel;
        _calendarViewModel = calendarViewModel;
        _settingsViewModel = settingsViewModel;
        AppState = appStateService;
        Theme = themeService;
        _navigationService = navigationService;
        Toast = toastService;
        _preferencesStore = preferencesStore;

        _currentViewModel = _dashboardViewModel;
        _navigationService.NavigationRequested += OnNavigationRequested;

        RefreshPinState(lockIfConfigured: true);
        _preferencesStore.PinConfigurationChanged += (_, _) => RefreshPinState(lockIfConfigured: false);
        _preferencesStore.LockRequested += (_, _) => LockNow();

        Theme.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ThemeService.IsDarkTheme))
            {
                OnPropertyChanged(nameof(ThemeButtonText));
            }
        };

        Theme.ThemeChanged += (_, _) =>
        {
            CurrentViewModel.RefreshThemeVisuals();
        };

        LocalizationService.Instance.CultureChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(ThemeButtonText));
        };
    }

    public AppStateService AppState { get; }

    public ThemeService Theme { get; }

    public ToastService Toast { get; }

    public bool IsDashboardActive => CurrentSection == AppSection.Dashboard;
    public bool IsGradebookActive => CurrentSection == AppSection.Gradebook;
    public bool IsAttendanceActive => CurrentSection == AppSection.Attendance;
    public bool IsDossierActive => CurrentSection == AppSection.StudentDossier;
    public bool IsPlannerActive => CurrentSection == AppSection.Planner;
    public bool IsCalendarActive => CurrentSection == AppSection.Calendar;
    public bool IsSettingsActive => CurrentSection == AppSection.Settings;

    public string ThemeButtonText => Theme.IsDarkTheme
        ? LocalizationService.Instance.GetString("LightModeButton")
        : LocalizationService.Instance.GetString("DarkModeButton");

    public override async Task LoadAsync()
    {
        await NavigateToAsync(AppSection.Dashboard);
    }

    [RelayCommand]
    private Task NavigateDashboardAsync() => NavigateToAsync(AppSection.Dashboard);

    [RelayCommand]
    private Task NavigateGradebookAsync() => NavigateToAsync(AppSection.Gradebook);

    [RelayCommand]
    private Task NavigateAttendanceAsync() => NavigateToAsync(AppSection.Attendance);

    [RelayCommand]
    private Task NavigateDossierAsync() => NavigateToAsync(AppSection.StudentDossier);

    [RelayCommand]
    private Task NavigatePlannerAsync() => NavigateToAsync(AppSection.Planner);

    [RelayCommand]
    private Task NavigateCalendarAsync() => NavigateToAsync(AppSection.Calendar);

    [RelayCommand]
    private Task NavigateSettingsAsync() => NavigateToAsync(AppSection.Settings);

    [RelayCommand]
    private Task SaveCurrentViewAsync() => CurrentViewModel.TrySaveAsync();

    [RelayCommand]
    private void ToggleTheme() => Theme.ToggleTheme();

    [RelayCommand]
    private void SetPortuguese() => LocalizationService.Instance.SetCulture("pt-BR");

    [RelayCommand]
    private void SetEnglish() => LocalizationService.Instance.SetCulture("en-US");

    [RelayCommand]
    private void LockNow()
    {
        if (!HasPinConfigured)
        {
            return;
        }

        PinInput = string.Empty;
        PinErrorText = null;
        IsLocked = true;
    }

    [RelayCommand]
    private void Unlock()
    {
        string? storedHash = _preferencesStore.Load().PinHash;
        if (string.IsNullOrWhiteSpace(storedHash) || PinHasher.VerifyPin(PinInput.Trim(), storedHash))
        {
            PinInput = string.Empty;
            PinErrorText = null;
            IsLocked = false;
            return;
        }

        PinInput = string.Empty;
        PinErrorText = LocalizationService.Instance.GetString("InvalidPinError");
    }

    private void RefreshPinState(bool lockIfConfigured)
    {
        bool configured = !string.IsNullOrWhiteSpace(_preferencesStore.Load().PinHash);
        HasPinConfigured = configured;
        if (!configured)
        {
            IsLocked = false;
            PinInput = string.Empty;
            PinErrorText = null;
        }
        else if (lockIfConfigured)
        {
            IsLocked = true;
        }
    }

    private void OnNavigationRequested(AppSection section)
    {
        _ = NavigateToAsync(section);
    }

    private async Task NavigateToAsync(AppSection section)
    {
        CurrentSection = section;
        OnPropertyChanged(nameof(IsDashboardActive));
        OnPropertyChanged(nameof(IsGradebookActive));
        OnPropertyChanged(nameof(IsAttendanceActive));
        OnPropertyChanged(nameof(IsDossierActive));
        OnPropertyChanged(nameof(IsPlannerActive));
        OnPropertyChanged(nameof(IsCalendarActive));
        OnPropertyChanged(nameof(IsSettingsActive));

        CurrentViewModel = section switch
        {
            AppSection.Dashboard => _dashboardViewModel,
            AppSection.Gradebook => _gradebookViewModel,
            AppSection.Attendance => _attendanceViewModel,
            AppSection.StudentDossier => _studentDossierViewModel,
            AppSection.Planner => _plannerViewModel,
            AppSection.Calendar => _calendarViewModel,
            AppSection.Settings => _settingsViewModel,
            _ => _dashboardViewModel
        };

        await CurrentViewModel.LoadAsync();
    }
}
