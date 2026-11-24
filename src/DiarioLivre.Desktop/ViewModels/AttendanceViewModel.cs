using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiarioLivre.Core.Entities;
using DiarioLivre.Core.Enums;
using DiarioLivre.Core.Interfaces;
using DiarioLivre.Core.Models;
using DiarioLivre.Desktop.Localization;
using DiarioLivre.Desktop.Services;

namespace DiarioLivre.Desktop.ViewModels;

public sealed partial class SessionChipViewModel : ObservableObject
{
    private readonly Action<DateOnly> _selectAction;

    [ObservableProperty]
    private bool _isSelected;

    public SessionChipViewModel(DateOnly date, int lessonCount, bool isSelected, Action<DateOnly> selectAction)
    {
        Date = date;
        LessonCount = lessonCount;
        _isSelected = isSelected;
        _selectAction = selectAction;
        Label = $"{date:dd/MM} ({lessonCount}×)";
    }

    public DateOnly Date { get; }
    public int LessonCount { get; }
    public string Label { get; }

    [RelayCommand]
    private void Select() => _selectAction(Date);
}

public sealed partial class LessonLogItemViewModel : ObservableObject, IDisposable
{
    private readonly GradingSystem _gradingSystem;
    private readonly Action<DateOnly> _openAction;
    private readonly Func<Guid, Task> _deleteAction;

    public LessonLogItemViewModel(
        AttendanceSession session,
        GradingSystem gradingSystem,
        Action<DateOnly> openAction,
        Func<Guid, Task> deleteAction)
    {
        SessionId = session.Id;
        Date = session.Date;
        TermNumber = Math.Max(1, session.TermNumber);
        _gradingSystem = gradingSystem;
        LessonCount = session.LessonCount;
        TopicCovered = string.IsNullOrWhiteSpace(session.TopicCovered) ? "—" : session.TopicCovered;
        DailyActivity = string.IsNullOrWhiteSpace(session.DailyActivity) ? null : session.DailyActivity.Trim();
        Homework = string.IsNullOrWhiteSpace(session.Homework) ? null : session.Homework.Trim();
        LessonNotes = string.IsNullOrWhiteSpace(session.LessonNotes) ? null : session.LessonNotes.Trim();
        _openAction = openAction;
        _deleteAction = deleteAction;

        int present = session.Entries.Count(e => e.Status == AttendanceStatus.Present);
        int absent = session.Entries.Count(e => e.Status == AttendanceStatus.Absent);
        int justified = session.Entries.Count(e => e.Status == AttendanceStatus.Justified);

        DateDisplay = session.Date.ToString("dd/MM/yyyy");
        LessonCountDisplay = $"{session.LessonCount}×";
        CountsDisplay = $"P: {present}  •  F: {absent}  •  FJ: {justified}";

        LocalizationService.Instance.CultureChanged += OnCultureChanged;
    }

    public Guid SessionId { get; }
    public LocalizationService Loc => LocalizationService.Instance;
    public DateOnly Date { get; }
    public int TermNumber { get; }
    public int LessonCount { get; }
    public string DateDisplay { get; }
    public string LessonCountDisplay { get; }
    public string TermBadge => LocalizationService.Instance.GetTermShortName(_gradingSystem, TermNumber);
    public string TopicCovered { get; }
    public string? DailyActivity { get; }
    public bool HasDailyActivity => !string.IsNullOrWhiteSpace(DailyActivity);
    public string? Homework { get; }
    public bool HasHomework => !string.IsNullOrWhiteSpace(Homework);
    public string? LessonNotes { get; }
    public bool HasLessonNotes => !string.IsNullOrWhiteSpace(LessonNotes);
    public string CountsDisplay { get; }

    [RelayCommand]
    private void Open() => _openAction(Date);

    [RelayCommand]
    private Task DeleteAsync() => _deleteAction(SessionId);

    public void Dispose()
    {
        LocalizationService.Instance.CultureChanged -= OnCultureChanged;
    }

    private void OnCultureChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(TermBadge));
    }
}

public sealed partial class StudentAttendanceRowViewModel : ObservableObject, IDisposable
{
    private readonly Action<StudentAttendanceRowViewModel> _onStatusChanged;

    [ObservableProperty]
    private AttendanceStatus _currentStatus;

    [ObservableProperty]
    private decimal _absencePercentage;

    [ObservableProperty]
    private decimal _attendancePercentage;

    [ObservableProperty]
    private AttendanceAlertStatus _alertStatus;

    public StudentAttendanceRowViewModel(
        Student student,
        string? photoPath,
        AttendanceStatus initialStatus,
        Action<StudentAttendanceRowViewModel> onStatusChanged)
    {
        StudentId = student.Id;
        RollNumber = student.RollNumber;
        FullName = student.FullName;
        PhotoPath = photoPath;
        HasSpecialNeeds = student.HasSpecialNeeds;
        _currentStatus = initialStatus;
        _onStatusChanged = onStatusChanged;

        LocalizationService.Instance.CultureChanged += OnCultureChanged;
    }

