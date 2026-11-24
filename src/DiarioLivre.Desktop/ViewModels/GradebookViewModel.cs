using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiarioLivre.Core.Entities;
using DiarioLivre.Core.Enums;
using DiarioLivre.Core.Interfaces;
using DiarioLivre.Core.Models;
using DiarioLivre.Desktop.Localization;
using DiarioLivre.Desktop.Services;

namespace DiarioLivre.Desktop.ViewModels;

public sealed partial class AssessmentChipViewModel : ObservableObject
{
    private readonly Func<Assessment, Task> _deleteAction;

    public AssessmentChipViewModel(Assessment assessment, Func<Assessment, Task> deleteAction)
    {
        Assessment = assessment;
        _deleteAction = deleteAction;
        Label = assessment.IsRecovery
            ? $"{assessment.Title} (Rec • {assessment.MaxScore:0.#})"
            : $"{assessment.Title} ({assessment.MaxScore:0.#} • w:{assessment.Weight:0.#})";
    }

    public Assessment Assessment { get; }
    public string Label { get; }
    public bool IsRecovery => Assessment.IsRecovery;

    [RelayCommand]
    private Task DeleteAsync() => _deleteAction(Assessment);
}

public sealed partial class GradeCellViewModel : ObservableObject
{
    private readonly Action _onScoreChanged;
    private readonly bool _suppressCallback;

    [ObservableProperty]
    private string _scoreText = string.Empty;

    [ObservableProperty]
    private bool _isInvalid;

    public GradeCellViewModel(Assessment assessment, decimal? initialScore, Action onScoreChanged)
    {
        AssessmentId = assessment.Id;
        MaxScore = assessment.MaxScore;
        _onScoreChanged = onScoreChanged;

        if (initialScore.HasValue)
        {
            _suppressCallback = true;
            _scoreText = initialScore.Value.ToString("0.##", CultureInfo.CurrentCulture);
            ParsedScore = initialScore.Value;
            _suppressCallback = false;
        }
    }

    public Guid AssessmentId { get; }
    public decimal MaxScore { get; }
    public decimal? ParsedScore { get; private set; }

    partial void OnScoreTextChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            ParsedScore = null;
            IsInvalid = false;
            if (!_suppressCallback)
            {
                _onScoreChanged();
            }
            return;
        }

        string normalized = value.Trim().Replace(',', '.');
        if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed) &&
            parsed >= 0m &&
            parsed <= MaxScore)
        {
            ParsedScore = parsed;
            IsInvalid = false;
        }
        else
        {
            IsInvalid = true;
        }

        if (!_suppressCallback)
        {
            _onScoreChanged();
        }
    }
}

public sealed partial class StudentGradeRowViewModel : ObservableObject, IDisposable
{
    private readonly Func<IReadOnlyList<Grade>, (TermGradeResult Term, FinalGradeResult Final)> _recalculateFunc;
    private readonly Action? _onUserEdited;
    private readonly bool _isInitializing = true;

    [ObservableProperty]
    private string _termScoreText = "—";

    [ObservableProperty]
    private string _finalScoreText = "—";

    [ObservableProperty]
    private bool _isBelowPassing;

    [ObservableProperty]
    private bool? _belowPassingState;

    [ObservableProperty]
    private bool? _finalBelowPassingState;

    [ObservableProperty]
    private string _statusLabel = string.Empty;

    [ObservableProperty]
    private bool _hasPending;

    [ObservableProperty]
    private bool _recoveryApplied;

    public StudentGradeRowViewModel(
        Student student,
        string? photoPath,
        IReadOnlyList<Assessment> termAssessments,
        IReadOnlyDictionary<(Guid AssessmentId, Guid StudentId), decimal?> existingGrades,
        Func<IReadOnlyList<Grade>, (TermGradeResult Term, FinalGradeResult Final)> recalculateFunc,
        Action? onUserEdited = null)
    {
        StudentId = student.Id;
        RollNumber = student.RollNumber;
        FullName = student.FullName;
        PhotoPath = photoPath;
        HasSpecialNeeds = student.HasSpecialNeeds;
        _recalculateFunc = recalculateFunc;
        _onUserEdited = onUserEdited;

        Cells = new ObservableCollection<GradeCellViewModel>();
        foreach (Assessment assessment in termAssessments)
        {
            existingGrades.TryGetValue((assessment.Id, student.Id), out decimal? score);
            Cells.Add(new GradeCellViewModel(assessment, score, HandleCellChanged));
        }

        LocalizationService.Instance.CultureChanged += OnCultureChanged;
        Recalculate();
        _isInitializing = false;
    }

