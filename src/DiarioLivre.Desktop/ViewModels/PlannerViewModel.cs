using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiarioLivre.Core.Entities;
using DiarioLivre.Core.Enums;
using DiarioLivre.Core.Interfaces;
using DiarioLivre.Desktop.Localization;
using DiarioLivre.Desktop.Services;

namespace DiarioLivre.Desktop.ViewModels;

public sealed partial class ScheduleSlotItemViewModel : ObservableObject, IDisposable
{
    private readonly ScheduleSlot _slot;
    private readonly Action<ScheduleSlot> _startAttendanceAction;
    private readonly Func<Guid, Task> _deleteAction;

    public ScheduleSlotItemViewModel(
        ScheduleSlot slot,
        Action<ScheduleSlot> startAttendanceAction,
        Func<Guid, Task> deleteAction)
    {
        _slot = slot;
        _startAttendanceAction = startAttendanceAction;
        _deleteAction = deleteAction;
        TimeRangeText = $"{slot.StartTime:HH\\:mm} – {slot.EndTime:HH\\:mm}";
        ClassroomName = slot.Classroom?.Name ?? "—";
        LessonBadge = $"{slot.LessonCount}×";
        RoomLabel = slot.RoomLabel;
        HasRoom = !string.IsNullOrWhiteSpace(slot.RoomLabel);
        LocalizationService.Instance.CultureChanged += OnCultureChanged;
    }

    public LocalizationService Loc => LocalizationService.Instance;
    public Guid Id => _slot.Id;
    public string TimeRangeText { get; }
    public string ClassroomName { get; }
    public string LessonBadge { get; }
    public string? RoomLabel { get; }
    public bool HasRoom { get; }

    public string SubjectDisplay => _slot.Classroom?.IsMultiSubject == true
        ? LocalizationService.Instance.GetString("PolyvalentBadge")
        : _slot.Subject?.Name ?? "—";

    [RelayCommand]
    private void StartAttendance() => _startAttendanceAction(_slot);

    [RelayCommand]
    private Task DeleteAsync() => _deleteAction(Id);

    public void Dispose()
    {
        LocalizationService.Instance.CultureChanged -= OnCultureChanged;
    }

    private void OnCultureChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(SubjectDisplay));
    }
}

public sealed partial class ScheduleDayColumnViewModel : ObservableObject, IDisposable
{
    public ScheduleDayColumnViewModel(DayOfWeek dayOfWeek, IReadOnlyList<ScheduleSlotItemViewModel> slots)
    {
        DayOfWeek = dayOfWeek;
        foreach (ScheduleSlotItemViewModel slot in slots)
        {
            Slots.Add(slot);
        }

        LocalizationService.Instance.CultureChanged += OnCultureChanged;
    }

    public LocalizationService Loc => LocalizationService.Instance;
    public DayOfWeek DayOfWeek { get; }
    public string DayName => LocalizationService.Instance.GetDayOfWeekName(DayOfWeek);
    public ObservableCollection<ScheduleSlotItemViewModel> Slots { get; } = new();
    public bool HasNoSlots => Slots.Count == 0;

    public void Dispose()
    {
        foreach (ScheduleSlotItemViewModel slot in Slots)
        {
            slot.Dispose();
        }
        LocalizationService.Instance.CultureChanged -= OnCultureChanged;
    }

    private void OnCultureChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(DayName));
    }
}

public sealed partial class LessonPlanItemViewModel : ObservableObject, IDisposable
{
    private static readonly IBrush LightPlannedBg = new SolidColorBrush(Color.FromArgb(30, 79, 70, 229));
    private static readonly IBrush DarkPlannedBg = new SolidColorBrush(Color.FromArgb(44, 99, 102, 241));
    private static readonly IBrush LightPlannedFg = new SolidColorBrush(Color.Parse("#4F46E5"));
    private static readonly IBrush DarkPlannedFg = new SolidColorBrush(Color.Parse("#A5B4FC"));

    private static readonly IBrush LightTaughtBg = new SolidColorBrush(Color.FromArgb(30, 22, 163, 74));
    private static readonly IBrush DarkTaughtBg = new SolidColorBrush(Color.FromArgb(38, 34, 197, 94));
    private static readonly IBrush LightTaughtFg = new SolidColorBrush(Color.Parse("#15803D"));
    private static readonly IBrush DarkTaughtFg = new SolidColorBrush(Color.Parse("#4ADE80"));

