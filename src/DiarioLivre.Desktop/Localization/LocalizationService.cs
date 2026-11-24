using System.Globalization;
using System.Resources;
using CommunityToolkit.Mvvm.ComponentModel;
using DiarioLivre.Core.Enums;
using DiarioLivre.Desktop.Services;

namespace DiarioLivre.Desktop.Localization;

/// <summary>
/// Provides live-updating bilingual string lookup backed by .resx resources (pt-BR default and en-US).
/// </summary>
public sealed class LocalizationService : ObservableObject
{
    private readonly ResourceManager _resourceManager;
    private CultureInfo _currentCulture;

    public static LocalizationService Instance { get; } = new();

    private LocalizationService()
    {
        _resourceManager = new ResourceManager(
            "DiarioLivre.Desktop.Resources.Strings",
            typeof(LocalizationService).Assembly);

        string savedCode = UserPreferencesStore.LoadStatic().CultureCode;
        string initialCode = string.Equals(savedCode, "en-US", StringComparison.OrdinalIgnoreCase)
            ? "en-US"
            : "pt-BR";

        _currentCulture = new CultureInfo(initialCode);
        ApplyCulture(_currentCulture);
    }

    public CultureInfo CurrentCulture
    {
        get => _currentCulture;
        private set
        {
            if (SetProperty(ref _currentCulture, value))
            {
                OnPropertyChanged(nameof(IsPortuguese));
                OnPropertyChanged(nameof(IsEnglish));
                OnPropertyChanged(nameof(CurrentCultureCode));
                OnPropertyChanged("Item");
                OnPropertyChanged("Item[]");
                CultureChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public bool IsPortuguese => CurrentCulture.Name.StartsWith("pt", StringComparison.OrdinalIgnoreCase);

    public bool IsEnglish => CurrentCulture.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase);

    public string CurrentCultureCode => CurrentCulture.Name;

    public event EventHandler? CultureChanged;

    public string this[string key] => GetString(key);

    public string GetString(string key)
        => _resourceManager.GetString(key, CurrentCulture) ?? key;

    public void SetCulture(string cultureName)
    {
        var nextCulture = new CultureInfo(cultureName);
        ApplyCulture(nextCulture);
        UserPreferencesStore.UpdateStatic(p => p with { CultureCode = nextCulture.Name });
        CurrentCulture = nextCulture;
    }

    public string GetDayOfWeekName(DayOfWeek dayOfWeek) => dayOfWeek switch
    {
        DayOfWeek.Monday => GetString("DayMonday"),
        DayOfWeek.Tuesday => GetString("DayTuesday"),
        DayOfWeek.Wednesday => GetString("DayWednesday"),
        DayOfWeek.Thursday => GetString("DayThursday"),
        DayOfWeek.Friday => GetString("DayFriday"),
        DayOfWeek.Saturday => GetString("DaySaturday"),
        _ => GetString("DaySunday")
    };

    public string GetLessonPlanStatusName(LessonPlanStatus status) => status switch
    {
        LessonPlanStatus.Taught => GetString("StatusTaught"),
        LessonPlanStatus.Postponed => GetString("StatusPostponed"),
        _ => GetString("StatusPlanned")
    };

    public string GetShiftName(Shift shift) => shift switch
    {
        Shift.Morning => GetString("ShiftMorning"),
        Shift.Afternoon => GetString("ShiftAfternoon"),
        Shift.Night => GetString("ShiftNight"),
        Shift.FullTime => GetString("ShiftFullTime"),
        _ => shift.ToString()
    };

    public string GetGradingSystemName(GradingSystem gradingSystem) => gradingSystem switch
    {
        GradingSystem.Bimonthly => GetString("GradingBimonthly"),
        GradingSystem.Trimester => GetString("GradingTrimester"),
        GradingSystem.Semester => GetString("GradingSemester"),
        _ => gradingSystem.ToString()
    };

    public string GetGradeCalculationModeName(GradeCalculationMode mode) => mode switch
    {
        GradeCalculationMode.SimpleSum => GetString("ModeSimpleSum"),
        GradeCalculationMode.WeightedAverage => GetString("ModeWeightedAverage"),
        _ => mode.ToString()
    };

    public string GetRecoveryPolicyName(RecoveryPolicy policy) => policy switch
    {
        RecoveryPolicy.ReplaceLowest => GetString("RecoveryReplaceLowest"),
        RecoveryPolicy.AverageWithRecovery => GetString("RecoveryAverageWith"),
        _ => policy.ToString()
    };

    public string GetObservationCategoryName(ObservationCategory category) => category switch
    {
        ObservationCategory.Pedagogical => GetString("CategoryPedagogical"),
        ObservationCategory.Behavioral => GetString("CategoryBehavioral"),
        ObservationCategory.Praise => GetString("CategoryPraise"),
        ObservationCategory.FamilyContact => GetString("CategoryFamilyContact"),
        _ => category.ToString()
    };

    public string GetAlertStatusName(AttendanceAlertStatus status) => status switch
    {
        AttendanceAlertStatus.Normal => GetString("AlertNormal"),
        AttendanceAlertStatus.Warning => GetString("AlertWarning"),
        AttendanceAlertStatus.Critical => GetString("AlertCritical"),
        _ => status.ToString()
    };

    public string GetCalendarEventTypeName(SchoolCalendarEventType type) => type switch
    {
        SchoolCalendarEventType.Holiday => GetString("EventTypeHoliday"),
        SchoolCalendarEventType.Recess => GetString("EventTypeRecess"),
        SchoolCalendarEventType.SchoolSaturday => GetString("EventTypeSchoolSaturday"),
        SchoolCalendarEventType.PedagogicalDay => GetString("EventTypePedagogicalDay"),
        SchoolCalendarEventType.ExtraSchoolDay => GetString("EventTypeExtraSchoolDay"),
        _ => type.ToString()
    };

    public string GetTermName(GradingSystem system, int termNumber)
    {
        string unit = system switch
        {
            GradingSystem.Semester => GetString("SemesterSingular"),
            GradingSystem.Trimester => GetString("TrimesterSingular"),
            _ => GetString("BimesterSingular")
        };

        if (IsPortuguese)
        {
            return $"{termNumber}º {unit}";
        }

        string ordinal = termNumber switch
        {
            1 => "1st",
            2 => "2nd",
            3 => "3rd",
            _ => $"{termNumber}th"
        };

        return $"{ordinal} {unit}";
    }

    public string GetTermShortName(GradingSystem system, int termNumber)
    {
        string shortUnit = system switch
        {
            GradingSystem.Semester => GetString("SemesterShort"),
            GradingSystem.Trimester => GetString("TrimesterShort"),
            _ => GetString("BimesterShort")
        };

        return IsPortuguese
            ? $"{termNumber}º {shortUnit}"
            : $"{shortUnit}{termNumber}";
    }

    public string GetPdfTermPrefix(GradingSystem system)
    {
        if (IsPortuguese)
        {
            return system switch
            {
                GradingSystem.Semester => "Sem ",
                GradingSystem.Trimester => "Tri ",
                _ => "Bim "
            };
        }

        return system switch
        {
            GradingSystem.Semester => "S",
            GradingSystem.Trimester => "T",
            _ => "B"
        };
    }

    private static void ApplyCulture(CultureInfo culture)
    {
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
    }
}
