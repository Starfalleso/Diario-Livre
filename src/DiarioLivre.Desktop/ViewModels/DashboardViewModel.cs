using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiarioLivre.Core.Entities;
using DiarioLivre.Core.Enums;
using DiarioLivre.Core.Interfaces;
using DiarioLivre.Core.Models;
using DiarioLivre.Desktop.Localization;
using DiarioLivre.Desktop.Services;

namespace DiarioLivre.Desktop.ViewModels;

public sealed partial class TodayClassCardViewModel : ObservableObject, IDisposable
{
    private readonly ScheduleSlot _slot;
    private readonly Action<ScheduleSlot> _startAttendanceAction;

    public TodayClassCardViewModel(ScheduleSlot slot, Action<ScheduleSlot> startAttendanceAction)
    {
        _slot = slot;
        _startAttendanceAction = startAttendanceAction;
        TimeRangeText = $"{slot.StartTime:HH\\:mm} – {slot.EndTime:HH\\:mm}";
        ClassroomName = slot.Classroom?.Name ?? "—";
        LessonBadge = $"{slot.LessonCount}×";
        LocalizationService.Instance.CultureChanged += OnCultureChanged;
    }

    public LocalizationService Loc => LocalizationService.Instance;
    public string TimeRangeText { get; }
    public string ClassroomName { get; }
    public string LessonBadge { get; }

    public string SubjectAndRoomText
    {
        get
        {
            string subject = _slot.Classroom?.IsMultiSubject == true
                ? LocalizationService.Instance.GetString("PolyvalentBadge")
                : _slot.Subject?.Name ?? "—";
            return string.IsNullOrWhiteSpace(_slot.RoomLabel)
                ? subject
                : $"{subject} • {_slot.RoomLabel}";
        }
    }

    [RelayCommand]
    private void StartAttendance() => _startAttendanceAction(_slot);

    public void Dispose()
    {
        LocalizationService.Instance.CultureChanged -= OnCultureChanged;
    }

    private void OnCultureChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(SubjectAndRoomText));
    }
}

public sealed class StudentAvatarPreview
{
    public StudentAvatarPreview(string fullName, string? photoPath)
    {
        FullName = fullName;
        PhotoPath = photoPath;
    }

    public string FullName { get; }
    public string? PhotoPath { get; }
}

public sealed partial class AtRiskStudentItemViewModel : ObservableObject, IDisposable
{
    private readonly Action<Classroom, Student> _openDossierAction;

    public AtRiskStudentItemViewModel(
        Classroom classroom,
        Student student,
        string? photoPath,
        AttendanceSummary summary,
        Action<Classroom, Student> openDossierAction)
    {
        Classroom = classroom;
        Student = student;
        PhotoPath = photoPath;
        Summary = summary;
        _openDossierAction = openDossierAction;

        AbsencePercentage = summary.AbsencePercentage;
        AbsencePercentageText = $"{summary.AbsencePercentage:F1}%";
        AlertStatus = summary.AlertStatus;

        LocalizationService.Instance.CultureChanged += OnCultureChanged;
    }

    public Classroom Classroom { get; }
    public Student Student { get; }
    public LocalizationService Loc => LocalizationService.Instance;
    public string FullName => Student.FullName;
    public string ClassroomName => Classroom.Name;
    public string? PhotoPath { get; }
    public AttendanceSummary Summary { get; }
    public decimal AbsencePercentage { get; }
    public string AbsencePercentageText { get; }
    public AttendanceAlertStatus AlertStatus { get; }
    public string AlertLabel => LocalizationService.Instance.GetAlertStatusName(AlertStatus);

    [RelayCommand]
    private void OpenDossier() => _openDossierAction(Classroom, Student);

    public void RefreshTheme()
    {
        OnPropertyChanged(nameof(AlertStatus));
    }

    public void Dispose()
    {
        LocalizationService.Instance.CultureChanged -= OnCultureChanged;
    }

    private void OnCultureChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(AlertLabel));
    }
}

public sealed partial class ClassroomCardViewModel : ObservableObject, IDisposable
{
    private readonly Action<Classroom, AppSection> _openAction;
    private readonly Func<Classroom, Task> _deleteAction;

    [ObservableProperty]
    private bool _confirmingDelete;