    public Guid StudentId { get; }
    public LocalizationService Loc => LocalizationService.Instance;
    public int RollNumber { get; }
    public string FullName { get; }
    public string? PhotoPath { get; }
    public bool HasSpecialNeeds { get; }
    public string RollDisplay => $"{RollNumber:D2}";

    public string AbsencePercentageText => $"{AbsencePercentage:F1}%";
    public string AttendancePercentageText => $"{AttendancePercentage:F1}%";
    public string AlertLabel => LocalizationService.Instance.GetAlertStatusName(AlertStatus);

    public void UpdateMetrics(AttendanceSummary summary)
    {
        AbsencePercentage = summary.AbsencePercentage;
        AttendancePercentage = summary.AttendancePercentage;
        AlertStatus = summary.AlertStatus;
        OnPropertyChanged(nameof(AbsencePercentageText));
        OnPropertyChanged(nameof(AttendancePercentageText));
        OnPropertyChanged(nameof(AlertLabel));
    }

    partial void OnCurrentStatusChanged(AttendanceStatus value)
    {
        _onStatusChanged(this);
    }

    [RelayCommand]
    private void SetPresent() => CurrentStatus = AttendanceStatus.Present;

    [RelayCommand]
    private void SetAbsent() => CurrentStatus = AttendanceStatus.Absent;

    [RelayCommand]
    private void SetJustified() => CurrentStatus = AttendanceStatus.Justified;