    private static readonly IBrush LightPostponedBg = new SolidColorBrush(Color.FromArgb(30, 217, 119, 6));
    private static readonly IBrush DarkPostponedBg = new SolidColorBrush(Color.FromArgb(38, 245, 158, 11));
    private static readonly IBrush LightPostponedFg = new SolidColorBrush(Color.Parse("#B45309"));
    private static readonly IBrush DarkPostponedFg = new SolidColorBrush(Color.Parse("#FBBF24"));

    private readonly GradingSystem _gradingSystem;
    private readonly Func<LessonPlan, Task> _cycleStatusAction;
    private readonly Func<LessonPlan, Task> _applyToAttendanceAction;
    private readonly Func<Guid, Task> _deleteAction;

    public LessonPlanItemViewModel(
        LessonPlan plan,
        GradingSystem gradingSystem,
        Func<LessonPlan, Task> cycleStatusAction,
        Func<LessonPlan, Task> applyToAttendanceAction,
        Func<Guid, Task> deleteAction)
    {
        Plan = plan;
        _gradingSystem = gradingSystem;
        _cycleStatusAction = cycleStatusAction;
        _applyToAttendanceAction = applyToAttendanceAction;
        _deleteAction = deleteAction;

        DateDisplay = plan.PlannedDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "—";
        HasBncc = !string.IsNullOrWhiteSpace(plan.BnccCode);
        HasObjectives = !string.IsNullOrWhiteSpace(plan.Objectives);
        HasActivity = !string.IsNullOrWhiteSpace(plan.PlannedActivity);
        HasHomework = !string.IsNullOrWhiteSpace(plan.Homework);

        LocalizationService.Instance.CultureChanged += OnCultureChanged;
    }

    public LessonPlan Plan { get; }
    public LocalizationService Loc => LocalizationService.Instance;
    public Guid Id => Plan.Id;
    public string Title => Plan.Title;
    public string? BnccCode => Plan.BnccCode;
    public bool HasBncc { get; }
    public string? Objectives => Plan.Objectives;
    public bool HasObjectives { get; }
    public string? PlannedActivity => Plan.PlannedActivity;
    public bool HasActivity { get; }
    public string? Homework => Plan.Homework;
    public bool HasHomework { get; }
    public string DateDisplay { get; }
    public string SubjectDisplay => Plan.Subject?.Name ?? LocalizationService.Instance.GetString("PolyvalentBadge");
    public string TermBadge => LocalizationService.Instance.GetTermShortName(_gradingSystem, Plan.TermNumber);
    public string StatusLabel => LocalizationService.Instance.GetLessonPlanStatusName(Plan.Status);

    public IBrush StatusBackground
    {
        get
        {
            bool isDark = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
            return Plan.Status switch
            {
                LessonPlanStatus.Taught => isDark ? DarkTaughtBg : LightTaughtBg,
                LessonPlanStatus.Postponed => isDark ? DarkPostponedBg : LightPostponedBg,
                _ => isDark ? DarkPlannedBg : LightPlannedBg
            };
        }
    }

    public IBrush StatusForeground
    {
        get
        {
            bool isDark = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
            return Plan.Status switch
            {
                LessonPlanStatus.Taught => isDark ? DarkTaughtFg : LightTaughtFg,
                LessonPlanStatus.Postponed => isDark ? DarkPostponedFg : LightPostponedFg,
                _ => isDark ? DarkPlannedFg : LightPlannedFg
            };
        }
    }

    public void RefreshTheme()
    {
        OnPropertyChanged(nameof(StatusBackground));
        OnPropertyChanged(nameof(StatusForeground));
    }

    [RelayCommand]
    private Task CycleStatusAsync() => _cycleStatusAction(Plan);

    [RelayCommand]
    private Task ApplyToAttendanceAsync() => _applyToAttendanceAction(Plan);

    [RelayCommand]
    private Task DeleteAsync() => _deleteAction(Plan.Id);

    public void Dispose()
    {
        LocalizationService.Instance.CultureChanged -= OnCultureChanged;
    }

    private void OnCultureChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(SubjectDisplay));
        OnPropertyChanged(nameof(TermBadge));
        OnPropertyChanged(nameof(StatusLabel));
    }
}

