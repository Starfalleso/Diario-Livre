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
using DiarioLivre.Core.Models;
using DiarioLivre.Desktop.Localization;
using DiarioLivre.Desktop.Services;

namespace DiarioLivre.Desktop.ViewModels;

public sealed partial class CalendarDayCellViewModel : ObservableObject
{
    private static readonly IBrush TransparentBrush = Brushes.Transparent;
    private static readonly IBrush WeekendBgBrush = new SolidColorBrush(Color.FromArgb(20, 148, 163, 184));
    private static readonly IBrush LightHolidayBgBrush = new SolidColorBrush(Color.FromArgb(34, 220, 38, 38));
    private static readonly IBrush DarkHolidayBgBrush = new SolidColorBrush(Color.FromArgb(42, 239, 68, 68));
    private static readonly IBrush LightSchoolSaturdayBgBrush = new SolidColorBrush(Color.FromArgb(34, 79, 70, 229));
    private static readonly IBrush DarkSchoolSaturdayBgBrush = new SolidColorBrush(Color.FromArgb(46, 99, 102, 241));

    private static readonly IBrush LightDefaultBorderBrush = new SolidColorBrush(Color.FromArgb(45, 148, 163, 184));
    private static readonly IBrush DarkDefaultBorderBrush = new SolidColorBrush(Color.FromArgb(45, 100, 116, 139));
    private static readonly IBrush TodayBorderBrush = new SolidColorBrush(Color.Parse("#818CF8"));
    private static readonly IBrush SelectedBorderBrush = new SolidColorBrush(Color.Parse("#6366F1"));

    private static readonly IBrush LightHolidayTextBrush = new SolidColorBrush(Color.Parse("#DC2626"));
    private static readonly IBrush DarkHolidayTextBrush = new SolidColorBrush(Color.Parse("#F87171"));
    private static readonly IBrush LightSchoolSaturdayTextBrush = new SolidColorBrush(Color.Parse("#4F46E5"));
    private static readonly IBrush DarkSchoolSaturdayTextBrush = new SolidColorBrush(Color.Parse("#A5B4FC"));

    private readonly Action<DateOnly> _selectAction;

    [ObservableProperty]
    private bool _isSelected;

    public CalendarDayCellViewModel(
        DateOnly date,
        bool isCurrentMonth,
        bool isToday,
        bool isSelected,
        int recordedLessonsCount,
        IReadOnlyList<SchoolCalendarEvent> eventsOnDate,
        Action<DateOnly> selectAction)
    {
        Date = date;
        IsCurrentMonth = isCurrentMonth;
        IsToday = isToday;
        _isSelected = isSelected;
        RecordedLessonsCount = recordedLessonsCount;
        HasRecordedLesson = recordedLessonsCount > 0;
        _selectAction = selectAction;

        DayNumberText = date.Day.ToString(CultureInfo.InvariantCulture);
        IsWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
        LessonBadgeText = recordedLessonsCount > 0 ? $"{recordedLessonsCount}×" : string.Empty;
        ContentOpacity = isCurrentMonth ? 1.0 : 0.36;

        bool isDark = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
        DefaultBorderBrush = isDark ? DarkDefaultBorderBrush : LightDefaultBorderBrush;

        SchoolCalendarEvent? primaryEvent = eventsOnDate.FirstOrDefault(e => e.Type.CountsAsSchoolDay())
            ?? eventsOnDate.FirstOrDefault();

        if (primaryEvent is not null)
        {
            HasEvent = true;
            EventBadgeText = primaryEvent.Title;
            bool countsAsDay = primaryEvent.Type.CountsAsSchoolDay();
            CellBackground = countsAsDay
                ? (isDark ? DarkSchoolSaturdayBgBrush : LightSchoolSaturdayBgBrush)
                : (isDark ? DarkHolidayBgBrush : LightHolidayBgBrush);
            EventLabelBrush = countsAsDay
                ? (isDark ? DarkSchoolSaturdayTextBrush : LightSchoolSaturdayTextBrush)
                : (isDark ? DarkHolidayTextBrush : LightHolidayTextBrush);
        }
        else if (IsWeekend)
        {
            HasEvent = false;
            EventBadgeText = string.Empty;
            CellBackground = WeekendBgBrush;
            EventLabelBrush = TransparentBrush;
        }
        else
        {
            HasEvent = false;
            EventBadgeText = string.Empty;
            CellBackground = TransparentBrush;
            EventLabelBrush = TransparentBrush;
        }
    }