    public void RefreshTheme()
    {
        OnPropertyChanged(nameof(CurrentStatus));
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

public sealed partial class AttendanceViewModel : ViewModelBase
{
    private readonly IAcademicYearRepository _academicYearRepository;
    private readonly IClassroomRepository _classroomRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly ISchoolCalendarRepository _calendarRepository;
    private readonly IAttendanceCalculator _attendanceCalculator;
    private readonly ISchoolDaysCalculator _schoolDaysCalculator;
    private readonly IAvatarService _avatarService;
    private readonly IReportGenerator _reportGenerator;
    private readonly AppStateService _appStateService;
    private readonly ToastService _toastService;

    private AcademicYear? _activeYear;
    private List<AttendanceSession> _historicalSessions = new();
    private List<SchoolCalendarEvent> _calendarEvents = new();
    private readonly List<StudentAttendanceRowViewModel> _allRows = new();
    private Guid? _currentSessionId;
    private bool _isLoading;

    [ObservableProperty]
    private Classroom? _selectedClassroom;

    [ObservableProperty]
    private Subject? _selectedSubject;

    [ObservableProperty]
    private DateTimeOffset? _selectedDate = DateTimeOffset.Now.Date;

    [ObservableProperty]
    private decimal? _lessonCountValue = 1m;

    [ObservableProperty]
    private string? _topicCovered;

    [ObservableProperty]
    private string? _dailyActivity;

    [ObservableProperty]
    private string? _homework;

    [ObservableProperty]
    private string? _lessonNotes;

    [ObservableProperty]
    private bool _isLessonDetailsExpanded;

    [ObservableProperty]
    private int? _selectedTermFilter = 1;

    [ObservableProperty]
    private int _periodLessonsCount;

    [ObservableProperty]
    private string _schoolDaysCountText = "0 / 200";

    [ObservableProperty]
    private string? _selectedDateCalendarBadge;

    [ObservableProperty]
    private bool _isSelectedDateNonSchoolDay;

    [ObservableProperty]
    private bool _isSelectedDateSpecialSchoolDay;

    [ObservableProperty]
    private string _studentSearchQuery = string.Empty;

    [ObservableProperty]
    private bool _isLessonLogVisible;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _hasUnsavedChanges;

    [ObservableProperty]
    private int _presentCount;

    [ObservableProperty]
    private int _absentCount;

    [ObservableProperty]
    private int _justifiedCount;

    [ObservableProperty]
    private int _alertCount;

    public AttendanceViewModel(
        IAcademicYearRepository academicYearRepository,
        IClassroomRepository classroomRepository,
        IAttendanceRepository attendanceRepository,
        ISchoolCalendarRepository calendarRepository,
        IAttendanceCalculator attendanceCalculator,
        ISchoolDaysCalculator schoolDaysCalculator,
        IAvatarService avatarService,
        IReportGenerator reportGenerator,
        AppStateService appStateService,
        ToastService toastService)
    {
        _academicYearRepository = academicYearRepository;
        _classroomRepository = classroomRepository;
        _attendanceRepository = attendanceRepository;
        _calendarRepository = calendarRepository;
        _attendanceCalculator = attendanceCalculator;
        _schoolDaysCalculator = schoolDaysCalculator;
        _avatarService = avatarService;
        _reportGenerator = reportGenerator;
        _appStateService = appStateService;
        _toastService = toastService;

        LocalizationService.Instance.CultureChanged += (_, _) =>
        {
            UpdateSelectedDateCalendarBadge();
        };
    }

    public ObservableCollection<Classroom> Classrooms { get; } = new();
    public ObservableCollection<Subject> Subjects { get; } = new();
    public ObservableCollection<TermPillViewModel> TermFilterPills { get; } = new();
    public ObservableCollection<SessionChipViewModel> RecentSessions { get; } = new();
    public ObservableCollection<LessonLogItemViewModel> LessonLogItems { get; } = new();
    public ObservableCollection<StudentAttendanceRowViewModel> Rows { get; } = new();

    public bool IsMultiSubjectClassroom => SelectedClassroom?.IsMultiSubject ?? false;
    public bool IsSubjectSelectionVisible => !IsMultiSubjectClassroom;
    public bool HasRecentSessions => RecentSessions.Count > 0;
    public bool HasNoLessonLog => LessonLogItems.Count == 0;
    public bool HasHomeworkOrNotes => !string.IsNullOrWhiteSpace(Homework) || !string.IsNullOrWhiteSpace(LessonNotes);
    public bool CanExport => _allRows.Count > 0;

    private int EffectiveLessonCount => Math.Clamp((int)(LessonCountValue ?? 1m), 1, 6);

    public override async Task LoadAsync()
    {
        _isLoading = true;
        StatusMessage = null;
        HasUnsavedChanges = false;
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
            RebuildTermFilterPills(year?.GradingSystem ?? GradingSystem.Semester);

            Classrooms.Clear();
            if (year is null)
            {
                _calendarEvents.Clear();
                return;
            }

            _calendarEvents = (await _calendarRepository.GetByAcademicYearAsync(year.Id)).ToList();
            IReadOnlyList<Classroom> classrooms = await _classroomRepository.GetByAcademicYearAsync(year.Id);
            foreach (Classroom classroom in classrooms)
            {
                Classrooms.Add(classroom);
            }

            Classroom? target = _appStateService.SelectedClassroom is not null
                ? Classrooms.FirstOrDefault(c => c.Id == _appStateService.SelectedClassroom.Id)
                : null;

            AttendancePrefillRequest? prefill = _appStateService.PendingAttendancePrefill;
            _appStateService.PendingAttendancePrefill = null;

            if (prefill is not null)
            {
                Classroom? prefillClassroom = Classrooms.FirstOrDefault(c => c.Id == prefill.ClassroomId);
                if (prefillClassroom is not null)
                {
                    target = prefillClassroom;
                }

                GradingSystem grading = year.GradingSystem;
                int prefillTerm = grading.GetDefaultTermForDate(prefill.Date);
                SelectedTermFilter = prefillTerm;
                _appStateService.SelectedTermNumber = prefillTerm;
                foreach (TermPillViewModel pill in TermFilterPills)
                {
                    pill.IsSelected = pill.TermNumber == prefillTerm;
                }
            }

            SelectedClassroom = target ?? Classrooms.FirstOrDefault();

            _isLoading = false;
            await LoadClassroomAttendanceContextAsync(prefill);
            return;
        }
        finally
        {
            _isLoading = false;
        }
    }

    public override Task TrySaveAsync() => SaveAttendanceCommand.ExecuteAsync(null);

    public override void RefreshThemeVisuals()
    {
        foreach (StudentAttendanceRowViewModel row in _allRows)
        {
            row.RefreshTheme();
        }
    }

    partial void OnSelectedClassroomChanged(Classroom? value)
    {
        _appStateService.SelectedClassroom = value;
        OnPropertyChanged(nameof(IsMultiSubjectClassroom));
        OnPropertyChanged(nameof(IsSubjectSelectionVisible));

        if (!_isLoading)
        {
            _ = LoadClassroomAttendanceContextAsync();
        }
    }

    partial void OnSelectedSubjectChanged(Subject? value)
    {
        if (!_isLoading)
        {
            _ = LoadSessionsForSelectionAsync();
        }
    }

    partial void OnSelectedDateChanged(DateTimeOffset? value)
    {
        if (!_isLoading)
        {
            UpdateSessionChipSelection();
            _ = ApplySessionForSelectedDateAsync();
        }
    }

    partial void OnLessonCountValueChanged(decimal? value)
    {
        if (!_isLoading)
        {
            HasUnsavedChanges = true;
            StatusMessage = null;
            RecalculateAllStudentMetrics();
        }
    }

    partial void OnTopicCoveredChanged(string? value)
    {
        if (!_isLoading)
        {
            HasUnsavedChanges = true;
            StatusMessage = null;
        }
    }

    partial void OnDailyActivityChanged(string? value)
    {
        if (!_isLoading)
        {
            HasUnsavedChanges = true;
            StatusMessage = null;
        }
    }

    partial void OnHomeworkChanged(string? value)
    {
        OnPropertyChanged(nameof(HasHomeworkOrNotes));
        if (!_isLoading)
        {
            HasUnsavedChanges = true;
            StatusMessage = null;
        }
    }

    partial void OnLessonNotesChanged(string? value)
    {
        OnPropertyChanged(nameof(HasHomeworkOrNotes));
        if (!_isLoading)
        {
            HasUnsavedChanges = true;
            StatusMessage = null;
        }
    }

    partial void OnStudentSearchQueryChanged(string value)
    {
        ApplyRowFilter();
    }

    [RelayCommand]
    private void SetToday()
    {
        SelectedDate = DateTimeOffset.Now.Date;
    }

    [RelayCommand]
    private void ToggleLessonLog()
    {
        IsLessonLogVisible = !IsLessonLogVisible;
    }

    [RelayCommand]
    private void ToggleLessonDetails()
    {
        IsLessonDetailsExpanded = !IsLessonDetailsExpanded;
    }

    [RelayCommand]
    private void AppendActivityTag(string? resourceKey)
    {
        if (string.IsNullOrWhiteSpace(resourceKey))
        {
            return;
        }

        string tag = LocalizationService.Instance.GetString(resourceKey);
        if (string.IsNullOrWhiteSpace(DailyActivity))
        {
            DailyActivity = tag;
            return;
        }

        if (!DailyActivity.Contains(tag, StringComparison.OrdinalIgnoreCase))
        {
            DailyActivity = $"{DailyActivity.TrimEnd(' ', ',', '•', ';')}, {tag}";
        }
    }

    [RelayCommand]
    private void MarkAllPresent()
    {
        foreach (StudentAttendanceRowViewModel row in Rows)
        {
            row.CurrentStatus = AttendanceStatus.Present;
        }
    }

    [RelayCommand]
    private void InvertAttendance()
    {
        foreach (StudentAttendanceRowViewModel row in Rows)
        {
            if (row.CurrentStatus == AttendanceStatus.Present)
            {
                row.CurrentStatus = AttendanceStatus.Absent;
            }
            else if (row.CurrentStatus == AttendanceStatus.Absent)
            {
                row.CurrentStatus = AttendanceStatus.Present;
            }
        }
    }

    [RelayCommand]
    private async Task SaveAttendanceAsync()
    {
        if (SelectedClassroom is null || _allRows.Count == 0)
        {
            return;
        }

        DateOnly date = DateOnly.FromDateTime((SelectedDate ?? DateTimeOffset.Now).DateTime);
        Guid? effectiveSubjectId = SelectedClassroom.IsMultiSubject ? null : SelectedSubject?.Id;
        GradingSystem grading = _activeYear?.GradingSystem ?? GradingSystem.Semester;
        AttendanceSession? existingForDate = _historicalSessions.FirstOrDefault(s => s.Date == date);
        int resolvedTerm = SelectedTermFilter
            ?? existingForDate?.TermNumber
            ?? grading.GetDefaultTermForDate(date);

        var session = new AttendanceSession
        {
            Id = _currentSessionId ?? Guid.NewGuid(),
            ClassroomId = SelectedClassroom.Id,
            SubjectId = effectiveSubjectId,
            Date = date,
            TermNumber = resolvedTerm,
            LessonCount = EffectiveLessonCount,
            TopicCovered = TrimOrNull(TopicCovered),
            DailyActivity = TrimOrNull(DailyActivity),
            Homework = TrimOrNull(Homework),
            LessonNotes = TrimOrNull(LessonNotes),
            Entries = _allRows.Select(r => new AttendanceEntry
            {
                Id = Guid.NewGuid(),
                StudentId = r.StudentId,
                Status = r.CurrentStatus
            }).ToList()
        };

        await _attendanceRepository.SaveSessionAsync(session);
        HasUnsavedChanges = false;
        string savedMsg = LocalizationService.Instance.GetString("SavedSuccessMessage");
        StatusMessage = savedMsg;
        _toastService.ShowSuccess(savedMsg);

        await LoadSessionsForSelectionAsync(preserveCurrentDate: true);
    }

    public async Task ExportAttendancePdfAsync(string outputPath)
    {
        if (SelectedClassroom is null || _allRows.Count == 0)
        {
            return;
        }

        LocalizationService loc = LocalizationService.Instance;
        GradingSystem grading = _activeYear?.GradingSystem ?? GradingSystem.Semester;
        string subjectDisplay = SelectedClassroom.IsMultiSubject
            ? loc.GetString("PolyvalentBadge")
            : SelectedSubject?.Name ?? "—";
        string periodDisplay = SelectedTermFilter.HasValue
            ? loc.GetTermName(grading, SelectedTermFilter.Value)
            : loc.GetString("AllYearPeriodOption");

        List<AttendanceSession> filteredSessions = GetFilteredHistoricalSessions();
        decimal minAttendance = _activeYear?.MinAttendancePercentage ?? 75.0m;
        var studentRows = new List<ClassAttendanceStudentRow>();

        foreach (StudentAttendanceRowViewModel row in _allRows)
        {
            var records = new List<AttendanceRecord>();
            foreach (AttendanceSession session in filteredSessions)
            {
                AttendanceEntry? entry = session.Entries.FirstOrDefault(e => e.StudentId == row.StudentId);
                if (entry is not null)
                {
                    records.Add(new AttendanceRecord(session.LessonCount, entry.Status));
                }
            }

            AttendanceSummary summary = _attendanceCalculator.Calculate(records, minAttendance);
            studentRows.Add(new ClassAttendanceStudentRow(
                row.RollNumber,
                row.FullName,
                summary.TotalLessons,
                summary.PresentLessons,
                summary.UnexcusedAbsences,
                summary.JustifiedAbsences,
                summary.AttendancePercentage,
                summary.AbsencePercentage,
                summary.AlertStatus,
                loc.GetAlertStatusName(summary.AlertStatus)));
        }

        var sessionItems = filteredSessions
            .OrderByDescending(s => s.Date)
            .Select(s => new ClassAttendanceSessionItem(
                s.Date,
                s.LessonCount,
                s.TopicCovered,
                s.Entries.Count(e => e.Status == AttendanceStatus.Present),
                s.Entries.Count(e => e.Status == AttendanceStatus.Absent),
                s.Entries.Count(e => e.Status == AttendanceStatus.Justified),
                s.TermNumber,
                s.DailyActivity,
                s.Homework))
            .ToList();

        var labels = new ClassAttendanceReportLabels(
            ReportTitle: loc.GetString("ClassAttendanceReportTitle"),
            StudentsSummaryTitle: loc.GetString("StudentsSummaryReportTitle"),
            LessonLogTitle: loc.GetString("LessonLogReportTitle"),
            RollColumn: loc.GetString("RollColumnHeader"),
            StudentColumn: loc.GetString("StudentColumnHeader"),
            TotalLessonsColumn: loc.GetString("LessonsCountShort"),
            PresentColumn: loc.GetString("PresentLessonsLabel"),
            UnexcusedColumn: loc.GetString("UnexcusedAbsencesLabel"),
            JustifiedColumn: loc.GetString("JustifiedAbsencesLabel"),
            AttendanceRateColumn: loc.GetString("AttendanceRateLabel"),
            StatusColumn: loc.GetString("StatusColumnHeader"),
            DateColumn: loc.GetString("DateLabel"),
            TopicColumn: loc.GetString("TopicCoveredLabel"),
            NoSessionsText: loc.GetString("LessonLogEmptyText"),
            GeneratedAtLabel: loc.GetString("PdfGeneratedAtLabel"),
            DailyActivityColumn: loc.GetString("DailyActivityLabel"),
            HomeworkColumn: loc.GetString("HomeworkLabel"),
            PeriodLabel: loc.GetString("ActivePeriodLabel"));

        var reportData = new ClassAttendanceReportData(
            SchoolName: _activeYear?.SchoolName ?? "Diário Livre",
            Year: _activeYear?.Year ?? DateTime.Now.Year,
            ClassroomName: SelectedClassroom.Name,
            SubjectDisplay: subjectDisplay,
            Students: studentRows,
            Sessions: sessionItems,
            Labels: labels,
            PeriodDisplay: periodDisplay);

        await _reportGenerator.GenerateClassAttendancePdfAsync(reportData, outputPath);
        _toastService.ShowSuccess(loc.GetString("PdfExportedMessage"));
    }

    public string GetSuggestedPdfFileName()
    {
        string classroom = SelectedClassroom?.Name ?? "turma";
        GradingSystem grading = _activeYear?.GradingSystem ?? GradingSystem.Semester;
        string periodSuffix = SelectedTermFilter.HasValue
            ? "_" + LocalizationService.Instance.GetTermShortName(grading, SelectedTermFilter.Value)
            : string.Empty;
        string raw = $"Frequencia_{classroom}{periodSuffix}".Replace(' ', '_');
        string safe = string.Join("_", raw.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        return $"{safe}.pdf";
    }

    private async Task LoadClassroomAttendanceContextAsync(AttendancePrefillRequest? prefill = null)
    {
        _isLoading = true;
        try
        {
            Subjects.Clear();
            if (SelectedClassroom is not null && !SelectedClassroom.IsMultiSubject)
            {
                Classroom? details = await _classroomRepository.GetWithDetailsAsync(SelectedClassroom.Id);
                if (details is not null)
                {
                    foreach (Subject s in details.Subjects)
                    {
                        Subjects.Add(s);
                    }
                }
            }

            Subject? preferredSubject = prefill?.SubjectId is Guid targetSubjectId
                ? Subjects.FirstOrDefault(s => s.Id == targetSubjectId)
                : null;

            SelectedSubject = preferredSubject ?? Subjects.FirstOrDefault();

            if (prefill is not null)
            {
                SelectedDate = new DateTimeOffset(prefill.Date.ToDateTime(TimeOnly.MinValue));
            }
        }
        finally
        {
            _isLoading = false;
        }

        await LoadSessionsForSelectionAsync(preserveCurrentDate: prefill is not null);

        if (prefill is not null)
        {
            bool hasExistingSession = _currentSessionId.HasValue;
            if (!hasExistingSession)
            {
                LessonCountValue = Math.Clamp(prefill.LessonCount, 1, 6);
            }

            if (!string.IsNullOrWhiteSpace(prefill.TopicCovered) && string.IsNullOrWhiteSpace(TopicCovered))
            {
                TopicCovered = prefill.TopicCovered;
            }

            if (!string.IsNullOrWhiteSpace(prefill.DailyActivity) && string.IsNullOrWhiteSpace(DailyActivity))
            {
                DailyActivity = prefill.DailyActivity;
            }

            if (!string.IsNullOrWhiteSpace(prefill.Homework) && string.IsNullOrWhiteSpace(Homework))
            {
                Homework = prefill.Homework;
                IsLessonDetailsExpanded = true;
            }
        }
    }

    private async Task LoadSessionsForSelectionAsync(bool preserveCurrentDate = false)
    {
        StatusMessage = preserveCurrentDate ? StatusMessage : null;
        ClearRows();
        RecentSessions.Clear();
        ClearLessonLogItems();
        _historicalSessions.Clear();
        PeriodLessonsCount = 0;

        if (SelectedClassroom is null)
        {
            OnPropertyChanged(nameof(HasRecentSessions));
            OnPropertyChanged(nameof(HasNoLessonLog));
            OnPropertyChanged(nameof(CanExport));
            UpdateSessionAnalytics();
            return;
        }

        Guid? effectiveSubjectId = SelectedClassroom.IsMultiSubject ? null : SelectedSubject?.Id;
        if (!SelectedClassroom.IsMultiSubject && effectiveSubjectId is null)
        {
            OnPropertyChanged(nameof(HasRecentSessions));
            OnPropertyChanged(nameof(HasNoLessonLog));
            OnPropertyChanged(nameof(CanExport));
            UpdateSessionAnalytics();
            return;
        }

        IReadOnlyList<AttendanceSession> sessions = await _attendanceRepository.GetSessionsAsync(
            SelectedClassroom.Id,
            effectiveSubjectId);

        _historicalSessions = SelectedClassroom.IsMultiSubject
            ? sessions.Where(s => s.SubjectId is null).ToList()
            : sessions.ToList();

        if (!preserveCurrentDate)
        {
            List<AttendanceSession> filtered = GetFilteredHistoricalSessions();
            if (filtered.Count > 0)
            {
                DateOnly latestDate = filtered.Max(s => s.Date);
                _isLoading = true;
                SelectedDate = new DateTimeOffset(latestDate.ToDateTime(TimeOnly.MinValue));
                _isLoading = false;
            }
            else if (SelectedTermFilter.HasValue)
            {
                DateOnly fallback = GetFallbackDateForTerm(SelectedTermFilter.Value);
                _isLoading = true;
                SelectedDate = new DateTimeOffset(fallback.ToDateTime(TimeOnly.MinValue));
                _isLoading = false;
            }
        }

        RebuildRecentSessionChipsAndLog();
        await ApplySessionForSelectedDateAsync();
    }

    private async Task ApplySessionForSelectedDateAsync()
    {
        if (SelectedClassroom is null)
        {
            return;
        }

        Classroom? details = await _classroomRepository.GetWithDetailsAsync(SelectedClassroom.Id);
        List<Student> students = details?.Students.OrderBy(s => s.RollNumber).ToList() ?? new List<Student>();

        DateOnly targetDate = DateOnly.FromDateTime((SelectedDate ?? DateTimeOffset.Now).DateTime);
        AttendanceSession? existing = _historicalSessions.FirstOrDefault(s => s.Date == targetDate);

        _isLoading = true;
        try
        {
            _currentSessionId = existing?.Id;
            LessonCountValue = existing?.LessonCount ?? 1;
            TopicCovered = existing?.TopicCovered ?? string.Empty;
            DailyActivity = existing?.DailyActivity ?? string.Empty;
            Homework = existing?.Homework ?? string.Empty;
            LessonNotes = existing?.LessonNotes ?? string.Empty;
            HasUnsavedChanges = false;
            OnPropertyChanged(nameof(HasHomeworkOrNotes));

            Dictionary<Guid, AttendanceStatus> statusByStudent = existing?.Entries
                .ToDictionary(e => e.StudentId, e => e.Status) ?? new Dictionary<Guid, AttendanceStatus>();

            ClearRows();
            foreach (Student student in students)
            {
                AttendanceStatus initialStatus = statusByStudent.TryGetValue(student.Id, out AttendanceStatus st)
                    ? st
                    : AttendanceStatus.Present;

                string? photoPath = _avatarService.GetAvatarFullPath(student.PhotoFileName);
                var row = new StudentAttendanceRowViewModel(student, photoPath, initialStatus, OnRowStatusEdited);
                _allRows.Add(row);
            }

            ApplyRowFilter();
            OnPropertyChanged(nameof(CanExport));
        }
        finally
        {
            _isLoading = false;
        }

        RecalculateAllStudentMetrics();
        UpdateSelectedDateCalendarBadge();
    }

    private void ApplyRowFilter()
    {
        Rows.Clear();
        string query = StudentSearchQuery.Trim();

        IEnumerable<StudentAttendanceRowViewModel> matches = string.IsNullOrEmpty(query)
            ? _allRows
            : _allRows.Where(r => r.FullName.Contains(query, StringComparison.OrdinalIgnoreCase));

        foreach (StudentAttendanceRowViewModel item in matches)
        {
            Rows.Add(item);
        }
    }

    private void OnRowStatusEdited(StudentAttendanceRowViewModel row)
    {
        if (!_isLoading)
        {
            HasUnsavedChanges = true;
            StatusMessage = null;
        }

        RecalculateRowMetrics(row);
        UpdateSessionAnalytics();
    }

    private void RebuildRecentSessionChipsAndLog()
    {
        RecentSessions.Clear();
        ClearLessonLogItems();
        DateOnly selected = DateOnly.FromDateTime((SelectedDate ?? DateTimeOffset.Now).DateTime);
        GradingSystem grading = _activeYear?.GradingSystem ?? GradingSystem.Semester;

        List<AttendanceSession> filtered = GetFilteredHistoricalSessions();
        PeriodLessonsCount = filtered.Sum(s => s.LessonCount);

        if (_activeYear is not null)
        {
            SchoolDaysSummary daysSummary = _schoolDaysCalculator.CalculateRecordedProgress(
                _historicalSessions,
                _calendarEvents,
                _activeYear.TargetSchoolDays,
                _activeYear.GradingSystem,
                SelectedTermFilter);
            SchoolDaysCountText = $"{daysSummary.RecordedDays} / {daysSummary.TargetDays}";
        }
        else
        {
            SchoolDaysCountText = "0 / 200";
        }

        List<AttendanceSession> ordered = filtered.OrderByDescending(s => s.Date).ToList();
        foreach (AttendanceSession session in ordered.Take(7))
        {
            RecentSessions.Add(new SessionChipViewModel(
                session.Date,
                session.LessonCount,
                session.Date == selected,
                SelectSessionDate));
        }

        foreach (AttendanceSession session in ordered)
        {
            LessonLogItems.Add(new LessonLogItemViewModel(session, grading, OpenSessionFromLog, DeleteSessionAsync));
        }

        OnPropertyChanged(nameof(HasRecentSessions));
        OnPropertyChanged(nameof(HasNoLessonLog));
    }

    private void RebuildTermFilterPills(GradingSystem gradingSystem)
    {
        foreach (TermPillViewModel pill in TermFilterPills)
        {
            pill.Dispose();
        }
        TermFilterPills.Clear();

        int count = gradingSystem.GetTermCount();
        int desired = SelectedTermFilter.HasValue
            ? Math.Clamp(SelectedTermFilter.Value, 1, count)
            : Math.Clamp(_appStateService.SelectedTermNumber, 1, count);

        if (SelectedTermFilter.HasValue || TermFilterPills.Count == 0)
        {
            SelectedTermFilter = desired;
            _appStateService.SelectedTermNumber = desired;
        }

        for (int i = 1; i <= count; i++)
        {
            int termIndex = i;
            TermFilterPills.Add(new TermPillViewModel(
                termIndex,
                loc => loc.GetTermName(gradingSystem, termIndex),
                SelectedTermFilter == termIndex,
                SelectTermFilter));
        }

        TermFilterPills.Add(new TermPillViewModel(
            null,
            loc => loc.GetString("AllYearPeriodOption"),
            SelectedTermFilter is null,
            SelectTermFilter));
    }

    private void SelectTermFilter(int? termNumber)
    {
        SelectedTermFilter = termNumber;
        if (termNumber.HasValue)
        {
            _appStateService.SelectedTermNumber = termNumber.Value;
        }

        foreach (TermPillViewModel pill in TermFilterPills)
        {
            pill.IsSelected = pill.TermNumber == termNumber;
        }

        List<AttendanceSession> filtered = GetFilteredHistoricalSessions();
        if (filtered.Count > 0)
        {
            DateOnly latest = filtered.Max(s => s.Date);
            _isLoading = true;
            SelectedDate = new DateTimeOffset(latest.ToDateTime(TimeOnly.MinValue));
            _isLoading = false;
        }
        else if (termNumber.HasValue)
        {
            DateOnly fallback = GetFallbackDateForTerm(termNumber.Value);
            _isLoading = true;
            SelectedDate = new DateTimeOffset(fallback.ToDateTime(TimeOnly.MinValue));
            _isLoading = false;
        }

        RebuildRecentSessionChipsAndLog();
        _ = ApplySessionForSelectedDateAsync();
    }

    private List<AttendanceSession> GetFilteredHistoricalSessions()
        => SelectedTermFilter.HasValue
            ? _historicalSessions.Where(s => s.TermNumber == SelectedTermFilter.Value).ToList()
            : _historicalSessions;

    private DateOnly GetFallbackDateForTerm(int termNumber)
    {
        GradingSystem grading = _activeYear?.GradingSystem ?? GradingSystem.Semester;
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        if (grading.GetDefaultTermForDate(today) == termNumber)
        {
            return today;
        }

        int year = _activeYear?.Year ?? today.Year;
        int startMonth = grading switch
        {
            GradingSystem.Semester => termNumber <= 1 ? 2 : 8,
            GradingSystem.Trimester => termNumber switch { 1 => 2, 2 => 5, _ => 9 },
            _ => termNumber switch { 1 => 2, 2 => 4, 3 => 8, _ => 10 }
        };

        return new DateOnly(year, startMonth, 1);
    }

    private void ClearLessonLogItems()
    {
        foreach (LessonLogItemViewModel existing in LessonLogItems)
        {
            existing.Dispose();
        }
        LessonLogItems.Clear();
    }

    private void UpdateSelectedDateCalendarBadge()
    {
        DateOnly date = DateOnly.FromDateTime((SelectedDate ?? DateTimeOffset.Now).DateTime);
        SchoolCalendarEvent? ev = _calendarEvents.FirstOrDefault(e => e.Date == date);
        if (ev is not null)
        {
            string typeName = LocalizationService.Instance.GetCalendarEventTypeName(ev.Type);
            SelectedDateCalendarBadge = $"{typeName}: {ev.Title}";
            IsSelectedDateNonSchoolDay = ev.Type.ExcludesRegularWeekday();
            IsSelectedDateSpecialSchoolDay = ev.Type.CountsAsSchoolDay();
            return;
        }

        if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
        {
            SelectedDateCalendarBadge = LocalizationService.Instance.GetString("WeekendLabel");
            IsSelectedDateNonSchoolDay = true;
            IsSelectedDateSpecialSchoolDay = false;
            return;
        }

        SelectedDateCalendarBadge = null;
        IsSelectedDateNonSchoolDay = false;
        IsSelectedDateSpecialSchoolDay = false;
    }

    private static string? TrimOrNull(string? text)
        => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private void UpdateSessionChipSelection()
    {
        DateOnly selected = DateOnly.FromDateTime((SelectedDate ?? DateTimeOffset.Now).DateTime);
        foreach (SessionChipViewModel chip in RecentSessions)
        {
            chip.IsSelected = chip.Date == selected;
        }
    }

    private void SelectSessionDate(DateOnly date)
    {
        SelectedDate = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue));
    }