    public Guid StudentId { get; }
    public LocalizationService Loc => LocalizationService.Instance;
    public int RollNumber { get; }
    public string FullName { get; }
    public string? PhotoPath { get; }
    public bool HasSpecialNeeds { get; }
    public string RollDisplay => $"{RollNumber:D2}";
    public decimal? NumericTermScore { get; private set; }
    public bool HasInvalidCells => Cells.Any(c => c.IsInvalid);
    public ObservableCollection<GradeCellViewModel> Cells { get; }

    public IEnumerable<Grade> BuildGradesForPersistence()
        => Cells.Where(c => !c.IsInvalid).Select(c => new Grade
        {
            Id = Guid.NewGuid(),
            AssessmentId = c.AssessmentId,
            StudentId = StudentId,
            Score = c.ParsedScore
        });

    public void Recalculate()
    {
        var termGrades = Cells.Select(c => new Grade
        {
            AssessmentId = c.AssessmentId,
            StudentId = StudentId,
            Score = c.ParsedScore
        }).ToList();

        (TermGradeResult term, FinalGradeResult final) = _recalculateFunc(termGrades);
        HasPending = term.HasPendingAssessments;
        RecoveryApplied = term.RecoveryApplied;
        NumericTermScore = term.Score;

        if (term.Score.HasValue)
        {
            TermScoreText = term.Score.Value.ToString("F2", CultureInfo.CurrentCulture);
            IsBelowPassing = term.IsPassing == false;
            BelowPassingState = IsBelowPassing;
            StatusLabel = IsBelowPassing
                ? LocalizationService.Instance.GetString("BelowPassingStatus")
                : LocalizationService.Instance.GetString("PassingStatus");
        }
        else
        {
            TermScoreText = "—";
            IsBelowPassing = false;
            BelowPassingState = null;
            StatusLabel = LocalizationService.Instance.GetString("PendingStatus");
        }

        if (final.FinalScore.HasValue)
        {
            FinalScoreText = final.FinalScore.Value.ToString("F2", CultureInfo.CurrentCulture);
            FinalBelowPassingState = !final.IsPassing;
        }
        else
        {
            FinalScoreText = "—";
            FinalBelowPassingState = null;
        }
    }

    public void Dispose()
    {
        LocalizationService.Instance.CultureChanged -= OnCultureChanged;
    }

    private void OnCultureChanged(object? sender, EventArgs e)
    {
        Recalculate();
    }

    private void HandleCellChanged()
    {
        Recalculate();
        if (!_isInitializing)
        {
            _onUserEdited?.Invoke();
        }
    }
}

public sealed partial class GradebookViewModel : ViewModelBase
{
    private readonly IAcademicYearRepository _academicYearRepository;
    private readonly IClassroomRepository _classroomRepository;
    private readonly IGradebookRepository _gradebookRepository;
    private readonly IGradeCalculator _gradeCalculator;
    private readonly IAvatarService _avatarService;
    private readonly IReportGenerator _reportGenerator;
    private readonly AppStateService _appStateService;
    private readonly ToastService _toastService;

    private AcademicYear? _activeYear;
    private IReadOnlyList<Assessment> _allSubjectAssessments = Array.Empty<Assessment>();
    private IReadOnlyList<Grade> _allSubjectGrades = Array.Empty<Grade>();
    private bool _isLoading;

    [ObservableProperty]
    private Classroom? _selectedClassroom;

    [ObservableProperty]
    private Subject? _selectedSubject;

    [ObservableProperty]
    private LocalizedOption<int>? _selectedTermOption;

    [ObservableProperty]
    private LocalizedOption<GradeCalculationMode>? _selectedCalculationModeOption;

    [ObservableProperty]
    private LocalizedOption<RecoveryPolicy>? _selectedRecoveryPolicyOption;

    [ObservableProperty]
    private bool _isAddingAssessment;

    [ObservableProperty]
    private string _newAssessmentTitle = string.Empty;

    [ObservableProperty]
    private decimal? _newAssessmentMaxScore = 10.0m;

    [ObservableProperty]
    private decimal? _newAssessmentWeight = 1.0m;

    [ObservableProperty]
    private bool _newAssessmentIsRecovery;