    public DateOnly Date { get; }
    public bool IsCurrentMonth { get; }
    public bool IsToday { get; }
    public bool IsWeekend { get; }
    public int RecordedLessonsCount { get; }
    public bool HasRecordedLesson { get; }
    public bool HasEvent { get; }
    public string DayNumberText { get; }
    public string LessonBadgeText { get; }
    public string EventBadgeText { get; }
    public double ContentOpacity { get; }
    public IBrush CellBackground { get; }
    public IBrush EventLabelBrush { get; }
    private IBrush DefaultBorderBrush { get; }

    public IBrush CellBorderBrush => IsSelected
        ? SelectedBorderBrush
        : IsToday
            ? TodayBorderBrush
            : DefaultBorderBrush;

    public Avalonia.Thickness CellBorderThickness => IsSelected
        ? new Avalonia.Thickness(2)
        : IsToday
            ? new Avalonia.Thickness(1.5)
            : new Avalonia.Thickness(1);

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(CellBorderBrush));
        OnPropertyChanged(nameof(CellBorderThickness));
    }

    [RelayCommand]
    private void Select() => _selectAction(Date);
}

public sealed partial class CalendarEventItemViewModel : ObservableObject, IDisposable
{
    private static readonly IBrush LightHolidayBgBrush = new SolidColorBrush(Color.FromArgb(30, 220, 38, 38));
    private static readonly IBrush DarkHolidayBgBrush = new SolidColorBrush(Color.FromArgb(40, 239, 68, 68));
    private static readonly IBrush LightHolidayFgBrush = new SolidColorBrush(Color.Parse("#DC2626"));
    private static readonly IBrush DarkHolidayFgBrush = new SolidColorBrush(Color.Parse("#F87171"));
    private static readonly IBrush LightSchoolSaturdayBgBrush = new SolidColorBrush(Color.FromArgb(30, 79, 70, 229));
    private static readonly IBrush DarkSchoolSaturdayBgBrush = new SolidColorBrush(Color.FromArgb(44, 99, 102, 241));
    private static readonly IBrush LightSchoolSaturdayFgBrush = new SolidColorBrush(Color.Parse("#4F46E5"));
    private static readonly IBrush DarkSchoolSaturdayFgBrush = new SolidColorBrush(Color.Parse("#A5B4FC"));

    private readonly GradingSystem _gradingSystem;
    private readonly Func<Guid, Task> _deleteAction;

    public CalendarEventItemViewModel(
        SchoolCalendarEvent calendarEvent,
        GradingSystem gradingSystem,
        Func<Guid, Task> deleteAction)
    {
        Event = calendarEvent;
        _gradingSystem = gradingSystem;
        _deleteAction = deleteAction;

        DateDisplay = calendarEvent.Date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        HasNotes = !string.IsNullOrWhiteSpace(calendarEvent.Notes);
        bool countsAsSchoolDay = calendarEvent.Type.CountsAsSchoolDay();
        bool isDark = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
        BadgeBackground = countsAsSchoolDay
            ? (isDark ? DarkSchoolSaturdayBgBrush : LightSchoolSaturdayBgBrush)
            : (isDark ? DarkHolidayBgBrush : LightHolidayBgBrush);
        BadgeForeground = countsAsSchoolDay
            ? (isDark ? DarkSchoolSaturdayFgBrush : LightSchoolSaturdayFgBrush)
            : (isDark ? DarkHolidayFgBrush : LightHolidayFgBrush);

        LocalizationService.Instance.CultureChanged += OnCultureChanged;
    }

    public SchoolCalendarEvent Event { get; }
    public LocalizationService Loc => LocalizationService.Instance;
    public Guid Id => Event.Id;
    public string DateDisplay { get; }
    public string Title => Event.Title;
    public string? Notes => Event.Notes;
    public bool HasNotes { get; }
    public IBrush BadgeBackground { get; }
    public IBrush BadgeForeground { get; }
    public string TypeLabel => LocalizationService.Instance.GetCalendarEventTypeName(Event.Type);
    public string TermBadge => LocalizationService.Instance.GetTermShortName(_gradingSystem, Event.TermNumber);

    [RelayCommand]
    private Task DeleteAsync() => _deleteAction(Id);