public sealed partial class SeatingStudentChipViewModel : ObservableObject
{
    private readonly Action<Guid> _selectAction;

    [ObservableProperty]
    private bool _isSelected;

    public SeatingStudentChipViewModel(Student student, string? photoPath, bool isSelected, Action<Guid> selectAction)
    {
        Student = student;
        PhotoPath = photoPath;
        _isSelected = isSelected;
        _selectAction = selectAction;
        RollDisplay = $"{student.RollNumber:D2}";
    }

    public Student Student { get; }
    public Guid StudentId => Student.Id;
    public string FullName => Student.FullName;
    public string RollDisplay { get; }
    public string? PhotoPath { get; }
    public bool HasSpecialNeeds => Student.HasSpecialNeeds;

    [RelayCommand]
    private void Select() => _selectAction(StudentId);
}

public sealed partial class SeatingCellViewModel : ObservableObject
{
    private static readonly IBrush EmptyBorderBrush = new SolidColorBrush(Color.FromArgb(64, 148, 163, 184));
    private static readonly IBrush SelectedBorderBrush = new SolidColorBrush(Color.Parse("#6366F1"));

    private readonly Action<int, int> _clickSeatAction;

    [ObservableProperty]
    private bool _isSelected;

    public SeatingCellViewModel(
        int row,
        int column,
        Student? student,
        string? photoPath,
        bool isSelected,
        Action<int, int> clickSeatAction)
    {
        Row = row;
        Column = column;
        Student = student;
        PhotoPath = photoPath;
        _isSelected = isSelected;
        _clickSeatAction = clickSeatAction;
    }

    public LocalizationService Loc => LocalizationService.Instance;
    public int Row { get; }
    public int Column { get; }
    public Student? Student { get; }
    public string? PhotoPath { get; }
    public bool IsOccupied => Student is not null;
    public bool IsEmpty => Student is null;
    public string FullName => Student?.FullName ?? string.Empty;
    public string RollDisplay => Student is not null ? $"{Student.RollNumber:D2}" : $"F{Row + 1}·C{Column + 1}";
    public bool HasSpecialNeeds => Student?.HasSpecialNeeds ?? false;

    public IBrush CellBorderBrush => IsSelected ? SelectedBorderBrush : EmptyBorderBrush;
    public Avalonia.Thickness CellBorderThickness => IsSelected
        ? new Avalonia.Thickness(2)
        : new Avalonia.Thickness(1);

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(CellBorderBrush));
        OnPropertyChanged(nameof(CellBorderThickness));
    }

    [RelayCommand]
    private void ClickSeat() => _clickSeatAction(Row, Column);
}

/// <summary>
/// Unified ViewModel for Weekly Timetable, BNCC Lesson Planner, and Visual Classroom Seating Chart.
/// </summary>
public sealed partial class PlannerViewModel : ViewModelBase
{
    private readonly IAcademicYearRepository _academicYearRepository;
    private readonly IClassroomRepository _classroomRepository;
    private readonly IScheduleRepository _scheduleRepository;
    private readonly ILessonPlanRepository _lessonPlanRepository;
    private readonly IAvatarService _avatarService;
    private readonly AppStateService _appStateService;
    private readonly NavigationService _navigationService;
    private readonly ToastService _toastService;

    private AcademicYear? _activeYear;
    private List<Student> _classroomStudents = new();
    private readonly Dictionary<(int Row, int Column), Guid> _seatAssignments = new();
    private Guid? _selectedStudentToPlace;
    private (int Row, int Column)? _selectedSeatOrigin;
    private bool _isLoading;

    [ObservableProperty]
    private int _activeTabIndex;

    [ObservableProperty]
    private Classroom? _selectedClassroom;

    [ObservableProperty]
    private Subject? _selectedSubject;

    [ObservableProperty]
    private LocalizedOption<DayOfWeek>? _selectedDayOption;

    [ObservableProperty]
    private string _newSlotStartTime = "07:30";

    [ObservableProperty]
    private string _newSlotEndTime = "09:10";

    [ObservableProperty]
    private decimal? _newSlotLessonCount = 2m;

    [ObservableProperty]
    private string _newSlotRoom = string.Empty;

    [ObservableProperty]
    private LocalizedOption<int>? _selectedTermOption;