    public ClassroomCardViewModel(
        Classroom classroom,
        int alertCount,
        double attendanceHealthPercentage,
        IReadOnlyList<StudentAvatarPreview> avatarPreviews,
        Action<Classroom, AppSection> openAction,
        Func<Classroom, Task> deleteAction)
    {
        Classroom = classroom;
        AlertCount = alertCount;
        AttendanceHealthPercentage = attendanceHealthPercentage;
        AttendanceHealthText = $"{attendanceHealthPercentage:F0}%";
        AvatarPreviews = avatarPreviews;
        _openAction = openAction;
        _deleteAction = deleteAction;

        LocalizationService.Instance.CultureChanged += OnCultureChanged;
    }

    public Classroom Classroom { get; }
    public LocalizationService Loc => LocalizationService.Instance;
    public string Name => Classroom.Name;
    public int StudentCount => Classroom.Students.Count;
    public int SubjectCount => Classroom.Subjects.Count;
    public int AlertCount { get; }
    public bool HasAlerts => AlertCount > 0;
    public double AttendanceHealthPercentage { get; }
    public string AttendanceHealthText { get; }
    public IReadOnlyList<StudentAvatarPreview> AvatarPreviews { get; }

    public string ShiftLabel => LocalizationService.Instance.GetShiftName(Classroom.Shift);
    public string ModeBadge => Classroom.IsMultiSubject
        ? LocalizationService.Instance.GetString("PolyvalentBadge")
        : LocalizationService.Instance.GetString("SpecialistBadge");

    public IBrush ShiftAccentBrush => Classroom.Shift switch
    {
        Shift.Morning => new SolidColorBrush(Color.Parse("#0284C7")),
        Shift.Afternoon => new SolidColorBrush(Color.Parse("#D97706")),
        Shift.Night => new SolidColorBrush(Color.Parse("#6366F1")),
        _ => new SolidColorBrush(Color.Parse("#0D9488"))
    };

    public string DeleteLabel => ConfirmingDelete
        ? LocalizationService.Instance.GetString("ConfirmDeleteButton")
        : LocalizationService.Instance.GetString("DeleteButton");

    [RelayCommand]
    private void OpenGradebook() => _openAction(Classroom, AppSection.Gradebook);

    [RelayCommand]
    private void OpenAttendance() => _openAction(Classroom, AppSection.Attendance);

    [RelayCommand]
    private void OpenDossier() => _openAction(Classroom, AppSection.StudentDossier);

    [RelayCommand]
    private async Task RequestDeleteAsync()
    {
        if (!ConfirmingDelete)
        {
            ConfirmingDelete = true;
            OnPropertyChanged(nameof(DeleteLabel));
            return;
        }

        await _deleteAction(Classroom);
    }

    [RelayCommand]
    private void CancelDelete()
    {
        ConfirmingDelete = false;
        OnPropertyChanged(nameof(DeleteLabel));
    }

    public void Dispose()
    {
        LocalizationService.Instance.CultureChanged -= OnCultureChanged;
    }

    private void OnCultureChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(ShiftLabel));
        OnPropertyChanged(nameof(ModeBadge));
        OnPropertyChanged(nameof(DeleteLabel));
    }
}

public sealed partial class DashboardViewModel : ViewModelBase
{
    private readonly IAcademicYearRepository _academicYearRepository;
    private readonly IClassroomRepository _classroomRepository;
    private readonly ISchoolCalendarRepository _calendarRepository;
    private readonly IScheduleRepository _scheduleRepository;
    private readonly IAttendanceCalculator _attendanceCalculator;
    private readonly ISchoolDaysCalculator _schoolDaysCalculator;
    private readonly IAvatarService _avatarService;
    private readonly AppStateService _appStateService;
    private readonly NavigationService _navigationService;
    private readonly ToastService _toastService;

    private bool _isLoading;

    [ObservableProperty]
    private AcademicYear? _selectedAcademicYear;

    [ObservableProperty]
    private bool _isAddingClassroom;

    [ObservableProperty]
    private string _newClassroomName = string.Empty;

    [ObservableProperty]
    private LocalizedOption<Shift>? _selectedShiftOption;

    [ObservableProperty]
    private bool _newIsMultiSubject;

    [ObservableProperty]
    private bool _isAddingAcademicYear;

    [ObservableProperty]
    private decimal? _newYearValue = DateTime.Now.Year;