    [ObservableProperty]
    private bool _isAddingSubject;

    [ObservableProperty]
    private string _newSubjectName = string.Empty;

    [ObservableProperty]
    private bool _hasUnsavedChanges;

    [ObservableProperty]
    private bool _hasInvalidCells;

    [ObservableProperty]
    private string _classAverageText = "—";

    [ObservableProperty]
    private int _passingCount;

    [ObservableProperty]
    private int _belowPassingCount;

    [ObservableProperty]
    private int _pendingCount;

    [ObservableProperty]
    private double _passingDistributionPercentage;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private string? _warningMessage;

    public GradebookViewModel(
        IAcademicYearRepository academicYearRepository,
        IClassroomRepository classroomRepository,
        IGradebookRepository gradebookRepository,
        IGradeCalculator gradeCalculator,
        IAvatarService avatarService,
        IReportGenerator reportGenerator,
        AppStateService appStateService,
        ToastService toastService)
    {
        _academicYearRepository = academicYearRepository;
        _classroomRepository = classroomRepository;
        _gradebookRepository = gradebookRepository;
        _gradeCalculator = gradeCalculator;
        _avatarService = avatarService;
        _reportGenerator = reportGenerator;
        _appStateService = appStateService;
        _toastService = toastService;

        CalculationModeOptions = new ObservableCollection<LocalizedOption<GradeCalculationMode>>
        {
            new(GradeCalculationMode.WeightedAverage, loc => loc.GetGradeCalculationModeName(GradeCalculationMode.WeightedAverage)),
            new(GradeCalculationMode.SimpleSum, loc => loc.GetGradeCalculationModeName(GradeCalculationMode.SimpleSum))
        };
        _selectedCalculationModeOption = CalculationModeOptions[0];

        RecoveryPolicyOptions = new ObservableCollection<LocalizedOption<RecoveryPolicy>>
        {
            new(RecoveryPolicy.ReplaceLowest, loc => loc.GetRecoveryPolicyName(RecoveryPolicy.ReplaceLowest)),
            new(RecoveryPolicy.AverageWithRecovery, loc => loc.GetRecoveryPolicyName(RecoveryPolicy.AverageWithRecovery))
        };
        _selectedRecoveryPolicyOption = RecoveryPolicyOptions[0];
    }

    public event EventHandler? AssessmentsReloaded;

    public ObservableCollection<Classroom> Classrooms { get; } = new();
    public ObservableCollection<Subject> Subjects { get; } = new();
    public ObservableCollection<LocalizedOption<int>> TermOptions { get; } = new();
    public ObservableCollection<TermPillViewModel> TermPills { get; } = new();
    public ObservableCollection<LocalizedOption<GradeCalculationMode>> CalculationModeOptions { get; }
    public ObservableCollection<LocalizedOption<RecoveryPolicy>> RecoveryPolicyOptions { get; }
    public ObservableCollection<Assessment> Assessments { get; } = new();
    public ObservableCollection<AssessmentChipViewModel> AssessmentChips { get; } = new();
    public ObservableCollection<StudentGradeRowViewModel> Rows { get; } = new();

    public bool CanAddSubject => SelectedClassroom is { IsMultiSubject: false };
    public bool CanExport => Rows.Count > 0;

    public override async Task LoadAsync()
    {
        _isLoading = true;
        StatusMessage = null;
        WarningMessage = null;
        try
        {
            AcademicYear? year = _appStateService.SelectedAcademicYear;
            if (year is null)
            {
                IReadOnlyList<AcademicYear> allYears = await _academicYearRepository.GetAllAsync();
                year = allYears.FirstOrDefault();
                _appStateService.SelectedAcademicYear = year;
            }

            _activeYear = year;
            Classrooms.Clear();
            if (year is null)
            {
                return;
            }

            SelectedCalculationModeOption = CalculationModeOptions.First(x => x.Value == year.GradeCalculationMode);
            SelectedRecoveryPolicyOption = RecoveryPolicyOptions.First(x => x.Value == year.RecoveryPolicy);

            RebuildTermOptions(year.GradingSystem);

            IReadOnlyList<Classroom> classrooms = await _classroomRepository.GetByAcademicYearAsync(year.Id);
            foreach (Classroom c in classrooms)
            {
                Classrooms.Add(c);
            }

            Classroom? targetClassroom = _appStateService.SelectedClassroom is not null
                ? Classrooms.FirstOrDefault(c => c.Id == _appStateService.SelectedClassroom.Id)
                : null;

            SelectedClassroom = targetClassroom ?? Classrooms.FirstOrDefault();
        }
        finally
        {
            _isLoading = false;
        }

        await LoadSubjectsAndGridAsync();
    }

