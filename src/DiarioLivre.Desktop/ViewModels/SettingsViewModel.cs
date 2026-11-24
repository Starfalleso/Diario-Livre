using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiarioLivre.Core.Entities;
using DiarioLivre.Core.Enums;
using DiarioLivre.Core.Interfaces;
using DiarioLivre.Core.Models;
using DiarioLivre.Core.Services;
using DiarioLivre.Desktop.Localization;
using DiarioLivre.Desktop.Services;

namespace DiarioLivre.Desktop.ViewModels;

public sealed class AutoBackupItemViewModel
{
    public AutoBackupItemViewModel(AutoBackupEntry entry, string sizeDisplay)
    {
        FileName = entry.FileName;
        CreatedAtDisplay = entry.CreatedAtLocal.ToString("dd/MM/yyyy HH:mm");
        SizeDisplay = sizeDisplay;
    }

    public string FileName { get; }
    public string CreatedAtDisplay { get; }
    public string SizeDisplay { get; }
}

/// <summary>
/// ViewModel for editing active academic year defaults, theme/language preferences, offline .zip backup/restore, auto-backups, and LGPD PIN.
/// </summary>
public sealed partial class SettingsViewModel : ViewModelBase
{
    private readonly IAcademicYearRepository _academicYearRepository;
    private readonly IBackupService _backupService;
    private readonly AppStateService _appStateService;
    private readonly ToastService _toastService;
    private readonly UserPreferencesStore _preferencesStore;
    private bool _isLoading;

    [ObservableProperty]
    private AcademicYear? _selectedAcademicYear;

    [ObservableProperty]
    private decimal? _editYearValue = DateTime.Now.Year;

    [ObservableProperty]
    private string _editSchoolName = string.Empty;

    [ObservableProperty]
    private LocalizedOption<GradingSystem>? _selectedGradingSystemOption;

    [ObservableProperty]
    private decimal? _editPassingGrade = 6.0m;

    [ObservableProperty]
    private decimal? _editMinAttendance = 75.0m;

    [ObservableProperty]
    private decimal? _editTargetSchoolDays = 200m;

    [ObservableProperty]
    private LocalizedOption<GradeCalculationMode>? _selectedGradeModeOption;

    [ObservableProperty]
    private LocalizedOption<RecoveryPolicy>? _selectedRecoveryPolicyOption;

    [ObservableProperty]
    private string _databasePath = string.Empty;

    [ObservableProperty]
    private string _databaseSizeText = "0 KB";

    [ObservableProperty]
    private int _avatarFilesCount;

    [ObservableProperty]
    private int _classroomsCount;

    [ObservableProperty]
    private int _studentsCount;

    [ObservableProperty]
    private int _gradesCount;

    [ObservableProperty]
    private int _sessionsCount;

    [ObservableProperty]
    private int _observationsCount;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _autoBackupOnStartup = true;

    [ObservableProperty]
    private string _newPinText = string.Empty;

    [ObservableProperty]
    private string _confirmPinText = string.Empty;

    [ObservableProperty]
    private bool _hasPinConfigured;