    [ObservableProperty]
    private string _newSchoolName = string.Empty;

    [ObservableProperty]
    private LocalizedOption<GradingSystem>? _selectedGradingSystemOption;

    [ObservableProperty]
    private decimal? _newPassingGrade = 6.0m;

    [ObservableProperty]
    private decimal? _newMinAttendance = 75.0m;

    [ObservableProperty]
    private int _totalClassroomsCount;

    [ObservableProperty]
    private int _totalStudentsCount;

    [ObservableProperty]
    private int _totalAlertsCount;

    [ObservableProperty]
    private string _schoolDaysCountText = "0 / 200";

    [ObservableProperty]
    private double _schoolDaysProgressValue;

    public DashboardViewModel(
        IAcademicYearRepository academicYearRepository,
        IClassroomRepository classroomRepository,
        ISchoolCalendarRepository calendarRepository,
        IScheduleRepository scheduleRepository,
        IAttendanceCalculator attendanceCalculator,
        ISchoolDaysCalculator schoolDaysCalculator,
        IAvatarService avatarService,
        AppStateService appStateService,
        NavigationService navigationService,
        ToastService toastService)
    {
        _academicYearRepository = academicYearRepository;
        _classroomRepository = classroomRepository;
        _calendarRepository = calendarRepository;
        _scheduleRepository = scheduleRepository;
        _attendanceCalculator = attendanceCalculator;
        _schoolDaysCalculator = schoolDaysCalculator;
        _avatarService = avatarService;
        _appStateService = appStateService;
        _navigationService = navigationService;
        _toastService = toastService;

        ShiftOptions = new ObservableCollection<LocalizedOption<Shift>>
        {
            new(Shift.Morning, loc => loc.GetShiftName(Shift.Morning)),
            new(Shift.Afternoon, loc => loc.GetShiftName(Shift.Afternoon)),
            new(Shift.Night, loc => loc.GetShiftName(Shift.Night)),
            new(Shift.FullTime, loc => loc.GetShiftName(Shift.FullTime))
        };
        _selectedShiftOption = ShiftOptions[0];

        GradingSystemOptions = new ObservableCollection<LocalizedOption<GradingSystem>>
        {
            new(GradingSystem.Semester, loc => loc.GetGradingSystemName(GradingSystem.Semester)),
            new(GradingSystem.Bimonthly, loc => loc.GetGradingSystemName(GradingSystem.Bimonthly)),
            new(GradingSystem.Trimester, loc => loc.GetGradingSystemName(GradingSystem.Trimester))
        };
        _selectedGradingSystemOption = GradingSystemOptions[0];

        LocalizationService.Instance.CultureChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(GradingSystemDisplay));
        };
    }

    public ObservableCollection<AcademicYear> AcademicYears { get; } = new();
    public ObservableCollection<ClassroomCardViewModel> ClassroomCards { get; } = new();
    public ObservableCollection<AtRiskStudentItemViewModel> AtRiskStudents { get; } = new();
    public ObservableCollection<TodayClassCardViewModel> TodaysClasses { get; } = new();
    public ObservableCollection<LocalizedOption<Shift>> ShiftOptions { get; }
    public ObservableCollection<LocalizedOption<GradingSystem>> GradingSystemOptions { get; }

    public bool HasAtRiskStudents => AtRiskStudents.Count > 0;
    public bool HasNoAtRiskStudents => AtRiskStudents.Count == 0;
    public bool HasTodaysClasses => TodaysClasses.Count > 0;
    public bool HasNoTodaysClasses => TodaysClasses.Count == 0;

    public string SchoolNameDisplay => SelectedAcademicYear?.SchoolName ?? "—";
    public string GradingSystemDisplay => SelectedAcademicYear is null
        ? "—"
        : LocalizationService.Instance.GetGradingSystemName(SelectedAcademicYear.GradingSystem);
    public string PassingGradeDisplay => SelectedAcademicYear is null
        ? "—"
        : SelectedAcademicYear.PassingGrade.ToString("F1");
    public string MinAttendanceDisplay => SelectedAcademicYear is null
        ? "—"
        : $"{SelectedAcademicYear.MinAttendancePercentage:F0}%";

    public bool IsSemesterSystem => SelectedAcademicYear?.GradingSystem == GradingSystem.Semester;
    public bool IsBimonthlySystem => SelectedAcademicYear?.GradingSystem == GradingSystem.Bimonthly;
    public bool IsTrimesterSystem => SelectedAcademicYear?.GradingSystem == GradingSystem.Trimester;

    public override async Task LoadAsync()
    {
        _isLoading = true;
        try
        {
            IReadOnlyList<AcademicYear> years = await _academicYearRepository.GetAllAsync();
            AcademicYears.Clear();
            foreach (AcademicYear year in years)
            {
                AcademicYears.Add(year);
            }

            AcademicYear? preferred = _appStateService.SelectedAcademicYear is not null
                ? AcademicYears.FirstOrDefault(y => y.Id == _appStateService.SelectedAcademicYear.Id)
                : null;

            SelectedAcademicYear = preferred ?? AcademicYears.FirstOrDefault();
            _appStateService.SelectedAcademicYear = SelectedAcademicYear;
        }
        finally
        {
            _isLoading = false;
        }

        await LoadClassroomsAsync();
    }

    public override void RefreshThemeVisuals()
    {
        foreach (AtRiskStudentItemViewModel risk in AtRiskStudents)
        {
            risk.RefreshTheme();
        }
    }

    partial void OnSelectedAcademicYearChanged(AcademicYear? value)
    {
        _appStateService.SelectedAcademicYear = value;
        if (value is not null)
        {
            int maxTerms = value.GradingSystem.GetTermCount();
            if (_appStateService.SelectedTermNumber > maxTerms)
            {
                _appStateService.SelectedTermNumber = 1;
            }
        }

        OnPropertyChanged(nameof(SchoolNameDisplay));
        OnPropertyChanged(nameof(GradingSystemDisplay));
        OnPropertyChanged(nameof(PassingGradeDisplay));
        OnPropertyChanged(nameof(MinAttendanceDisplay));
        OnPropertyChanged(nameof(IsSemesterSystem));
        OnPropertyChanged(nameof(IsBimonthlySystem));
        OnPropertyChanged(nameof(IsTrimesterSystem));

        if (!_isLoading)
        {
            _ = LoadClassroomsAsync();
        }
    }

    [RelayCommand]
    private Task SetSemesterSystemAsync() => SwitchGradingSystemAsync(GradingSystem.Semester);

    [RelayCommand]
    private Task SetBimonthlySystemAsync() => SwitchGradingSystemAsync(GradingSystem.Bimonthly);

    [RelayCommand]
    private Task SetTrimesterSystemAsync() => SwitchGradingSystemAsync(GradingSystem.Trimester);

    private async Task SwitchGradingSystemAsync(GradingSystem system)
    {
        if (SelectedAcademicYear is null || SelectedAcademicYear.GradingSystem == system)
        {
            return;
        }

        SelectedAcademicYear.GradingSystem = system;
        await _academicYearRepository.UpdateAsync(SelectedAcademicYear);

        int maxTerms = system.GetTermCount();
        if (_appStateService.SelectedTermNumber > maxTerms)
        {
            _appStateService.SelectedTermNumber = 1;
        }

        OnPropertyChanged(nameof(GradingSystemDisplay));
        OnPropertyChanged(nameof(IsSemesterSystem));
        OnPropertyChanged(nameof(IsBimonthlySystem));
        OnPropertyChanged(nameof(IsTrimesterSystem));
        _toastService.ShowSuccess(LocalizationService.Instance.GetString("SavedSuccessMessage"));
    }

    [RelayCommand]
    private void ToggleAddClassroom()
    {
        IsAddingClassroom = !IsAddingClassroom;
        if (IsAddingClassroom)
        {
            IsAddingAcademicYear = false;
        }
        else
        {
            NewClassroomName = string.Empty;
        }
    }

    [RelayCommand]
    private void ToggleAddAcademicYear()
    {
        IsAddingAcademicYear = !IsAddingAcademicYear;
        if (IsAddingAcademicYear)
        {
            IsAddingClassroom = false;
        }
        else
        {
            NewSchoolName = string.Empty;
        }
    }

    [RelayCommand]
    private async Task CreateAcademicYearAsync()
    {
        if (string.IsNullOrWhiteSpace(NewSchoolName))
        {
            return;
        }

        var academicYear = new AcademicYear
        {
            Id = Guid.NewGuid(),
            Year = (int)(NewYearValue ?? DateTime.Now.Year),
            SchoolName = NewSchoolName.Trim(),
            GradingSystem = SelectedGradingSystemOption?.Value ?? GradingSystem.Bimonthly,
            PassingGrade = NewPassingGrade is > 0 ? NewPassingGrade.Value : 6.0m,
            MinAttendancePercentage = NewMinAttendance is > 0 ? NewMinAttendance.Value : 75.0m,
            GradeCalculationMode = GradeCalculationMode.WeightedAverage,
            RecoveryPolicy = RecoveryPolicy.ReplaceLowest
        };

        await _academicYearRepository.AddAsync(academicYear);
        _appStateService.SelectedAcademicYear = academicYear;
        _appStateService.SelectedClassroom = null;

        NewSchoolName = string.Empty;
        IsAddingAcademicYear = false;
        _toastService.ShowSuccess(LocalizationService.Instance.GetString("SavedSuccessMessage"));
        await LoadAsync();
    }

    [RelayCommand]
    private async Task CreateClassroomAsync()
    {
        if (SelectedAcademicYear is null || string.IsNullOrWhiteSpace(NewClassroomName))
        {
            return;
        }

        var classroomId = Guid.NewGuid();
        var classroom = new Classroom
        {
            Id = classroomId,
            AcademicYearId = SelectedAcademicYear.Id,
            Name = NewClassroomName.Trim(),
            Shift = SelectedShiftOption?.Value ?? Shift.Morning,
            IsMultiSubject = NewIsMultiSubject,
            Subjects = new List<Subject>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ClassroomId = classroomId,
                    Name = NewIsMultiSubject ? "Polivalente" : "Língua Portuguesa"
                }
            }
        };

        await _classroomRepository.AddAsync(classroom);
        NewClassroomName = string.Empty;
        IsAddingClassroom = false;
        _toastService.ShowSuccess(LocalizationService.Instance.GetString("SavedSuccessMessage"));
        await LoadClassroomsAsync();
    }

    private async Task DeleteClassroomAsync(Classroom classroom)
    {
        await _classroomRepository.DeleteAsync(classroom.Id);
        if (_appStateService.SelectedClassroom?.Id == classroom.Id)
        {
            _appStateService.SelectedClassroom = null;
        }

        _toastService.ShowInfo(LocalizationService.Instance.GetString("SavedSuccessMessage"));
        await LoadClassroomsAsync();
    }

    private async Task LoadClassroomsAsync()
    {
        foreach (ClassroomCardViewModel existing in ClassroomCards)
        {
            existing.Dispose();
        }
        ClassroomCards.Clear();

        foreach (AtRiskStudentItemViewModel existingRisk in AtRiskStudents)
        {
            existingRisk.Dispose();
        }
        AtRiskStudents.Clear();

        foreach (TodayClassCardViewModel existingToday in TodaysClasses)
        {
            existingToday.Dispose();
        }
        TodaysClasses.Clear();

        TotalClassroomsCount = 0;
        TotalStudentsCount = 0;
        TotalAlertsCount = 0;
        SchoolDaysCountText = SelectedAcademicYear is null ? "0 / 200" : $"0 / {SelectedAcademicYear.TargetSchoolDays}";
        SchoolDaysProgressValue = 0;

        if (SelectedAcademicYear is null)
        {
            OnPropertyChanged(nameof(HasAtRiskStudents));
            OnPropertyChanged(nameof(HasNoAtRiskStudents));
            OnPropertyChanged(nameof(HasTodaysClasses));
            OnPropertyChanged(nameof(HasNoTodaysClasses));
            return;
        }

        IReadOnlyList<Classroom> classrooms = await _classroomRepository.GetByAcademicYearAsync(SelectedAcademicYear.Id);
        IReadOnlyList<SchoolCalendarEvent> calendarEvents = await _calendarRepository.GetByAcademicYearAsync(SelectedAcademicYear.Id);
        IReadOnlyList<ScheduleSlot> scheduleSlots = await _scheduleRepository.GetByAcademicYearAsync(SelectedAcademicYear.Id);

        DayOfWeek todayDow = DateTime.Today.DayOfWeek;
        foreach (ScheduleSlot slot in scheduleSlots.Where(s => s.DayOfWeek == todayDow).OrderBy(s => s.StartTime))
        {
            TodaysClasses.Add(new TodayClassCardViewModel(slot, StartAttendanceFromSchedule));
        }

        OnPropertyChanged(nameof(HasTodaysClasses));
        OnPropertyChanged(nameof(HasNoTodaysClasses));
        List<AttendanceSession> allSessions = classrooms.SelectMany(c => c.AttendanceSessions).ToList();
        SchoolDaysSummary daysSummary = _schoolDaysCalculator.CalculateRecordedProgress(
            allSessions,
            calendarEvents,
            SelectedAcademicYear.TargetSchoolDays,
            SelectedAcademicYear.GradingSystem);
        SchoolDaysCountText = $"{daysSummary.RecordedDays} / {daysSummary.TargetDays}";
        SchoolDaysProgressValue = (double)daysSummary.ProgressPercentage;
        int totalStudents = 0;
        int totalAlerts = 0;
        var riskItems = new List<AtRiskStudentItemViewModel>();

        foreach (Classroom classroom in classrooms)
        {
            totalStudents += classroom.Students.Count;
            int alertCount = 0;
            double attendanceSum = 0;

            foreach (Student student in classroom.Students)
            {
                AttendanceSummary summary = _attendanceCalculator.CalculateForStudent(
                    student.Id,
                    classroom.AttendanceSessions,
                    SelectedAcademicYear.MinAttendancePercentage);

                attendanceSum += (double)summary.AttendancePercentage;

                if (summary.AlertStatus != AttendanceAlertStatus.Normal)
                {
                    alertCount++;
                    riskItems.Add(new AtRiskStudentItemViewModel(
                        classroom,
                        student,
                        _avatarService.GetAvatarFullPath(student.PhotoFileName),
                        summary,
                        OpenStudentDossier));
                }
            }

            totalAlerts += alertCount;
            double healthPercentage = classroom.Students.Count > 0
                ? Math.Round(attendanceSum / classroom.Students.Count, 1)
                : 100.0;

            List<StudentAvatarPreview> previews = classroom.Students
                .OrderBy(s => s.RollNumber)
                .Take(4)
                .Select(s => new StudentAvatarPreview(s.FullName, _avatarService.GetAvatarFullPath(s.PhotoFileName)))
                .ToList();

            ClassroomCards.Add(new ClassroomCardViewModel(
                classroom,
                alertCount,
                healthPercentage,
                previews,
                OpenClassroomSection,
                DeleteClassroomAsync));
        }

        foreach (AtRiskStudentItemViewModel risk in riskItems
                     .OrderByDescending(r => r.AlertStatus)
                     .ThenByDescending(r => r.AbsencePercentage))
        {
            AtRiskStudents.Add(risk);
        }

        TotalClassroomsCount = classrooms.Count;
        TotalStudentsCount = totalStudents;
        TotalAlertsCount = totalAlerts;

        OnPropertyChanged(nameof(HasAtRiskStudents));
        OnPropertyChanged(nameof(HasNoAtRiskStudents));

        if (_appStateService.SelectedClassroom is null && classrooms.Count > 0)
        {
            _appStateService.SelectedClassroom = classrooms[0];
        }
        else if (_appStateService.SelectedClassroom is not null)
        {
            Classroom? refreshed = classrooms.FirstOrDefault(c => c.Id == _appStateService.SelectedClassroom.Id);
            _appStateService.SelectedClassroom = refreshed ?? classrooms.FirstOrDefault();
        }
    }

    [RelayCommand]
    private void OpenCalendar()
    {
        _navigationService.NavigateTo(AppSection.Calendar);
    }

    [RelayCommand]
    private void OpenPlanner()
    {
        _navigationService.NavigateTo(AppSection.Planner);
    }

    private void StartAttendanceFromSchedule(ScheduleSlot slot)
    {
        _navigationService.NavigateToAttendanceWithPrefill(new AttendancePrefillRequest(
            slot.ClassroomId,
            slot.SubjectId,
            DateOnly.FromDateTime(DateTime.Today),
            slot.LessonCount));
    }

    private void OpenClassroomSection(Classroom classroom, AppSection section)
    {
        _appStateService.SelectedClassroom = classroom;
        _navigationService.NavigateTo(section);
    }

    private void OpenStudentDossier(Classroom classroom, Student student)
    {
        _navigationService.NavigateToStudentDossier(classroom, student);
    }
}