    [ObservableProperty]
    private DateTimeOffset? _newPlanDate = DateTimeOffset.Now.Date;

    [ObservableProperty]
    private string _newPlanTitle = string.Empty;

    [ObservableProperty]
    private string _newPlanBnccCode = string.Empty;

    [ObservableProperty]
    private string _newPlanObjectives = string.Empty;

    [ObservableProperty]
    private string _newPlanActivity = string.Empty;

    [ObservableProperty]
    private string _newPlanHomework = string.Empty;

    [ObservableProperty]
    private decimal? _seatingRowsValue = 5m;

    [ObservableProperty]
    private decimal? _seatingColumnsValue = 6m;

    [ObservableProperty]
    private int _seatingGridRows = 5;

    [ObservableProperty]
    private int _seatingGridColumns = 6;

    public PlannerViewModel(
        IAcademicYearRepository academicYearRepository,
        IClassroomRepository classroomRepository,
        IScheduleRepository scheduleRepository,
        ILessonPlanRepository lessonPlanRepository,
        IAvatarService avatarService,
        AppStateService appStateService,
        NavigationService navigationService,
        ToastService toastService)
    {
        _academicYearRepository = academicYearRepository;
        _classroomRepository = classroomRepository;
        _scheduleRepository = scheduleRepository;
        _lessonPlanRepository = lessonPlanRepository;
        _avatarService = avatarService;
        _appStateService = appStateService;
        _navigationService = navigationService;
        _toastService = toastService;

        DayOptions = new ObservableCollection<LocalizedOption<DayOfWeek>>
        {
            new(DayOfWeek.Monday, loc => loc.GetDayOfWeekName(DayOfWeek.Monday)),
            new(DayOfWeek.Tuesday, loc => loc.GetDayOfWeekName(DayOfWeek.Tuesday)),
            new(DayOfWeek.Wednesday, loc => loc.GetDayOfWeekName(DayOfWeek.Wednesday)),
            new(DayOfWeek.Thursday, loc => loc.GetDayOfWeekName(DayOfWeek.Thursday)),
            new(DayOfWeek.Friday, loc => loc.GetDayOfWeekName(DayOfWeek.Friday)),
            new(DayOfWeek.Saturday, loc => loc.GetDayOfWeekName(DayOfWeek.Saturday))
        };
        _selectedDayOption = DayOptions[0];
    }

    public ObservableCollection<Classroom> Classrooms { get; } = new();
    public ObservableCollection<Subject> Subjects { get; } = new();
    public ObservableCollection<LocalizedOption<DayOfWeek>> DayOptions { get; }
    public ObservableCollection<LocalizedOption<int>> TermOptions { get; } = new();
    public ObservableCollection<ScheduleDayColumnViewModel> ScheduleDays { get; } = new();
    public ObservableCollection<LessonPlanItemViewModel> LessonPlans { get; } = new();
    public ObservableCollection<SeatingCellViewModel> SeatingCells { get; } = new();
    public ObservableCollection<SeatingStudentChipViewModel> UnassignedStudents { get; } = new();

    public bool IsScheduleTabActive => ActiveTabIndex == 0;
    public bool IsLessonPlansTabActive => ActiveTabIndex == 1;
    public bool IsSeatingTabActive => ActiveTabIndex == 2;
    public bool IsSubjectSelectionVisible => SelectedClassroom is { IsMultiSubject: false };
    public bool HasNoLessonPlans => LessonPlans.Count == 0;
    public bool HasUnassignedStudents => UnassignedStudents.Count > 0;

    public override async Task LoadAsync()
    {
        _isLoading = true;
        try
        {
            AcademicYear? year = _appStateService.SelectedAcademicYear;
            if (year is null)
            {
                IReadOnlyList<AcademicYear> years = await _academicYearRepository.GetAllAsync();
                year = years.FirstOrDefault();
                _appStateService.SelectedAcademicYear = year;
            }

            _activeYear = year;
            RebuildTermOptions(year?.GradingSystem ?? GradingSystem.Semester);

            Classrooms.Clear();
            if (year is not null)
            {
                IReadOnlyList<Classroom> classrooms = await _classroomRepository.GetByAcademicYearAsync(year.Id);
                foreach (Classroom classroom in classrooms)
                {
                    Classrooms.Add(classroom);
                }
            }

            Classroom? target = _appStateService.SelectedClassroom is not null
                ? Classrooms.FirstOrDefault(c => c.Id == _appStateService.SelectedClassroom.Id)
                : null;

            SelectedClassroom = target ?? Classrooms.FirstOrDefault();
        }
        finally
        {
            _isLoading = false;
        }

        await LoadTimetableAsync();
        await LoadClassroomContextAsync();
    }