    public void Dispose()
    {
        LocalizationService.Instance.CultureChanged -= OnCultureChanged;
    }

    private void OnCultureChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(TypeLabel));
        OnPropertyChanged(nameof(TermBadge));
    }
}

public sealed partial class CalendarViewModel : ViewModelBase
{
    private readonly IAcademicYearRepository _academicYearRepository;
    private readonly IClassroomRepository _classroomRepository;
    private readonly ISchoolCalendarRepository _calendarRepository;
    private readonly ISchoolDaysCalculator _schoolDaysCalculator;
    private readonly AppStateService _appStateService;
    private readonly ToastService _toastService;

    private AcademicYear? _activeYear;
    private List<Classroom> _classrooms = new();
    private List<AttendanceSession> _allSessions = new();
    private List<SchoolCalendarEvent> _allEvents = new();
    private int? _selectedTermFilter;
    private DateOnly _currentMonthFirstDay = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateOnly _selectedDate = DateOnly.FromDateTime(DateTime.Today);

    [ObservableProperty]
    private int _recordedSchoolDays;

    [ObservableProperty]
    private int _targetSchoolDays = 200;

    [ObservableProperty]
    private int _remainingSchoolDays = 200;

    [ObservableProperty]
    private double _schoolDaysProgressValue;

    [ObservableProperty]
    private string _schoolDaysProgressText = "0 / 200";

    [ObservableProperty]
    private int _plannedMonthDays;

    [ObservableProperty]
    private int _schoolSaturdaysCount;

    [ObservableProperty]
    private int _nonSchoolDaysCount;

    [ObservableProperty]
    private string _selectedDateStatusText = string.Empty;

    [ObservableProperty]
    private bool _hasSelectedDateStatus;

    [ObservableProperty]
    private DateTimeOffset? _newEventDate = DateTimeOffset.Now.Date;

    [ObservableProperty]
    private LocalizedOption<SchoolCalendarEventType>? _selectedEventTypeOption;

    [ObservableProperty]
    private string _newEventTitle = string.Empty;

    [ObservableProperty]
    private string _newEventNotes = string.Empty;