    private void OpenSessionFromLog(DateOnly date)
    {
        SelectSessionDate(date);
        IsLessonLogVisible = false;
    }

    private async Task DeleteSessionAsync(Guid sessionId)
    {
        await _attendanceRepository.DeleteSessionAsync(sessionId);
        _toastService.ShowInfo(LocalizationService.Instance.GetString("SessionDeletedMessage"));
        await LoadSessionsForSelectionAsync(preserveCurrentDate: false);
    }

    private void ClearRows()
    {
        foreach (StudentAttendanceRowViewModel existing in _allRows)
        {
            existing.Dispose();
        }
        _allRows.Clear();
        Rows.Clear();
    }

    private void RecalculateAllStudentMetrics()
    {
        foreach (StudentAttendanceRowViewModel row in _allRows)
        {
            RecalculateRowMetrics(row);
        }

        UpdateSessionAnalytics();
    }

    private void RecalculateRowMetrics(StudentAttendanceRowViewModel row)
    {
        DateOnly editingDate = DateOnly.FromDateTime((SelectedDate ?? DateTimeOffset.Now).DateTime);
        decimal minAttendance = _activeYear?.MinAttendancePercentage ?? 75.0m;

        var records = new List<AttendanceRecord>();
        foreach (AttendanceSession session in _historicalSessions)
        {
            if (session.Date == editingDate)
            {
                continue;
            }

            AttendanceEntry? entry = session.Entries.FirstOrDefault(e => e.StudentId == row.StudentId);
            if (entry is not null)
            {
                records.Add(new AttendanceRecord(session.LessonCount, entry.Status));
            }
        }

        records.Add(new AttendanceRecord(EffectiveLessonCount, row.CurrentStatus));

        AttendanceSummary summary = _attendanceCalculator.Calculate(records, minAttendance);
        row.UpdateMetrics(summary);
    }

    private void UpdateSessionAnalytics()
    {
        PresentCount = _allRows.Count(r => r.CurrentStatus == AttendanceStatus.Present);
        AbsentCount = _allRows.Count(r => r.CurrentStatus == AttendanceStatus.Absent);
        JustifiedCount = _allRows.Count(r => r.CurrentStatus == AttendanceStatus.Justified);
        AlertCount = _allRows.Count(r => r.AlertStatus != AttendanceAlertStatus.Normal);
    }
}