    public override Task TrySaveAsync() => activeSave();

    public override void RefreshThemeVisuals()
    {
        foreach (LessonPlanItemViewModel plan in LessonPlans)
        {
            plan.RefreshTheme();
        }
    }

    private Task activeSave() => ActiveTabIndex switch
    {
        0 => AddScheduleSlotAsync(),
        1 => CreateLessonPlanAsync(),
        2 => SaveSeatingChartAsync(),
        _ => Task.CompletedTask
    };

    partial void OnActiveTabIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsScheduleTabActive));
        OnPropertyChanged(nameof(IsLessonPlansTabActive));
        OnPropertyChanged(nameof(IsSeatingTabActive));
    }

    partial void OnSelectedClassroomChanged(Classroom? value)
    {
        _appStateService.SelectedClassroom = value;
        OnPropertyChanged(nameof(IsSubjectSelectionVisible));
        if (!_isLoading)
        {
            _ = LoadClassroomContextAsync();
        }
    }

    partial void OnSeatingRowsValueChanged(decimal? value)
    {
        if (_isLoading || !value.HasValue)
        {
            return;
        }

        SeatingGridRows = Math.Clamp((int)value.Value, 2, 8);
        RebuildSeatingVisuals();
    }

    partial void OnSeatingColumnsValueChanged(decimal? value)
    {
        if (_isLoading || !value.HasValue)
        {
            return;
        }

        SeatingGridColumns = Math.Clamp((int)value.Value, 2, 8);
        RebuildSeatingVisuals();
    }

    [RelayCommand]
    private void SelectScheduleTab() => ActiveTabIndex = 0;

    [RelayCommand]
    private void SelectLessonPlansTab() => ActiveTabIndex = 1;

    [RelayCommand]
    private void SelectSeatingTab() => ActiveTabIndex = 2;

    [RelayCommand]
    private async Task AddScheduleSlotAsync()
    {
        if (_activeYear is null || SelectedClassroom is null)
        {
            return;
        }

        if (!TimeOnly.TryParse(NewSlotStartTime.Trim(), out TimeOnly start) ||
            !TimeOnly.TryParse(NewSlotEndTime.Trim(), out TimeOnly end) ||
            end <= start)
        {
            _toastService.ShowWarning("HH:mm (ex: 07:30 – 09:10)");
            return;
        }

        var slot = new ScheduleSlot
        {
            Id = Guid.NewGuid(),
            AcademicYearId = _activeYear.Id,
            ClassroomId = SelectedClassroom.Id,
            SubjectId = SelectedClassroom.IsMultiSubject ? null : SelectedSubject?.Id,
            DayOfWeek = SelectedDayOption?.Value ?? DayOfWeek.Monday,
            StartTime = start,
            EndTime = end,
            LessonCount = Math.Clamp((int)(NewSlotLessonCount ?? 1m), 1, 6),
            RoomLabel = string.IsNullOrWhiteSpace(NewSlotRoom) ? null : NewSlotRoom.Trim()
        };

        await _scheduleRepository.AddAsync(slot);
        NewSlotRoom = string.Empty;
        _toastService.ShowSuccess(Loc.GetString("SavedSuccessMessage"));
        await LoadTimetableAsync();
    }

    [RelayCommand]
    private async Task CreateLessonPlanAsync()
    {
        if (SelectedClassroom is null || string.IsNullOrWhiteSpace(NewPlanTitle))
        {
            return;
        }

        var plan = new LessonPlan
        {
            Id = Guid.NewGuid(),
            ClassroomId = SelectedClassroom.Id,
            SubjectId = SelectedClassroom.IsMultiSubject ? null : SelectedSubject?.Id,
            TermNumber = SelectedTermOption?.Value ?? 1,
            PlannedDate = NewPlanDate.HasValue ? DateOnly.FromDateTime(NewPlanDate.Value.DateTime) : null,
            Title = NewPlanTitle.Trim(),
            BnccCode = string.IsNullOrWhiteSpace(NewPlanBnccCode) ? null : NewPlanBnccCode.Trim().ToUpperInvariant(),
            Objectives = string.IsNullOrWhiteSpace(NewPlanObjectives) ? null : NewPlanObjectives.Trim(),
            PlannedActivity = string.IsNullOrWhiteSpace(NewPlanActivity) ? null : NewPlanActivity.Trim(),
            Homework = string.IsNullOrWhiteSpace(NewPlanHomework) ? null : NewPlanHomework.Trim(),
            Status = LessonPlanStatus.Planned
        };

        await _lessonPlanRepository.AddAsync(plan);
        NewPlanTitle = string.Empty;
        NewPlanBnccCode = string.Empty;
        NewPlanObjectives = string.Empty;
        NewPlanActivity = string.Empty;
        NewPlanHomework = string.Empty;

        _toastService.ShowSuccess(Loc.GetString("SavedSuccessMessage"));
        await LoadLessonPlansAsync();
    }

    [RelayCommand]
    private void AutoFillSeats()
    {
        _seatAssignments.Clear();
        _selectedStudentToPlace = null;
        _selectedSeatOrigin = null;

        int idx = 0;
        foreach (Student student in _classroomStudents.OrderBy(s => s.RollNumber))
        {
            int row = idx / SeatingGridColumns;
            int col = idx % SeatingGridColumns;
            if (row >= SeatingGridRows)
            {
                break;
            }

            _seatAssignments[(row, col)] = student.Id;
            idx++;
        }

        RebuildSeatingVisuals();
    }

    [RelayCommand]
    private void ClearSeats()
    {
        _seatAssignments.Clear();
        _selectedStudentToPlace = null;
        _selectedSeatOrigin = null;
        RebuildSeatingVisuals();
    }

    [RelayCommand]
    private async Task SaveSeatingChartAsync()
    {
        if (SelectedClassroom is null)
        {
            return;
        }

        var reverseLookup = _seatAssignments.ToDictionary(kv => kv.Value, kv => kv.Key);
        var updates = new List<(Guid StudentId, int? SeatRow, int? SeatColumn)>(_classroomStudents.Count);

        foreach (Student student in _classroomStudents)
        {
            if (reverseLookup.TryGetValue(student.Id, out (int Row, int Column) pos))
            {
                updates.Add((student.Id, pos.Row, pos.Column));
            }
            else
            {
                updates.Add((student.Id, null, null));
            }
        }

        await _classroomRepository.UpdateSeatingGridAsync(
            SelectedClassroom.Id,
            SeatingGridRows,
            SeatingGridColumns,
            updates);

        _toastService.ShowSuccess(Loc.GetString("SavedSuccessMessage"));
    }

    private async Task LoadTimetableAsync()
    {
        foreach (ScheduleDayColumnViewModel existing in ScheduleDays)
        {
            existing.Dispose();
        }
        ScheduleDays.Clear();

        if (_activeYear is null)
        {
            return;
        }

        IReadOnlyList<ScheduleSlot> slots = await _scheduleRepository.GetByAcademicYearAsync(_activeYear.Id);
        DayOfWeek[] weekDays =
        {
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday,
            DayOfWeek.Saturday
        };

        foreach (DayOfWeek day in weekDays)
        {
            List<ScheduleSlotItemViewModel> daySlots = slots
                .Where(s => s.DayOfWeek == day)
                .OrderBy(s => s.StartTime)
                .Select(s => new ScheduleSlotItemViewModel(s, StartAttendanceFromSlot, DeleteScheduleSlotAsync))
                .ToList();

            ScheduleDays.Add(new ScheduleDayColumnViewModel(day, daySlots));
        }
    }

    private async Task LoadClassroomContextAsync()
    {
        _isLoading = true;
        try
        {
            Subjects.Clear();
            _classroomStudents.Clear();
            _seatAssignments.Clear();
            _selectedStudentToPlace = null;
            _selectedSeatOrigin = null;

            if (SelectedClassroom is not null)
            {
                Classroom? details = await _classroomRepository.GetWithDetailsAsync(SelectedClassroom.Id);
                if (details is not null)
                {
                    foreach (Subject s in details.Subjects)
                    {
                        Subjects.Add(s);
                    }

                    _classroomStudents = details.Students.OrderBy(s => s.RollNumber).ToList();
                    SeatingGridRows = Math.Clamp(details.SeatRows, 2, 8);
                    SeatingGridColumns = Math.Clamp(details.SeatColumns, 2, 8);
                    SeatingRowsValue = SeatingGridRows;
                    SeatingColumnsValue = SeatingGridColumns;

                    foreach (Student st in _classroomStudents)
                    {
                        if (st.SeatRow.HasValue && st.SeatColumn.HasValue &&
                            st.SeatRow.Value >= 0 && st.SeatRow.Value < SeatingGridRows &&
                            st.SeatColumn.Value >= 0 && st.SeatColumn.Value < SeatingGridColumns)
                        {
                            _seatAssignments[(st.SeatRow.Value, st.SeatColumn.Value)] = st.Id;
                        }
                    }
                }
            }

            SelectedSubject = Subjects.FirstOrDefault();
            RebuildSeatingVisuals();
        }
        finally
        {
            _isLoading = false;
        }

        await LoadLessonPlansAsync();
    }

    private async Task LoadLessonPlansAsync()
    {
        foreach (LessonPlanItemViewModel existing in LessonPlans)
        {
            existing.Dispose();
        }
        LessonPlans.Clear();

        if (SelectedClassroom is null)
        {
            OnPropertyChanged(nameof(HasNoLessonPlans));
            return;
        }

        GradingSystem grading = _activeYear?.GradingSystem ?? GradingSystem.Semester;
        IReadOnlyList<LessonPlan> plans = await _lessonPlanRepository.GetByClassroomAsync(SelectedClassroom.Id);

        foreach (LessonPlan plan in plans)
        {
            LessonPlans.Add(new LessonPlanItemViewModel(
                plan,
                grading,
                CycleLessonPlanStatusAsync,
                ApplyLessonPlanToAttendanceAsync,
                DeleteLessonPlanAsync));
        }

        OnPropertyChanged(nameof(HasNoLessonPlans));
    }

    private void RebuildTermOptions(GradingSystem gradingSystem)
    {
        TermOptions.Clear();
        int count = gradingSystem.GetTermCount();
        int desired = Math.Clamp(_appStateService.SelectedTermNumber, 1, count);

        for (int i = 1; i <= count; i++)
        {
            int termIndex = i;
            TermOptions.Add(new LocalizedOption<int>(termIndex, loc => loc.GetTermName(gradingSystem, termIndex)));
        }

        SelectedTermOption = TermOptions.FirstOrDefault(t => t.Value == desired) ?? TermOptions.FirstOrDefault();
    }

    private void StartAttendanceFromSlot(ScheduleSlot slot)
    {
        _navigationService.NavigateToAttendanceWithPrefill(new AttendancePrefillRequest(
            slot.ClassroomId,
            slot.SubjectId,
            DateOnly.FromDateTime(DateTime.Today),
            slot.LessonCount));
    }

    private async Task DeleteScheduleSlotAsync(Guid slotId)
    {
        await _scheduleRepository.DeleteAsync(slotId);
        _toastService.ShowInfo(Loc.GetString("SavedSuccessMessage"));
        await LoadTimetableAsync();
    }

    private async Task CycleLessonPlanStatusAsync(LessonPlan plan)
    {
        plan.Status = plan.Status switch
        {
            LessonPlanStatus.Planned => LessonPlanStatus.Taught,
            LessonPlanStatus.Taught => LessonPlanStatus.Postponed,
            _ => LessonPlanStatus.Planned
        };

        await _lessonPlanRepository.UpdateAsync(plan);
        await LoadLessonPlansAsync();
    }

    private async Task ApplyLessonPlanToAttendanceAsync(LessonPlan plan)
    {
        if (plan.Status != LessonPlanStatus.Taught)
        {
            plan.Status = LessonPlanStatus.Taught;
            await _lessonPlanRepository.UpdateAsync(plan);
        }

        string topic = string.IsNullOrWhiteSpace(plan.BnccCode)
            ? plan.Title
            : $"{plan.Title} [{plan.BnccCode}]";

        DateOnly date = plan.PlannedDate ?? DateOnly.FromDateTime(DateTime.Today);
        Guid? effectiveSubjectId = SelectedClassroom?.IsMultiSubject == true ? null : plan.SubjectId;

        _navigationService.NavigateToAttendanceWithPrefill(new AttendancePrefillRequest(
            plan.ClassroomId,
            effectiveSubjectId,
            date,
            LessonCount: 1,
            TopicCovered: topic,
            DailyActivity: plan.PlannedActivity,
            Homework: plan.Homework));
    }

    private async Task DeleteLessonPlanAsync(Guid planId)
    {
        await _lessonPlanRepository.DeleteAsync(planId);
        _toastService.ShowInfo(Loc.GetString("SavedSuccessMessage"));
        await LoadLessonPlansAsync();
    }

    private void SelectUnassignedStudent(Guid studentId)
    {
        _selectedSeatOrigin = null;
        _selectedStudentToPlace = _selectedStudentToPlace == studentId ? null : studentId;
        RebuildSeatingVisuals();
    }

    private void ClickSeat(int row, int col)
    {
        var clickedPos = (row, col);

        if (_selectedStudentToPlace is Guid studentToPlace)
        {
            // Remove if already placed elsewhere
            (int Row, int Column)? existingKey = _seatAssignments
                .Where(kv => kv.Value == studentToPlace)
                .Select(kv => ((int, int)?)kv.Key)
                .FirstOrDefault();

            if (existingKey.HasValue)
            {
                _seatAssignments.Remove(existingKey.Value);
            }

            _seatAssignments[clickedPos] = studentToPlace;
            _selectedStudentToPlace = null;
            RebuildSeatingVisuals();
            return;
        }

        if (_selectedSeatOrigin is (int originRow, int originCol))
        {
            if (originRow == row && originCol == col)
            {
                _selectedSeatOrigin = null;
                RebuildSeatingVisuals();
                return;
            }

            bool originHasStudent = _seatAssignments.TryGetValue((originRow, originCol), out Guid originStudent);
            bool destHasStudent = _seatAssignments.TryGetValue(clickedPos, out Guid destStudent);

            if (originHasStudent)
            {
                if (destHasStudent)
                {
                    _seatAssignments[(originRow, originCol)] = destStudent;
                    _seatAssignments[clickedPos] = originStudent;
                }
                else
                {
                    _seatAssignments.Remove((originRow, originCol));
                    _seatAssignments[clickedPos] = originStudent;
                }
            }

            _selectedSeatOrigin = null;
            RebuildSeatingVisuals();
            return;
        }

        if (_seatAssignments.ContainsKey(clickedPos))
        {
            _selectedSeatOrigin = clickedPos;
            RebuildSeatingVisuals();
        }
    }

    private void RebuildSeatingVisuals()
    {
        // Prune out-of-bounds entries cleanly when grid dimensions shrink
        List<(int Row, int Column)> outOfBounds = _seatAssignments.Keys
            .Where(k => k.Row >= SeatingGridRows || k.Column >= SeatingGridColumns)
            .ToList();
        foreach ((int Row, int Column) key in outOfBounds)
        {
            _seatAssignments.Remove(key);
        }

        SeatingCells.Clear();
        UnassignedStudents.Clear();

        var studentLookup = _classroomStudents.ToDictionary(s => s.Id);
        var assignedStudentIds = _seatAssignments.Values.ToHashSet();

        for (int r = 0; r < SeatingGridRows; r++)
        {
            for (int c = 0; c < SeatingGridColumns; c++)
            {
                Student? occupant = null;
                if (_seatAssignments.TryGetValue((r, c), out Guid stId))
                {
                    studentLookup.TryGetValue(stId, out occupant);
                }

                bool isSelected = _selectedSeatOrigin is (int sr, int sc) && sr == r && sc == c;
                string? photo = occupant is not null ? _avatarService.GetAvatarFullPath(occupant.PhotoFileName) : null;
                SeatingCells.Add(new SeatingCellViewModel(r, c, occupant, photo, isSelected, ClickSeat));
            }
        }

        foreach (Student student in _classroomStudents.OrderBy(s => s.RollNumber))
        {
            if (assignedStudentIds.Contains(student.Id))
            {
                continue;
            }

            bool isSelected = _selectedStudentToPlace == student.Id;
            string? photo = _avatarService.GetAvatarFullPath(student.PhotoFileName);
            UnassignedStudents.Add(new SeatingStudentChipViewModel(student, photo, isSelected, SelectUnassignedStudent));
        }

        OnPropertyChanged(nameof(HasUnassignedStudents));
    }
}