    public SettingsViewModel(
        IAcademicYearRepository academicYearRepository,
        IBackupService backupService,
        AppStateService appStateService,
        ThemeService themeService,
        ToastService toastService,
        UserPreferencesStore preferencesStore)
    {
        _academicYearRepository = academicYearRepository;
        _backupService = backupService;
        _appStateService = appStateService;
        Theme = themeService;
        _toastService = toastService;
        _preferencesStore = preferencesStore;

        GradingSystemOptions = new ObservableCollection<LocalizedOption<GradingSystem>>
        {
            new(GradingSystem.Bimonthly, loc => loc.GetGradingSystemName(GradingSystem.Bimonthly)),
            new(GradingSystem.Trimester, loc => loc.GetGradingSystemName(GradingSystem.Trimester)),
            new(GradingSystem.Semester, loc => loc.GetGradingSystemName(GradingSystem.Semester))
        };

        GradeModeOptions = new ObservableCollection<LocalizedOption<GradeCalculationMode>>
        {
            new(GradeCalculationMode.WeightedAverage, loc => loc.GetGradeCalculationModeName(GradeCalculationMode.WeightedAverage)),
            new(GradeCalculationMode.SimpleSum, loc => loc.GetGradeCalculationModeName(GradeCalculationMode.SimpleSum))
        };

        RecoveryPolicyOptions = new ObservableCollection<LocalizedOption<RecoveryPolicy>>
        {
            new(RecoveryPolicy.ReplaceLowest, loc => loc.GetRecoveryPolicyName(RecoveryPolicy.ReplaceLowest)),
            new(RecoveryPolicy.AverageWithRecovery, loc => loc.GetRecoveryPolicyName(RecoveryPolicy.AverageWithRecovery))
        };

        _selectedGradingSystemOption = GradingSystemOptions[0];
        _selectedGradeModeOption = GradeModeOptions[0];
        _selectedRecoveryPolicyOption = RecoveryPolicyOptions[0];

        Theme.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ThemeService.IsDarkTheme))
            {
                OnPropertyChanged(nameof(IsLightTheme));
                OnPropertyChanged(nameof(IsDarkTheme));
            }
        };
    }

    public ThemeService Theme { get; }

    public bool IsLightTheme => !Theme.IsDarkTheme;

    public bool IsDarkTheme => Theme.IsDarkTheme;

    public ObservableCollection<AcademicYear> AcademicYears { get; } = new();

    public ObservableCollection<AutoBackupItemViewModel> AutoBackups { get; } = new();

    public bool HasNoAutoBackups => AutoBackups.Count == 0;

    public ObservableCollection<LocalizedOption<GradingSystem>> GradingSystemOptions { get; }

    public ObservableCollection<LocalizedOption<GradeCalculationMode>> GradeModeOptions { get; }

    public ObservableCollection<LocalizedOption<RecoveryPolicy>> RecoveryPolicyOptions { get; }

    public string GetSuggestedBackupFileName()
        => $"DiarioLivre_Backup_{DateTime.Now:yyyyMMdd_HHmm}.zip";

    public override async Task LoadAsync()
    {
        _isLoading = true;
        try
        {
            IReadOnlyList<AcademicYear> years = await _academicYearRepository.GetAllAsync();
            Guid? preferredId = _appStateService.SelectedAcademicYear?.Id ?? SelectedAcademicYear?.Id;

            AcademicYears.Clear();
            foreach (AcademicYear year in years)
            {
                AcademicYears.Add(year);
            }

            AcademicYear? active = years.FirstOrDefault(y => y.Id == preferredId) ?? years.FirstOrDefault();
            SelectedAcademicYear = active;
            _appStateService.SelectedAcademicYear = active;
            PopulateForm(active);

            UserPreferencesData prefs = _preferencesStore.Load();
            AutoBackupOnStartup = prefs.AutoBackupOnStartup;
            HasPinConfigured = !string.IsNullOrWhiteSpace(prefs.PinHash);

            await RefreshStorageInfoAsync();
        }
        finally
        {
            _isLoading = false;
        }
    }

    public override Task TrySaveAsync() => SaveSettingsAsync();

    public async Task ExportBackupZipAsync(string destinationPath)
    {
        IsBusy = true;
        try
        {
            await _backupService.ExportBackupZipAsync(destinationPath);
            _toastService.ShowSuccess(Loc.GetString("BackupExportedMessage"));
        }
        catch (Exception ex)
        {
            _toastService.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RestoreBackupZipAsync(string sourcePath)
    {
        IsBusy = true;
        try
        {
            await _backupService.RestoreBackupZipAsync(sourcePath);
            _appStateService.SelectedClassroom = null;
            _appStateService.SelectedStudent = null;
            _appStateService.SelectedAcademicYear = null;

            await LoadAsync();
            _toastService.ShowSuccess(Loc.GetString("BackupRestoredMessage"));
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"{Loc.GetString("BackupRestoreErrorMessage")} {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedAcademicYearChanged(AcademicYear? value)
    {
        if (_isLoading || value is null)
        {
            return;
        }

        _appStateService.SelectedAcademicYear = value;
        PopulateForm(value);
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        if (SelectedAcademicYear is null || string.IsNullOrWhiteSpace(EditSchoolName))
        {
            return;
        }

        SelectedAcademicYear.Year = (int)Math.Clamp(EditYearValue ?? DateTime.Now.Year, 2000, 2100);
        SelectedAcademicYear.SchoolName = EditSchoolName.Trim();
        SelectedAcademicYear.GradingSystem = SelectedGradingSystemOption?.Value ?? GradingSystem.Bimonthly;
        SelectedAcademicYear.PassingGrade = Math.Max(0.5m, EditPassingGrade ?? 6.0m);
        SelectedAcademicYear.MinAttendancePercentage = Math.Clamp(EditMinAttendance ?? 75.0m, 1m, 100m);
        SelectedAcademicYear.TargetSchoolDays = Math.Clamp((int)(EditTargetSchoolDays ?? 200m), 1, 365);
        SelectedAcademicYear.GradeCalculationMode = SelectedGradeModeOption?.Value ?? GradeCalculationMode.WeightedAverage;
        SelectedAcademicYear.RecoveryPolicy = SelectedRecoveryPolicyOption?.Value ?? RecoveryPolicy.ReplaceLowest;

        await _academicYearRepository.UpdateAsync(SelectedAcademicYear);
        _appStateService.SelectedAcademicYear = SelectedAcademicYear;

        await LoadAsync();
        _toastService.ShowSuccess(Loc.GetString("SavedSuccessMessage"));
    }

    [RelayCommand]
    private void SetLightTheme() => Theme.IsDarkTheme = false;

    [RelayCommand]
    private void SetDarkTheme() => Theme.IsDarkTheme = true;

    [RelayCommand]
    private void SetPortuguese() => LocalizationService.Instance.SetCulture("pt-BR");

    [RelayCommand]
    private void SetEnglish() => LocalizationService.Instance.SetCulture("en-US");

    partial void OnAutoBackupOnStartupChanged(bool value)
    {
        if (_isLoading)
        {
            return;
        }

        _preferencesStore.Update(prefs => prefs with { AutoBackupOnStartup = value });
    }

    [RelayCommand]
    private async Task CreateAutoSnapshotNowAsync()
    {
        IsBusy = true;
        try
        {
            await _backupService.CreateRollingAutoBackupAsync(5);
            await RefreshStorageInfoAsync();
            _toastService.ShowSuccess(Loc.GetString("SnapshotCreatedMessage"));
        }
        catch (Exception ex)
        {
            _toastService.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SavePin()
    {
        string pin = NewPinText.Trim();
        string confirm = ConfirmPinText.Trim();
        if (!string.Equals(pin, confirm, StringComparison.Ordinal) || !PinHasher.IsValidPinFormat(pin))
        {
            _toastService.ShowWarning(Loc.GetString("PinValidationError"));
            return;
        }

        string hash = PinHasher.HashPin(pin);
        _preferencesStore.Update(prefs => prefs with { PinHash = hash });
        HasPinConfigured = true;
        NewPinText = string.Empty;
        ConfirmPinText = string.Empty;
        _toastService.ShowSuccess(Loc.GetString("PinSavedMessage"));
    }

    [RelayCommand]
    private void RemovePin()
    {
        _preferencesStore.Update(prefs => prefs with { PinHash = null });
        HasPinConfigured = false;
        NewPinText = string.Empty;
        ConfirmPinText = string.Empty;
        _toastService.ShowSuccess(Loc.GetString("PinRemovedMessage"));
    }

    [RelayCommand]
    private void LockNow()
    {
        if (HasPinConfigured)
        {
            _preferencesStore.RequestLock();
        }
    }

    [RelayCommand]
    private void OpenDataFolder()
    {
        try
        {
            Directory.CreateDirectory(_backupService.DataDirectory);
            Process.Start(new ProcessStartInfo
            {
                FileName = _backupService.DataDirectory,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _toastService.ShowError(ex.Message);
        }
    }

    private void PopulateForm(AcademicYear? year)
    {
        if (year is null)
        {
            EditYearValue = DateTime.Now.Year;
            EditSchoolName = string.Empty;
            EditPassingGrade = 6.0m;
            EditMinAttendance = 75.0m;
            EditTargetSchoolDays = 200m;
            return;
        }

        EditYearValue = year.Year;
        EditSchoolName = year.SchoolName;
        EditPassingGrade = year.PassingGrade;
        EditMinAttendance = year.MinAttendancePercentage;
        EditTargetSchoolDays = year.TargetSchoolDays;
        SelectedGradingSystemOption = GradingSystemOptions.FirstOrDefault(o => o.Value == year.GradingSystem) ?? GradingSystemOptions[0];
        SelectedGradeModeOption = GradeModeOptions.FirstOrDefault(o => o.Value == year.GradeCalculationMode) ?? GradeModeOptions[0];
        SelectedRecoveryPolicyOption = RecoveryPolicyOptions.FirstOrDefault(o => o.Value == year.RecoveryPolicy) ?? RecoveryPolicyOptions[0];
    }

    private async Task RefreshStorageInfoAsync()
    {
        BackupInfo info = await _backupService.GetStorageInfoAsync();
        DatabasePath = info.DatabasePath;
        DatabaseSizeText = FormatBytes(info.DatabaseSizeBytes);
        AvatarFilesCount = info.AvatarFilesCount;
        ClassroomsCount = info.ClassroomsCount;
        StudentsCount = info.StudentsCount;
        GradesCount = info.GradesCount;
        SessionsCount = info.AttendanceSessionsCount;
        ObservationsCount = info.ObservationsCount;

        IReadOnlyList<AutoBackupEntry> snapshots = await _backupService.GetAutoBackupsAsync();
        AutoBackups.Clear();
        foreach (AutoBackupEntry entry in snapshots)
        {
            AutoBackups.Add(new AutoBackupItemViewModel(entry, FormatBytes(entry.SizeBytes)));
        }

        OnPropertyChanged(nameof(HasNoAutoBackups));
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} B";
        }

        double kb = bytes / 1024.0;
        if (kb < 1024.0)
        {
            return $"{kb.ToString("F1", CultureInfo.CurrentCulture)} KB";
        }

        double mb = kb / 1024.0;
        return $"{mb.ToString("F2", CultureInfo.CurrentCulture)} MB";
    }
}