    public override Task TrySaveAsync() => SaveGradesCommand.ExecuteAsync(null);

    partial void OnSelectedClassroomChanged(Classroom? value)
    {
        _appStateService.SelectedClassroom = value;
        OnPropertyChanged(nameof(CanAddSubject));
        if (!CanAddSubject)
        {
            IsAddingSubject = false;
        }

        if (!_isLoading)
        {
            _ = LoadSubjectsAndGridAsync();
        }
    }

    partial void OnSelectedSubjectChanged(Subject? value)
    {
        if (!_isLoading)
        {
            _ = LoadGradeGridAsync();
        }
    }

    partial void OnSelectedTermOptionChanged(LocalizedOption<int>? value)
    {
        if (value is not null)
        {
            _appStateService.SelectedTermNumber = value.Value;
            foreach (TermPillViewModel pill in TermPills)
            {
                pill.IsSelected = pill.TermNumber == value.Value;
            }
        }

        if (!_isLoading)
        {
            _ = LoadGradeGridAsync();
        }
    }

    partial void OnSelectedCalculationModeOptionChanged(LocalizedOption<GradeCalculationMode>? value)
    {
        if (!_isLoading)
        {
            RecalculateAllRows();
        }
    }

    partial void OnSelectedRecoveryPolicyOptionChanged(LocalizedOption<RecoveryPolicy>? value)
    {
        if (!_isLoading)
        {
            RecalculateAllRows();
        }
    }

    [RelayCommand]
    private void ToggleAddAssessment()
    {
        IsAddingAssessment = !IsAddingAssessment;
        if (IsAddingAssessment)
        {
            IsAddingSubject = false;
        }
        else
        {
            NewAssessmentTitle = string.Empty;
        }
    }

    [RelayCommand]
    private void ToggleAddSubject()
    {
        if (!CanAddSubject)
        {
            return;
        }

        IsAddingSubject = !IsAddingSubject;
        if (IsAddingSubject)
        {
            IsAddingAssessment = false;
        }
        else
        {
            NewSubjectName = string.Empty;
        }
    }

    [RelayCommand]
    private async Task CreateSubjectAsync()
    {
        if (SelectedClassroom is null || SelectedClassroom.IsMultiSubject || string.IsNullOrWhiteSpace(NewSubjectName))
        {
            return;
        }

        var subject = new Subject
        {
            Id = Guid.NewGuid(),
            ClassroomId = SelectedClassroom.Id,
            Name = NewSubjectName.Trim()
        };

        await _classroomRepository.AddSubjectAsync(subject);
        NewSubjectName = string.Empty;
        IsAddingSubject = false;
        _toastService.ShowSuccess(LocalizationService.Instance.GetString("SavedSuccessMessage"));

        await LoadSubjectsAndGridAsync(preferredSubjectId: subject.Id);
    }

    [RelayCommand]
    private async Task CreateAssessmentAsync()
    {
        if (SelectedSubject is null || SelectedTermOption is null || string.IsNullOrWhiteSpace(NewAssessmentTitle))
        {
            return;
        }

        var assessment = new Assessment
        {
            Id = Guid.NewGuid(),
            SubjectId = SelectedSubject.Id,
            TermNumber = SelectedTermOption.Value,
            Title = NewAssessmentTitle.Trim(),
            MaxScore = NewAssessmentMaxScore is > 0 ? NewAssessmentMaxScore.Value : 10.0m,
            Weight = NewAssessmentWeight is > 0 ? NewAssessmentWeight.Value : 1.0m,
            IsRecovery = NewAssessmentIsRecovery
        };

        await _gradebookRepository.AddAssessmentAsync(assessment);
        NewAssessmentTitle = string.Empty;
        NewAssessmentIsRecovery = false;
        IsAddingAssessment = false;
        _toastService.ShowSuccess(LocalizationService.Instance.GetString("SavedSuccessMessage"));

        await LoadGradeGridAsync();
    }