    public CalendarViewModel(
        IAcademicYearRepository academicYearRepository,
        IClassroomRepository classroomRepository,
        ISchoolCalendarRepository calendarRepository,
        ISchoolDaysCalculator schoolDaysCalculator,
        AppStateService appStateService,
        ToastService toastService)
    {
        _academicYearRepository = academicYearRepository;
        _classroomRepository = classroomRepository;
        _calendarRepository = calendarRepository;
        _schoolDaysCalculator = schoolDaysCalculator;
        _appStateService = appStateService;
        _toastService = toastService;

        EventTypeOptions = new ObservableCollection<LocalizedOption<SchoolCalendarEventType>>
        {
            new(SchoolCalendarEventType.Holiday, loc => loc.GetCalendarEventTypeName(SchoolCalendarEventType.Holiday)),
            new(SchoolCalendarEventType.Recess, loc => loc.GetCalendarEventTypeName(SchoolCalendarEventType.Recess)),
            new(SchoolCalendarEventType.SchoolSaturday, loc => loc.GetCalendarEventTypeName(SchoolCalendarEventType.SchoolSaturday)),
            new(SchoolCalendarEventType.PedagogicalDay, loc => loc.GetCalendarEventTypeName(SchoolCalendarEventType.PedagogicalDay)),
            new(SchoolCalendarEventType.ExtraSchoolDay, loc => loc.GetCalendarEventTypeName(SchoolCalendarEventType.ExtraSchoolDay))
        };
        _selectedEventTypeOption = EventTypeOptions[2];

        LocalizationService.Instance.CultureChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(CurrentMonthLabel));
            OnPropertyChanged(nameof(SelectedDateDisplay));
            RefreshSelectedDateDetails();
        };
    }

    public ObservableCollection<TermPillViewModel> TermFilterPills { get; } = new();
    public ObservableCollection<CalendarDayCellViewModel> DayCells { get; } = new();
    public ObservableCollection<CalendarEventItemViewModel> PeriodEvents { get; } = new();
    public ObservableCollection<string> SelectedDateLessons { get; } = new();
    public ObservableCollection<LocalizedOption<SchoolCalendarEventType>> EventTypeOptions { get; }

    public string CurrentMonthLabel
    {
        get
        {
            CultureInfo culture = LocalizationService.Instance.CurrentCulture;
            string formatted = _currentMonthFirstDay.ToDateTime(TimeOnly.MinValue).ToString("MMMM yyyy", culture);
            return culture.TextInfo.ToTitleCase(formatted);
        }
    }

    public string SelectedDateDisplay
    {
        get
        {
            CultureInfo culture = LocalizationService.Instance.CurrentCulture;
            string formatted = _selectedDate.ToDateTime(TimeOnly.MinValue).ToString("dddd, dd/MM/yyyy", culture);
            return char.ToUpper(formatted[0], culture) + formatted[1..];
        }
    }

    public bool HasNoPeriodEvents => PeriodEvents.Count == 0;
    public bool HasNoSelectedDateLessons => SelectedDateLessons.Count == 0;

    public override async Task LoadAsync()
    {
        AcademicYear? year = _appStateService.SelectedAcademicYear;
        if (year is null)
        {
            IReadOnlyList<AcademicYear> allYears = await _academicYearRepository.GetAllAsync();
            year = allYears.FirstOrDefault();
            _appStateService.SelectedAcademicYear = year;
        }

        _activeYear = year;
        if (year is null)
        {
            _classrooms.Clear();
            _allSessions.Clear();
            _allEvents.Clear();
            RebuildCalendarGrid();
            RebuildPeriodEvents();
            RefreshSelectedDateDetails();
            return;
        }

        if (_currentMonthFirstDay.Year != year.Year)
        {
            int month = year.Year == DateTime.Today.Year ? DateTime.Today.Month : 3;
            _currentMonthFirstDay = new DateOnly(year.Year, month, 1);
            _selectedDate = new DateOnly(year.Year, month, 1);
            NewEventDate = new DateTimeOffset(_selectedDate.ToDateTime(TimeOnly.MinValue));
        }

        IReadOnlyList<Classroom> classrooms = await _classroomRepository.GetByAcademicYearAsync(year.Id);
        _classrooms = classrooms.ToList();
        _allSessions = _classrooms.SelectMany(c => c.AttendanceSessions).ToList();
        _allEvents = (await _calendarRepository.GetByAcademicYearAsync(year.Id)).ToList();

        RebuildTermFilterPills(year.GradingSystem);
        UpdateSchoolDaysSummary();
        RebuildCalendarGrid();
        RebuildPeriodEvents();
        RefreshSelectedDateDetails();
    }

    public override Task TrySaveAsync() => AddEventAsync();

    public override void RefreshThemeVisuals()
    {
        RebuildCalendarGrid();
        RebuildPeriodEvents();
        RefreshSelectedDateDetails();
    }

    [RelayCommand]
    private void PreviousMonth()
    {
        _currentMonthFirstDay = _currentMonthFirstDay.AddMonths(-1);
        OnPropertyChanged(nameof(CurrentMonthLabel));
        UpdateSchoolDaysSummary();
        RebuildCalendarGrid();
    }

    [RelayCommand]
    private void NextMonth()
    {
        _currentMonthFirstDay = _currentMonthFirstDay.AddMonths(1);
        OnPropertyChanged(nameof(CurrentMonthLabel));
        UpdateSchoolDaysSummary();
        RebuildCalendarGrid();
    }

    [RelayCommand]
    private void GoToToday()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        _currentMonthFirstDay = new DateOnly(today.Year, today.Month, 1);
        SelectDate(today);
        OnPropertyChanged(nameof(CurrentMonthLabel));
        UpdateSchoolDaysSummary();
        RebuildCalendarGrid();
    }

    [RelayCommand]
    private async Task ImportBrazilianHolidaysAsync()
    {
        if (_activeYear is null)
        {
            return;
        }

        IReadOnlyList<SchoolCalendarEvent> standardHolidays = _schoolDaysCalculator.CreateBrazilianNationalHolidays(
            _activeYear.Id,
            _activeYear.Year,
            _activeYear.GradingSystem);

        var existingKeys = _allEvents
            .Select(e => (e.Date, e.Title.Trim().ToUpperInvariant()))
            .ToHashSet();

        List<SchoolCalendarEvent> toInsert = standardHolidays
            .Where(h => !existingKeys.Contains((h.Date, h.Title.Trim().ToUpperInvariant())))
            .ToList();

        if (toInsert.Count == 0)
        {
            _toastService.ShowInfo(LocalizationService.Instance.GetString("NoNewHolidaysMessage"));
            return;
        }

        await _calendarRepository.AddRangeAsync(toInsert);
        _toastService.ShowSuccess(LocalizationService.Instance.GetString("HolidaysImportedMessage"));
        await LoadAsync();
    }

    [RelayCommand]
    private async Task AddEventAsync()
    {
        if (_activeYear is null || string.IsNullOrWhiteSpace(NewEventTitle))
        {
            return;
        }

        DateOnly date = DateOnly.FromDateTime((NewEventDate ?? DateTimeOffset.Now).DateTime);
        SchoolCalendarEventType type = SelectedEventTypeOption?.Value ?? SchoolCalendarEventType.SchoolSaturday;
        int termNumber = _selectedTermFilter ?? _activeYear.GradingSystem.GetDefaultTermForDate(date);

        var calendarEvent = new SchoolCalendarEvent
        {
            Id = Guid.NewGuid(),
            AcademicYearId = _activeYear.Id,
            Date = date,
            Type = type,
            Title = NewEventTitle.Trim(),
            TermNumber = termNumber,
            Notes = string.IsNullOrWhiteSpace(NewEventNotes) ? null : NewEventNotes.Trim()
        };

        await _calendarRepository.AddAsync(calendarEvent);
        NewEventTitle = string.Empty;
        NewEventNotes = string.Empty;
        _selectedDate = date;
        _currentMonthFirstDay = new DateOnly(date.Year, date.Month, 1);
        OnPropertyChanged(nameof(CurrentMonthLabel));

        _toastService.ShowSuccess(LocalizationService.Instance.GetString("SavedSuccessMessage"));
        await LoadAsync();
    }

    private async Task DeleteEventAsync(Guid eventId)
    {
        await _calendarRepository.DeleteAsync(eventId);
        _toastService.ShowInfo(LocalizationService.Instance.GetString("SavedSuccessMessage"));
        await LoadAsync();
    }

    private void SelectDate(DateOnly date)
    {
        _selectedDate = date;
        NewEventDate = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue));

        foreach (CalendarDayCellViewModel cell in DayCells)
        {
            cell.IsSelected = cell.Date == date;
        }

        OnPropertyChanged(nameof(SelectedDateDisplay));
        RefreshSelectedDateDetails();
    }

    private void SelectTermFilter(int? termNumber)
    {
        _selectedTermFilter = termNumber;
        foreach (TermPillViewModel pill in TermFilterPills)
        {
            pill.IsSelected = pill.TermNumber == termNumber;
        }

        UpdateSchoolDaysSummary();
        RebuildPeriodEvents();
    }

    private void RebuildTermFilterPills(GradingSystem gradingSystem)
    {
        foreach (TermPillViewModel pill in TermFilterPills)
        {
            pill.Dispose();
        }
        TermFilterPills.Clear();

        TermFilterPills.Add(new TermPillViewModel(
            null,
            loc => loc.GetString("AllYearPeriodOption"),
            _selectedTermFilter is null,
            SelectTermFilter));

        int count = gradingSystem.GetTermCount();
        for (int i = 1; i <= count; i++)
        {
            int termIndex = i;
            TermFilterPills.Add(new TermPillViewModel(
                termIndex,
                loc => loc.GetTermName(gradingSystem, termIndex),
                _selectedTermFilter == termIndex,
                SelectTermFilter));
        }
    }

    private void UpdateSchoolDaysSummary()
    {
        if (_activeYear is null)
        {
            RecordedSchoolDays = 0;
            TargetSchoolDays = 200;
            RemainingSchoolDays = 200;
            SchoolDaysProgressValue = 0;
            SchoolDaysProgressText = "0 / 200";
            PlannedMonthDays = 0;
            SchoolSaturdaysCount = 0;
            NonSchoolDaysCount = 0;
            return;
        }

        SchoolDaysSummary summary = _schoolDaysCalculator.CalculateRecordedProgress(
            _allSessions,
            _allEvents,
            _activeYear.TargetSchoolDays,
            _activeYear.GradingSystem,
            _selectedTermFilter);

        RecordedSchoolDays = summary.RecordedDays;
        TargetSchoolDays = summary.TargetDays;
        RemainingSchoolDays = summary.RemainingDays;
        SchoolDaysProgressValue = (double)summary.ProgressPercentage;
        SchoolDaysProgressText = $"{summary.RecordedDays} / {summary.TargetDays}";
        SchoolSaturdaysCount = summary.SchoolSaturdaysCount;
        NonSchoolDaysCount = summary.NonSchoolDaysCount;

        int daysInMonth = DateTime.DaysInMonth(_currentMonthFirstDay.Year, _currentMonthFirstDay.Month);
        var monthEnd = new DateOnly(_currentMonthFirstDay.Year, _currentMonthFirstDay.Month, daysInMonth);
        PlannedMonthDays = _schoolDaysCalculator.CountCalendarSchoolDays(_currentMonthFirstDay, monthEnd, _allEvents);
    }

    private void RebuildCalendarGrid()
    {
        DayCells.Clear();
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);

        int offsetFromSunday = (int)_currentMonthFirstDay.DayOfWeek;
        DateOnly gridStart = _currentMonthFirstDay.AddDays(-offsetFromSunday);

        var lessonsByDate = _allSessions
            .GroupBy(s => s.Date)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.LessonCount));

        var eventsByDate = _allEvents
            .GroupBy(e => e.Date)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<SchoolCalendarEvent>)g.ToList());

        for (int i = 0; i < 42; i++)
        {
            DateOnly cellDate = gridStart.AddDays(i);
            bool isCurrentMonth = cellDate.Month == _currentMonthFirstDay.Month && cellDate.Year == _currentMonthFirstDay.Year;
            int lessonCount = lessonsByDate.TryGetValue(cellDate, out int count) ? count : 0;
            IReadOnlyList<SchoolCalendarEvent> dateEvents = eventsByDate.TryGetValue(cellDate, out IReadOnlyList<SchoolCalendarEvent>? evs)
                ? evs
                : Array.Empty<SchoolCalendarEvent>();

            DayCells.Add(new CalendarDayCellViewModel(
                cellDate,
                isCurrentMonth,
                cellDate == today,
                cellDate == _selectedDate,
                lessonCount,
                dateEvents,
                SelectDate));
        }
    }

    private void RebuildPeriodEvents()
    {
        foreach (CalendarEventItemViewModel existing in PeriodEvents)
        {
            existing.Dispose();
        }
        PeriodEvents.Clear();

        GradingSystem grading = _activeYear?.GradingSystem ?? GradingSystem.Semester;
        IEnumerable<SchoolCalendarEvent> filtered = _selectedTermFilter.HasValue
            ? _allEvents.Where(e => e.TermNumber == _selectedTermFilter.Value)
            : _allEvents;

        foreach (SchoolCalendarEvent ev in filtered.OrderBy(e => e.Date))
        {
            PeriodEvents.Add(new CalendarEventItemViewModel(ev, grading, DeleteEventAsync));
        }

        OnPropertyChanged(nameof(HasNoPeriodEvents));
    }

    private void RefreshSelectedDateDetails()
    {
        SelectedDateLessons.Clear();

        var classroomLookup = _classrooms.ToDictionary(c => c.Id);
        foreach (AttendanceSession session in _allSessions.Where(s => s.Date == _selectedDate))
        {
            string classroomName = classroomLookup.TryGetValue(session.ClassroomId, out Classroom? c)
                ? c.Name
                : "—";
            string topic = string.IsNullOrWhiteSpace(session.TopicCovered) ? "—" : session.TopicCovered;
            SelectedDateLessons.Add($"{classroomName} ({session.LessonCount}×) — {topic}");
        }

        List<SchoolCalendarEvent> eventsOnDate = _allEvents.Where(e => e.Date == _selectedDate).ToList();
        if (eventsOnDate.Count > 0)
        {
            SchoolCalendarEvent primary = eventsOnDate[0];
            string typeName = LocalizationService.Instance.GetCalendarEventTypeName(primary.Type);
            SelectedDateStatusText = $"{typeName}: {primary.Title}";
            HasSelectedDateStatus = true;
        }
        else if (_selectedDate.DayOfWeek == DayOfWeek.Saturday || _selectedDate.DayOfWeek == DayOfWeek.Sunday)
        {
            SelectedDateStatusText = LocalizationService.Instance.GetString("WeekendLabel");
            HasSelectedDateStatus = true;
        }
        else
        {
            SelectedDateStatusText = string.Empty;
            HasSelectedDateStatus = false;
        }

        OnPropertyChanged(nameof(HasNoSelectedDateLessons));
    }
}