    [RelayCommand]
    private async Task SaveGradesAsync()
    {
        if (Rows.Count == 0 || Assessments.Count == 0)
        {
            return;
        }

        HasInvalidCells = Rows.Any(r => r.HasInvalidCells);
        if (HasInvalidCells)
        {
            string warning = LocalizationService.Instance.GetString("InvalidGradesWarning");
            WarningMessage = warning;
            StatusMessage = null;
            _toastService.ShowWarning(warning);
            return;
        }

        List<Grade> grades = Rows.SelectMany(r => r.BuildGradesForPersistence()).ToList();
        await _gradebookRepository.UpsertGradesAsync(grades);
        _allSubjectGrades = await _gradebookRepository.GetGradesForSubjectAsync(SelectedSubject!.Id);
        HasUnsavedChanges = false;
        WarningMessage = null;
        string msg = LocalizationService.Instance.GetString("SavedSuccessMessage");
        StatusMessage = msg;
        _toastService.ShowSuccess(msg);
    }

    public async Task ExportCsvAsync(string outputPath)
    {
        if (Rows.Count == 0)
        {
            return;
        }

        LocalizationService loc = LocalizationService.Instance;
        string delimiter = loc.IsPortuguese ? ";" : ",";
        var sb = new StringBuilder();

        var headers = new List<string>
        {
            loc.GetString("RollColumnHeader"),
            loc.GetString("StudentColumnHeader")
        };
        headers.AddRange(Assessments.Select(a => $"{a.Title} ({a.MaxScore:0.#})"));
        headers.Add(loc.GetString("TermScoreColumnHeader"));
        headers.Add(loc.GetString("FinalScoreColumnHeader"));
        headers.Add(loc.GetString("StatusColumnHeader"));
        sb.AppendLine(string.Join(delimiter, headers.Select(EscapeCsv)));

        foreach (StudentGradeRowViewModel row in Rows)
        {
            var cols = new List<string>
            {
                row.RollDisplay,
                row.FullName
            };
            cols.AddRange(row.Cells.Select(c => string.IsNullOrWhiteSpace(c.ScoreText) ? "" : c.ScoreText));
            cols.Add(row.TermScoreText);
            cols.Add(row.FinalScoreText);
            cols.Add(row.StatusLabel);
            sb.AppendLine(string.Join(delimiter, cols.Select(EscapeCsv)));
        }

        await File.WriteAllTextAsync(outputPath, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        _toastService.ShowSuccess(loc.GetString("CsvExportedMessage"));
    }

    public async Task ExportClassPdfAsync(string outputPath)
    {
        if (Rows.Count == 0 || SelectedClassroom is null || SelectedSubject is null)
        {
            return;
        }

        LocalizationService loc = LocalizationService.Instance;
        var labels = new ClassGradebookReportLabels(
            ReportTitle: loc.GetString("ClassGradebookReportTitle"),
            RollColumn: loc.GetString("RollColumnHeader"),
            StudentColumn: loc.GetString("StudentColumnHeader"),
            TermScoreColumn: loc.GetString("TermScoreColumnHeader"),
            FinalScoreColumn: loc.GetString("FinalScoreColumnHeader"),
            StatusColumn: loc.GetString("StatusColumnHeader"),
            ClassAverageLabel: loc.GetString("ClassAverageLabel"),
            PassingLabel: loc.GetString("PassingCountLabel"),
            BelowPassingLabel: loc.GetString("BelowPassingCountLabel"),
            PendingLabel: loc.GetString("PendingCountLabel"),
            GeneratedAtLabel: loc.GetString("PdfGeneratedAtLabel"));

        var reportData = new ClassGradebookReportData(
            SchoolName: _activeYear?.SchoolName ?? "Diário Livre",
            Year: _activeYear?.Year ?? DateTime.Now.Year,
            ClassroomName: SelectedClassroom.Name,
            SubjectName: SelectedSubject.Name,
            TermLabel: SelectedTermOption?.Label ?? "1",
            ClassAverageText: ClassAverageText,
            PassingCount: PassingCount,
            BelowPassingCount: BelowPassingCount,
            PendingCount: PendingCount,
            Assessments: Assessments.Select(a => new ClassGradebookAssessmentColumn(a.Title, a.MaxScore, a.Weight, a.IsRecovery)).ToList(),
            Rows: Rows.Select(r => new ClassGradebookStudentRow(
                r.RollNumber,
                r.FullName,
                r.Cells.Select(c => string.IsNullOrWhiteSpace(c.ScoreText) ? "—" : c.ScoreText).ToList(),
                r.TermScoreText,
                r.FinalScoreText,
                r.StatusLabel,
                r.IsBelowPassing,
                r.RecoveryApplied)).ToList(),
            Labels: labels);

        await _reportGenerator.GenerateClassGradebookPdfAsync(reportData, outputPath);
        _toastService.ShowSuccess(loc.GetString("PdfExportedMessage"));
    }

    public string GetSuggestedExportFileName(string extension)
    {
        string classroom = SelectedClassroom?.Name ?? "turma";
        string subject = SelectedSubject?.Name ?? "disciplina";
        string termTag = SelectedTermOption is null
            ? "P1"
            : LocalizationService.Instance.GetTermShortName(
                _activeYear?.GradingSystem ?? GradingSystem.Semester,
                SelectedTermOption.Value);
        string raw = $"Notas_{classroom}_{subject}_{termTag}".Replace(' ', '_');
        string safe = string.Join("_", raw.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        return $"{safe}.{extension}";
    }

    private async Task DeleteAssessmentAsync(Assessment assessment)
    {
        await _gradebookRepository.DeleteAssessmentAsync(assessment.Id);
        _toastService.ShowInfo(LocalizationService.Instance.GetString("SavedSuccessMessage"));
        await LoadGradeGridAsync();
    }

    private void RebuildTermOptions(GradingSystem gradingSystem)
    {
        foreach (TermPillViewModel existingPill in TermPills)
        {
            existingPill.Dispose();
        }
        TermPills.Clear();
        TermOptions.Clear();

        int count = gradingSystem.GetTermCount();
        int desiredTerm = Math.Clamp(_appStateService.SelectedTermNumber, 1, count);
        _appStateService.SelectedTermNumber = desiredTerm;

        for (int i = 1; i <= count; i++)
        {
            int termIndex = i;
            TermOptions.Add(new LocalizedOption<int>(
                termIndex,
                loc => loc.GetTermName(gradingSystem, termIndex)));
            TermPills.Add(new TermPillViewModel(
                termIndex,
                loc => loc.GetTermName(gradingSystem, termIndex),
                termIndex == desiredTerm,
                SelectTermFromPill));
        }

        SelectedTermOption = TermOptions.FirstOrDefault(t => t.Value == desiredTerm)
            ?? TermOptions.FirstOrDefault();
    }

    private void SelectTermFromPill(int? termNumber)
    {
        if (!termNumber.HasValue)
        {
            return;
        }

        LocalizedOption<int>? match = TermOptions.FirstOrDefault(t => t.Value == termNumber.Value);
        if (match is not null)
        {
            SelectedTermOption = match;
        }
    }

    private async Task LoadSubjectsAndGridAsync(Guid? preferredSubjectId = null)
    {
        _isLoading = true;
        try
        {
            Subjects.Clear();
            if (SelectedClassroom is not null)
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

            Subject? preferred = preferredSubjectId.HasValue
                ? Subjects.FirstOrDefault(s => s.Id == preferredSubjectId.Value)
                : null;

            SelectedSubject = preferred ?? Subjects.FirstOrDefault();
        }
        finally
        {
            _isLoading = false;
        }

        await LoadGradeGridAsync();
    }

    private async Task LoadGradeGridAsync()
    {
        StatusMessage = null;
        WarningMessage = null;
        HasUnsavedChanges = false;
        HasInvalidCells = false;

        Assessments.Clear();
        AssessmentChips.Clear();

        foreach (StudentGradeRowViewModel existingRow in Rows)
        {
            existingRow.Dispose();
        }
        Rows.Clear();

        if (SelectedClassroom is null || SelectedSubject is null || SelectedTermOption is null)
        {
            _allSubjectAssessments = Array.Empty<Assessment>();
            _allSubjectGrades = Array.Empty<Grade>();
            UpdateClassAnalytics();
            OnPropertyChanged(nameof(CanExport));
            AssessmentsReloaded?.Invoke(this, EventArgs.Empty);
            return;
        }

        int termNumber = SelectedTermOption.Value;
        _allSubjectAssessments = await _gradebookRepository.GetAssessmentsAsync(SelectedSubject.Id);
        _allSubjectGrades = await _gradebookRepository.GetGradesForSubjectAsync(SelectedSubject.Id);

        List<Assessment> termAssessments = _allSubjectAssessments
            .Where(a => a.TermNumber == termNumber)
            .ToList();

        foreach (Assessment assessment in termAssessments)
        {
            Assessments.Add(assessment);
            AssessmentChips.Add(new AssessmentChipViewModel(assessment, DeleteAssessmentAsync));
        }

        var gradeLookup = _allSubjectGrades.ToDictionary(g => (g.AssessmentId, g.StudentId), g => g.Score);

        Classroom? details = await _classroomRepository.GetWithDetailsAsync(SelectedClassroom.Id);
        List<Student> students = details?.Students.OrderBy(s => s.RollNumber).ToList() ?? new List<Student>();

        foreach (Student student in students)
        {
            string? photoPath = _avatarService.GetAvatarFullPath(student.PhotoFileName);
            Guid studentId = student.Id;
            Rows.Add(new StudentGradeRowViewModel(
                student,
                photoPath,
                Assessments,
                gradeLookup,
                termGrades => CalculateTermAndFinalForRow(studentId, termNumber, termAssessments, termGrades),
                OnCellEditedByUser));
        }

        UpdateClassAnalytics();
        OnPropertyChanged(nameof(CanExport));
        AssessmentsReloaded?.Invoke(this, EventArgs.Empty);
    }

    private void OnCellEditedByUser()
    {
        HasUnsavedChanges = true;
        StatusMessage = null;
        HasInvalidCells = Rows.Any(r => r.HasInvalidCells);
        WarningMessage = HasInvalidCells
            ? LocalizationService.Instance.GetString("InvalidGradesWarning")
            : null;
        UpdateClassAnalytics();
    }

    private void RecalculateAllRows()
    {
        foreach (StudentGradeRowViewModel row in Rows)
        {
            row.Recalculate();
        }

        UpdateClassAnalytics();
    }

    private void UpdateClassAnalytics()
    {
        List<decimal> scores = Rows
            .Where(r => r.NumericTermScore.HasValue)
            .Select(r => r.NumericTermScore!.Value)
            .ToList();

        ClassAverageText = scores.Count > 0
            ? scores.Average().ToString("F2", CultureInfo.CurrentCulture)
            : "—";

        PassingCount = Rows.Count(r => r.NumericTermScore.HasValue && !r.IsBelowPassing);
        BelowPassingCount = Rows.Count(r => r.NumericTermScore.HasValue && r.IsBelowPassing);
        PendingCount = Rows.Count(r => !r.NumericTermScore.HasValue);

        int gradedTotal = PassingCount + BelowPassingCount;
        PassingDistributionPercentage = gradedTotal > 0
            ? Math.Round((double)PassingCount / gradedTotal * 100.0, 1)
            : 0.0;
    }

    private (TermGradeResult Term, FinalGradeResult Final) CalculateTermAndFinalForRow(
        Guid studentId,
        int termNumber,
        IReadOnlyList<Assessment> termAssessments,
        IReadOnlyList<Grade> liveTermGrades)
    {
        GradeCalculationMode mode = SelectedCalculationModeOption?.Value ?? GradeCalculationMode.WeightedAverage;
        RecoveryPolicy policy = SelectedRecoveryPolicyOption?.Value ?? RecoveryPolicy.ReplaceLowest;
        decimal passingGrade = _activeYear?.PassingGrade ?? 6.0m;
        GradingSystem gradingSystem = _activeYear?.GradingSystem ?? GradingSystem.Bimonthly;

        int termCount = gradingSystem.GetTermCount();
        decimal termPassing = mode == GradeCalculationMode.SimpleSum
            ? Math.Round(passingGrade / termCount, 2, MidpointRounding.AwayFromZero)
            : passingGrade;

        var options = new GradeCalculationOptions(mode, policy, passingGrade);
        TermGradeResult termResult = _gradeCalculator.CalculateTermGrade(
            termNumber, termAssessments, liveTermGrades, options, termPassing);

        var activeTermIds = new HashSet<Guid>(termAssessments.Select(a => a.Id));
        var combinedGrades = _allSubjectGrades
            .Where(g => g.StudentId == studentId && !activeTermIds.Contains(g.AssessmentId))
            .Concat(liveTermGrades)
            .ToList();

        FinalGradeResult finalResult = _gradeCalculator.CalculateFinalGrade(
            gradingSystem, _allSubjectAssessments, combinedGrades, options);

        return (termResult, finalResult);
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(';') || value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
