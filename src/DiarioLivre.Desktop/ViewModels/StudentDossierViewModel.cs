using System.Collections.ObjectModel;
using Avalonia.Media;
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

public sealed class StudentListItemViewModel
{
    public StudentListItemViewModel(Student student, string? photoPath)
    {
        Student = student;
        PhotoPath = photoPath;
    }

    public Student Student { get; }
    public Guid Id => Student.Id;
    public int RollNumber => Student.RollNumber;
    public string RollDisplay => $"{RollNumber:D2}";
    public string FullName => Student.FullName;
    public bool HasSpecialNeeds => Student.HasSpecialNeeds;
    public string? PhotoPath { get; }
}

public sealed class SubjectDossierGradeViewModel : ObservableObject, IDisposable
{
    private static readonly IBrush PassingBarBrush = new SolidColorBrush(Color.Parse("#22C55E"));
    private static readonly IBrush DangerBarBrush = new SolidColorBrush(Color.Parse("#EF4444"));
    private readonly GradingSystem _gradingSystem;

    public SubjectDossierGradeViewModel(
        string subjectName,
        GradingSystem gradingSystem,
        FinalGradeResult result,
        decimal passingGrade)
    {
        SubjectName = subjectName;
        _gradingSystem = gradingSystem;
        FinalResult = result;
        FinalScoreText = result.FinalScore.HasValue ? result.FinalScore.Value.ToString("F2") : "—";
        IsBelowPassing = result.FinalScore.HasValue && !result.IsPassing;
        BelowPassingState = result.FinalScore.HasValue ? !result.IsPassing : null;

        decimal maxScale = result.TermResults
            .Select(t => t.MaxPossibleScore)
            .Where(m => m > 0)
            .DefaultIfEmpty(passingGrade > 10 ? 100m : 10m)
            .Max();

        if (result.FinalScore.HasValue && maxScale > 0)
        {
            double pct = (double)(result.FinalScore.Value / maxScale * 100m);
            ScoreProgressPercentage = Math.Clamp(pct, 0.0, 100.0);
        }
        else
        {
            ScoreProgressPercentage = 0.0;
        }

        ProgressBrush = IsBelowPassing ? DangerBarBrush : PassingBarBrush;

        LocalizationService.Instance.CultureChanged += OnCultureChanged;
    }

    public string SubjectName { get; }
    public FinalGradeResult FinalResult { get; }

    public string TermsSummary => string.Join("  |  ", FinalResult.TermResults.Select(t =>
        $"{LocalizationService.Instance.GetTermShortName(_gradingSystem, t.TermNumber)}: {(t.Score.HasValue ? t.Score.Value.ToString("F1") : "—")}"));

    public string FinalScoreText { get; }
    public bool IsBelowPassing { get; }
    public bool? BelowPassingState { get; }
    public double ScoreProgressPercentage { get; }
    public IBrush ProgressBrush { get; }

    public string StatusText => !FinalResult.FinalScore.HasValue
        ? LocalizationService.Instance.GetString("PendingStatus")
        : FinalResult.IsPassing
            ? LocalizationService.Instance.GetString("PassingStatus")
            : LocalizationService.Instance.GetString("BelowPassingStatus");

    public void RefreshTheme()
    {
        OnPropertyChanged(nameof(BelowPassingState));
    }

    public void Dispose()
    {
        LocalizationService.Instance.CultureChanged -= OnCultureChanged;
    }

    private void OnCultureChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(TermsSummary));
    }
}

public sealed partial class ObservationItemViewModel : ObservableObject, IDisposable
{
    private readonly Func<ObservationItemViewModel, Task> _deleteAction;

    public ObservationItemViewModel(
        StudentObservation observation,
        Func<ObservationItemViewModel, Task> deleteAction)
    {
        Observation = observation;
        _deleteAction = deleteAction;
        LocalizationService.Instance.CultureChanged += OnCultureChanged;
    }

    public StudentObservation Observation { get; }
    public LocalizationService Loc => LocalizationService.Instance;
    public Guid Id => Observation.Id;
    public DateOnly Date => Observation.Date;
    public string DateDisplay => Observation.Date.ToString("dd/MM/yyyy");
    public ObservationCategory Category => Observation.Category;
    public string CategoryLabel => LocalizationService.Instance.GetObservationCategoryName(Observation.Category);
    public string Content => Observation.Content;

    [RelayCommand]
    private Task DeleteAsync() => _deleteAction(this);

    public void RefreshTheme()
    {
        OnPropertyChanged(nameof(Category));
    }

    public void Dispose()
    {
        LocalizationService.Instance.CultureChanged -= OnCultureChanged;
    }

    private void OnCultureChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(CategoryLabel));
    }
}

public sealed partial class ObservationFilterPillViewModel : ObservableObject, IDisposable
{
    private readonly Func<LocalizationService, string> _labelSelector;
    private readonly Action<ObservationCategory?> _selectAction;

    [ObservableProperty]
    private bool _isSelected;

    public ObservationFilterPillViewModel(
        ObservationCategory? category,
        Func<LocalizationService, string> labelSelector,
        bool isSelected,
        Action<ObservationCategory?> selectAction)
    {
        Category = category;
        _labelSelector = labelSelector;
        _isSelected = isSelected;
        _selectAction = selectAction;
        LocalizationService.Instance.CultureChanged += OnCultureChanged;
    }

    public ObservationCategory? Category { get; }
    public string Label => _labelSelector(LocalizationService.Instance);

    [RelayCommand]
    private void Select() => _selectAction(Category);

    public void Dispose()
    {
        LocalizationService.Instance.CultureChanged -= OnCultureChanged;
    }

    private void OnCultureChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(Label));
    }
}

public sealed partial class StudentDossierViewModel : ViewModelBase
{
    private readonly IAcademicYearRepository _academicYearRepository;
    private readonly IClassroomRepository _classroomRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IObservationRepository _observationRepository;
    private readonly IGradeCalculator _gradeCalculator;
    private readonly IAttendanceCalculator _attendanceCalculator;
    private readonly IAvatarService _avatarService;
    private readonly IReportGenerator _reportGenerator;
    private readonly StudentBatchParser _batchParser;
    private readonly AppStateService _appStateService;
    private readonly ToastService _toastService;

    private List<StudentListItemViewModel> _allClassroomStudents = new();
    private List<ObservationItemViewModel> _allObservations = new();
    private Student? _loadedDossierStudent;
    private AttendanceSummary? _currentAttendanceSummary;
    private ObservationCategory? _activeObservationFilter;
    private bool _isLoading;

    [ObservableProperty]
    private Classroom? _selectedClassroom;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private StudentListItemViewModel? _selectedStudentItem;

    [ObservableProperty]
    private bool _isAddingStudent;

    [ObservableProperty]
    private string _newStudentFullName = string.Empty;

    [ObservableProperty]
    private decimal? _newStudentRollNumber = 1m;

    [ObservableProperty]
    private bool _newStudentHasSpecialNeeds;

    [ObservableProperty]
    private bool _isBatchImporting;

    [ObservableProperty]
    private string _batchImportText = string.Empty;

    [ObservableProperty]
    private int _batchDetectedCount;

    [ObservableProperty]
    private string _studentFullName = "—";

    [ObservableProperty]
    private decimal? _studentRollNumberValue = 1m;

    [ObservableProperty]
    private string _studentRollDisplay = "00";

    [ObservableProperty]
    private string? _studentPhotoPath;

    [ObservableProperty]
    private bool _hasSpecialNeeds;

    [ObservableProperty]
    private string _generalNotes = string.Empty;

    [ObservableProperty]
    private bool _confirmingDeleteStudent;

    [ObservableProperty]
    private int _totalLessons;

    [ObservableProperty]
    private int _presentLessons;

    [ObservableProperty]
    private int _unexcusedAbsences;

    [ObservableProperty]
    private int _justifiedAbsences;

    [ObservableProperty]
    private double _attendanceRateValue = 100.0;

    [ObservableProperty]
    private string _attendancePercentageText = "100.0%";

    [ObservableProperty]
    private string _absencePercentageText = "0.0%";

    [ObservableProperty]
    private AttendanceAlertStatus _alertStatus = AttendanceAlertStatus.Normal;

    [ObservableProperty]
    private LocalizedOption<ObservationCategory>? _selectedCategoryOption;

    [ObservableProperty]
    private DateTimeOffset? _newObservationDate = DateTimeOffset.Now.Date;

    [ObservableProperty]
    private string _newObservationContent = string.Empty;

    [ObservableProperty]
    private string? _statusMessage;

    public StudentDossierViewModel(
        IAcademicYearRepository academicYearRepository,
        IClassroomRepository classroomRepository,
        IStudentRepository studentRepository,
        IObservationRepository observationRepository,
        IGradeCalculator gradeCalculator,
        IAttendanceCalculator attendanceCalculator,
        IAvatarService avatarService,
        IReportGenerator reportGenerator,
        StudentBatchParser batchParser,
        AppStateService appStateService,
        ToastService toastService)
    {
        _academicYearRepository = academicYearRepository;
        _classroomRepository = classroomRepository;
        _studentRepository = studentRepository;
        _observationRepository = observationRepository;
        _gradeCalculator = gradeCalculator;
        _attendanceCalculator = attendanceCalculator;
        _avatarService = avatarService;
        _reportGenerator = reportGenerator;
        _batchParser = batchParser;
        _appStateService = appStateService;
        _toastService = toastService;

        CategoryOptions = new ObservableCollection<LocalizedOption<ObservationCategory>>
        {
            new(ObservationCategory.Pedagogical, loc => loc.GetObservationCategoryName(ObservationCategory.Pedagogical)),
            new(ObservationCategory.Behavioral, loc => loc.GetObservationCategoryName(ObservationCategory.Behavioral)),
            new(ObservationCategory.Praise, loc => loc.GetObservationCategoryName(ObservationCategory.Praise)),
            new(ObservationCategory.FamilyContact, loc => loc.GetObservationCategoryName(ObservationCategory.FamilyContact))
        };
        _selectedCategoryOption = CategoryOptions[0];

        ObservationFilterPills = new ObservableCollection<ObservationFilterPillViewModel>
        {
            new(null, loc => loc.GetString("AllCategoriesFilter"), true, SelectObservationFilter),
            new(ObservationCategory.Pedagogical, loc => loc.GetObservationCategoryName(ObservationCategory.Pedagogical), false, SelectObservationFilter),
            new(ObservationCategory.Behavioral, loc => loc.GetObservationCategoryName(ObservationCategory.Behavioral), false, SelectObservationFilter),
            new(ObservationCategory.Praise, loc => loc.GetObservationCategoryName(ObservationCategory.Praise), false, SelectObservationFilter),
            new(ObservationCategory.FamilyContact, loc => loc.GetObservationCategoryName(ObservationCategory.FamilyContact), false, SelectObservationFilter)
        };

        LocalizationService.Instance.CultureChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(AlertLabel));
            OnPropertyChanged(nameof(DeleteStudentButtonText));
        };
    }

    public ObservableCollection<Classroom> Classrooms { get; } = new();
    public ObservableCollection<StudentListItemViewModel> FilteredStudents { get; } = new();
    public ObservableCollection<SubjectDossierGradeViewModel> SubjectGrades { get; } = new();
    public ObservableCollection<TermPillViewModel> AttendancePeriodPills { get; } = new();
    public ObservableCollection<ObservationItemViewModel> Observations { get; } = new();
    public ObservableCollection<LocalizedOption<ObservationCategory>> CategoryOptions { get; }
    public ObservableCollection<ObservationFilterPillViewModel> ObservationFilterPills { get; }

    private int? _selectedAttendanceTermFilter;

    public bool HasSelectedStudent => _loadedDossierStudent is not null;
    public bool HasStudentPhoto => !string.IsNullOrWhiteSpace(StudentPhotoPath);
    public bool CanBatchImport => BatchDetectedCount > 0;
    public bool HasNoStudents => FilteredStudents.Count == 0;
    public bool HasNoObservations => Observations.Count == 0;
    public string AlertLabel => LocalizationService.Instance.GetAlertStatusName(AlertStatus);

    public string DeleteStudentButtonText => ConfirmingDeleteStudent
        ? LocalizationService.Instance.GetString("ConfirmDeleteButton")
        : LocalizationService.Instance.GetString("DeleteStudentButton");

    public override async Task LoadAsync()
    {
        _isLoading = true;
        StatusMessage = null;
        try
        {
            AcademicYear? year = _appStateService.SelectedAcademicYear;
            if (year is null)
            {
                IReadOnlyList<AcademicYear> years = await _academicYearRepository.GetAllAsync();
                year = years.FirstOrDefault();
                _appStateService.SelectedAcademicYear = year;
            }

            Classrooms.Clear();
            if (year is null)
            {
                return;
            }

            IReadOnlyList<Classroom> classrooms = await _classroomRepository.GetByAcademicYearAsync(year.Id);
            foreach (Classroom c in classrooms)
            {
                Classrooms.Add(c);
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

        await LoadClassroomStudentsAsync();
    }

    public override Task TrySaveAsync() => SaveProfileCommand.ExecuteAsync(null);

    public override void RefreshThemeVisuals()
    {
        OnPropertyChanged(nameof(AlertStatus));
        foreach (SubjectDossierGradeViewModel subjectGrade in SubjectGrades)
        {
            subjectGrade.RefreshTheme();
        }
        foreach (ObservationItemViewModel observation in _allObservations)
        {
            observation.RefreshTheme();
        }
    }

    partial void OnSelectedClassroomChanged(Classroom? value)
    {
        _appStateService.SelectedClassroom = value;
        IsAddingStudent = false;
        IsBatchImporting = false;
        if (!_isLoading)
        {
            _ = LoadClassroomStudentsAsync();
        }
    }

    partial void OnSearchQueryChanged(string value)
    {
        ApplyStudentFilter();
    }

    partial void OnSelectedStudentItemChanged(StudentListItemViewModel? value)
    {
        if (value is not null && !_isLoading)
        {
            _ = LoadDossierDetailsAsync(value.Id);
        }
    }

    partial void OnBatchImportTextChanged(string value)
    {
        BatchDetectedCount = _batchParser.Parse(value).Count;
        OnPropertyChanged(nameof(CanBatchImport));
    }

    partial void OnStudentPhotoPathChanged(string? value)
    {
        OnPropertyChanged(nameof(HasStudentPhoto));
    }

    partial void OnConfirmingDeleteStudentChanged(bool value)
    {
        OnPropertyChanged(nameof(DeleteStudentButtonText));
    }

    [RelayCommand]
    private void ToggleAddStudent()
    {
        IsAddingStudent = !IsAddingStudent;
        if (IsAddingStudent)
        {
            IsBatchImporting = false;
            int nextRoll = _allClassroomStudents.Count > 0
                ? _allClassroomStudents.Max(s => s.RollNumber) + 1
                : 1;
            NewStudentRollNumber = nextRoll;
            NewStudentFullName = string.Empty;
            NewStudentHasSpecialNeeds = false;
        }
    }

    [RelayCommand]
    private void ToggleBatchImport()
    {
        IsBatchImporting = !IsBatchImporting;
        if (IsBatchImporting)
        {
            IsAddingStudent = false;
        }
        else
        {
            BatchImportText = string.Empty;
        }
    }

    [RelayCommand]
    private async Task CreateStudentAsync()
    {
        if (SelectedClassroom is null || string.IsNullOrWhiteSpace(NewStudentFullName))
        {
            return;
        }

        var student = new Student
        {
            Id = Guid.NewGuid(),
            ClassroomId = SelectedClassroom.Id,
            RollNumber = Math.Max(1, (int)(NewStudentRollNumber ?? 1m)),
            FullName = NewStudentFullName.Trim(),
            HasSpecialNeeds = NewStudentHasSpecialNeeds
        };

        await _studentRepository.AddAsync(student);
        NewStudentFullName = string.Empty;
        NewStudentHasSpecialNeeds = false;
        IsAddingStudent = false;
        _toastService.ShowSuccess(LocalizationService.Instance.GetString("SavedSuccessMessage"));

        await LoadClassroomStudentsAsync(preserveSelectionId: student.Id);
    }

    [RelayCommand]
    private async Task ImportBatchStudentsAsync()
    {
        if (SelectedClassroom is null)
        {
            return;
        }

        IReadOnlyList<ParsedStudentEntry> entries = _batchParser.Parse(BatchImportText);
        if (entries.Count == 0)
        {
            return;
        }

        int nextRoll = _allClassroomStudents.Count > 0
            ? _allClassroomStudents.Max(s => s.RollNumber) + 1
            : 1;

        var newStudents = new List<Student>(entries.Count);
        foreach (ParsedStudentEntry entry in entries)
        {
            int roll = entry.RollNumber ?? nextRoll++;
            if (entry.RollNumber.HasValue && entry.RollNumber.Value >= nextRoll)
            {
                nextRoll = entry.RollNumber.Value + 1;
            }

            newStudents.Add(new Student
            {
                Id = Guid.NewGuid(),
                ClassroomId = SelectedClassroom.Id,
                RollNumber = roll,
                FullName = entry.FullName,
                HasSpecialNeeds = false
            });
        }

        await _studentRepository.AddRangeAsync(newStudents);
        BatchImportText = string.Empty;
        IsBatchImporting = false;
        _toastService.ShowSuccess(LocalizationService.Instance.GetString("BatchImportedMessage"));

        await LoadClassroomStudentsAsync(preserveSelectionId: newStudents[0].Id);
    }

    public async Task LoadBatchFromFileAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return;
        }

        BatchImportText = await File.ReadAllTextAsync(filePath);
    }

    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        if (_loadedDossierStudent is null || string.IsNullOrWhiteSpace(StudentFullName))
        {
            return;
        }

        _loadedDossierStudent.FullName = StudentFullName.Trim();
        _loadedDossierStudent.RollNumber = Math.Max(1, (int)(StudentRollNumberValue ?? _loadedDossierStudent.RollNumber));
        _loadedDossierStudent.HasSpecialNeeds = HasSpecialNeeds;
        _loadedDossierStudent.GeneralNotes = string.IsNullOrWhiteSpace(GeneralNotes) ? null : GeneralNotes.Trim();

        await _studentRepository.UpdateAsync(_loadedDossierStudent);
        StudentRollDisplay = $"{_loadedDossierStudent.RollNumber:D2}";
        string msg = LocalizationService.Instance.GetString("SavedSuccessMessage");
        StatusMessage = msg;
        _toastService.ShowSuccess(msg);
        await LoadClassroomStudentsAsync(preserveSelectionId: _loadedDossierStudent.Id);
    }

    [RelayCommand]
    private async Task RemovePhotoAsync()
    {
        if (_loadedDossierStudent is null || string.IsNullOrWhiteSpace(_loadedDossierStudent.PhotoFileName))
        {
            return;
        }

        _avatarService.DeleteAvatar(_loadedDossierStudent.PhotoFileName);
        _loadedDossierStudent.PhotoFileName = null;
        await _studentRepository.UpdateAsync(_loadedDossierStudent);

        StudentPhotoPath = null;
        _toastService.ShowInfo(LocalizationService.Instance.GetString("SavedSuccessMessage"));
        await LoadClassroomStudentsAsync(preserveSelectionId: _loadedDossierStudent.Id);
    }

    [RelayCommand]
    private async Task RequestDeleteStudentAsync()
    {
        if (_loadedDossierStudent is null)
        {
            return;
        }

        if (!ConfirmingDeleteStudent)
        {
            ConfirmingDeleteStudent = true;
            return;
        }

        Guid deletingId = _loadedDossierStudent.Id;
        string? photoToRemove = _loadedDossierStudent.PhotoFileName;

        await _studentRepository.DeleteAsync(deletingId);
        if (!string.IsNullOrWhiteSpace(photoToRemove))
        {
            _avatarService.DeleteAvatar(photoToRemove);
        }

        if (_appStateService.SelectedStudent?.Id == deletingId)
        {
            _appStateService.SelectedStudent = null;
        }

        ConfirmingDeleteStudent = false;
        _toastService.ShowInfo(LocalizationService.Instance.GetString("StudentDeletedMessage"));
        await LoadClassroomStudentsAsync();
    }

    [RelayCommand]
    private void CancelDeleteStudent()
    {
        ConfirmingDeleteStudent = false;
    }

    [RelayCommand]
    private async Task AddObservationAsync()
    {
        if (_loadedDossierStudent is null || string.IsNullOrWhiteSpace(NewObservationContent))
        {
            return;
        }

        var observation = new StudentObservation
        {
            Id = Guid.NewGuid(),
            StudentId = _loadedDossierStudent.Id,
            Date = DateOnly.FromDateTime((NewObservationDate ?? DateTimeOffset.Now).DateTime),
            Category = SelectedCategoryOption?.Value ?? ObservationCategory.Pedagogical,
            Content = NewObservationContent.Trim()
        };

        await _observationRepository.AddAsync(observation);
        NewObservationContent = string.Empty;
        _toastService.ShowSuccess(LocalizationService.Instance.GetString("SavedSuccessMessage"));
        await LoadDossierDetailsAsync(_loadedDossierStudent.Id);
    }

    [RelayCommand]
    private async Task DeleteObservationAsync(ObservationItemViewModel? item)
    {
        if (item is null || _loadedDossierStudent is null)
        {
            return;
        }

        await _observationRepository.DeleteAsync(item.Id);
        _toastService.ShowInfo(LocalizationService.Instance.GetString("SavedSuccessMessage"));
        await LoadDossierDetailsAsync(_loadedDossierStudent.Id);
    }

    public async Task UpdatePhotoFromFileAsync(string sourceFilePath)
    {
        if (_loadedDossierStudent is null || string.IsNullOrWhiteSpace(sourceFilePath))
        {
            return;
        }

        string? oldFileName = _loadedDossierStudent.PhotoFileName;
        string newFileName = await _avatarService.SaveAvatarAsync(sourceFilePath);

        _loadedDossierStudent.PhotoFileName = newFileName;
        await _studentRepository.UpdateAsync(_loadedDossierStudent);

        if (!string.IsNullOrWhiteSpace(oldFileName))
        {
            _avatarService.DeleteAvatar(oldFileName);
        }

        StudentPhotoPath = _avatarService.GetAvatarFullPath(newFileName);
        string msg = LocalizationService.Instance.GetString("SavedSuccessMessage");
        StatusMessage = msg;
        _toastService.ShowSuccess(msg);
        await LoadClassroomStudentsAsync(preserveSelectionId: _loadedDossierStudent.Id);
    }

    public async Task ExportPdfAsync(string outputPath)
    {
        if (_loadedDossierStudent is null || _currentAttendanceSummary is null)
        {
            return;
        }

        AcademicYear? year = _loadedDossierStudent.Classroom?.AcademicYear ?? _appStateService.SelectedAcademicYear;
        string classroomName = _loadedDossierStudent.Classroom?.Name ?? SelectedClassroom?.Name ?? string.Empty;
        LocalizationService loc = LocalizationService.Instance;

        StudentDossierReportData reportData = BuildDossierReportData(
            _loadedDossierStudent,
            year,
            classroomName,
            StudentPhotoPath,
            _currentAttendanceSummary,
            SubjectGrades.Select(s => new SubjectGradeReportItem(s.SubjectName, s.FinalResult)).ToList(),
            _allObservations.Select(o => new ObservationReportItem(o.Date, o.CategoryLabel, o.Content)).ToList());

        await _reportGenerator.GenerateStudentDossierPdfAsync(reportData, outputPath);
        string msg = loc.GetString("PdfExportedMessage");
        StatusMessage = msg;
        _toastService.ShowSuccess(msg);
    }

    public async Task ExportBatchReportCardsPdfAsync(string outputPath)
    {
        if (SelectedClassroom is null)
        {
            return;
        }

        Classroom? fullClassroom = await _classroomRepository.GetForCouncilReportAsync(SelectedClassroom.Id);
        if (fullClassroom is null || fullClassroom.Students.Count == 0)
        {
            return;
        }

        AcademicYear? year = fullClassroom.AcademicYear ?? _appStateService.SelectedAcademicYear;
        GradingSystem grading = year?.GradingSystem ?? GradingSystem.Semester;
        decimal minAttendance = year?.MinAttendancePercentage ?? 75.0m;
        decimal passingGrade = year?.PassingGrade ?? 6.0m;
        var calcOptions = new GradeCalculationOptions(
            year?.GradeCalculationMode ?? GradeCalculationMode.WeightedAverage,
            year?.RecoveryPolicy ?? RecoveryPolicy.ReplaceLowest,
            passingGrade);

        var dossiers = new List<StudentDossierReportData>();
        foreach (Student student in fullClassroom.Students.OrderBy(s => s.RollNumber))
        {
            AttendanceSummary attendance = _attendanceCalculator.CalculateForStudent(
                student.Id,
                fullClassroom.AttendanceSessions,
                minAttendance);

            List<SubjectGradeReportItem> grades = fullClassroom.Subjects
                .OrderBy(s => s.Name)
                .Select(s => new SubjectGradeReportItem(
                    s.Name,
                    _gradeCalculator.CalculateFinalGrade(grading, s.Assessments, student.Grades, calcOptions)))
                .ToList();

            List<ObservationReportItem> observations = student.Observations
                .OrderByDescending(o => o.Date)
                .Select(o => new ObservationReportItem(
                    o.Date,
                    LocalizationService.Instance.GetObservationCategoryName(o.Category),
                    o.Content))
                .ToList();

            dossiers.Add(BuildDossierReportData(
                student,
                year,
                fullClassroom.Name,
                _avatarService.GetAvatarFullPath(student.PhotoFileName),
                attendance,
                grades,
                observations));
        }

        await _reportGenerator.GenerateBatchStudentDossiersPdfAsync(dossiers, outputPath);
        _toastService.ShowSuccess(LocalizationService.Instance.GetString("PdfExportedMessage"));
    }

    public async Task ExportClassCouncilPdfAsync(string outputPath)
    {
        if (SelectedClassroom is null)
        {
            return;
        }

        Classroom? fullClassroom = await _classroomRepository.GetForCouncilReportAsync(SelectedClassroom.Id);
        if (fullClassroom is null || fullClassroom.Students.Count == 0)
        {
            return;
        }

        AcademicYear? year = fullClassroom.AcademicYear ?? _appStateService.SelectedAcademicYear;
        GradingSystem grading = year?.GradingSystem ?? GradingSystem.Semester;
        decimal minAttendance = year?.MinAttendancePercentage ?? 75.0m;
        decimal passingGrade = year?.PassingGrade ?? 6.0m;
        var calcOptions = new GradeCalculationOptions(
            year?.GradeCalculationMode ?? GradeCalculationMode.WeightedAverage,
            year?.RecoveryPolicy ?? RecoveryPolicy.ReplaceLowest,
            passingGrade);

        List<Subject> orderedSubjects = fullClassroom.Subjects.OrderBy(s => s.Name).ToList();
        LocalizationService loc = LocalizationService.Instance;

        var studentRows = new List<ClassCouncilStudentRow>();
        foreach (Student student in fullClassroom.Students.OrderBy(s => s.RollNumber))
        {
            AttendanceSummary attendance = _attendanceCalculator.CalculateForStudent(
                student.Id,
                fullClassroom.AttendanceSessions,
                minAttendance);

            var subjectScores = new List<string>(orderedSubjects.Count);
            var numericScores = new List<decimal>();

            foreach (Subject subject in orderedSubjects)
            {
                FinalGradeResult result = _gradeCalculator.CalculateFinalGrade(
                    grading,
                    subject.Assessments,
                    student.Grades,
                    calcOptions);

                if (result.FinalScore.HasValue)
                {
                    numericScores.Add(result.FinalScore.Value);
                    subjectScores.Add($"{result.FinalScore.Value:F2}");
                }
                else
                {
                    subjectScores.Add("—");
                }
            }

            decimal? overallAvg = numericScores.Count > 0 ? numericScores.Average() : null;
            string overallText = overallAvg.HasValue ? $"{overallAvg.Value:F2}" : "—";
            bool belowPassing = overallAvg.HasValue && overallAvg.Value < passingGrade;

            var notesParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(student.GeneralNotes))
            {
                notesParts.Add(student.GeneralNotes.Trim());
            }
            StudentObservation? latestObs = student.Observations.OrderByDescending(o => o.Date).FirstOrDefault();
            if (latestObs is not null)
            {
                notesParts.Add($"[{latestObs.Date:dd/MM}] {latestObs.Content.Trim()}");
            }

            studentRows.Add(new ClassCouncilStudentRow(
                student.RollNumber,
                student.FullName,
                student.HasSpecialNeeds,
                attendance.AttendancePercentage,
                attendance.AlertStatus,
                loc.GetAlertStatusName(attendance.AlertStatus),
                subjectScores,
                overallText,
                belowPassing,
                string.Join(" | ", notesParts)));
        }

        var labels = new ClassCouncilReportLabels(
            ReportTitle: loc.GetString("CouncilReportTitle"),
            RollColumn: loc.GetString("RollColumnHeader"),
            StudentColumn: loc.GetString("StudentColumnHeader"),
            PdiColumn: loc.GetString("CouncilPdiColumn"),
            AttendanceColumn: loc.GetString("CouncilOverallColumn") == "Overall"
                ? loc.GetString("AttendanceRateLabel")
                : loc.GetString("AttendanceRateLabel"),
            OverallColumn: loc.GetString("CouncilOverallColumn"),
            NotesColumn: loc.GetString("CouncilNotesColumn"),
            YesShort: loc.GetString("YesShort"),
            NoShort: loc.GetString("NoShort"),
            PassingRuleLabel: loc.GetString("PassingGradeLabel"),
            MinAttendanceLabel: loc.GetString("MinAttendanceLabel"),
            GeneratedAtLabel: loc.GetString("PdfGeneratedAtLabel"));

        var reportData = new ClassCouncilReportData(
            SchoolName: year?.SchoolName ?? "Diário Livre",
            Year: year?.Year ?? DateTime.Now.Year,
            ClassroomName: fullClassroom.Name,
            ShiftDisplay: loc.GetShiftName(fullClassroom.Shift),
            GradingSystemDisplay: loc.GetGradingSystemName(grading),
            PassingGrade: passingGrade,
            MinAttendancePercentage: minAttendance,
            SubjectNames: orderedSubjects.Select(s => s.Name).ToList(),
            Students: studentRows,
            Labels: labels);

        await _reportGenerator.GenerateClassCouncilPdfAsync(reportData, outputPath);
        _toastService.ShowSuccess(loc.GetString("PdfExportedMessage"));
    }

    public string GetSuggestedCouncilPdfFileName()
    {
        string classroom = SelectedClassroom?.Name ?? "turma";
        string safe = string.Join("_", $"Conselho_{classroom}".Replace(' ', '_').Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        return $"{safe}.pdf";
    }

    public string GetSuggestedBatchPdfFileName()
    {
        string classroom = SelectedClassroom?.Name ?? "turma";
        string safe = string.Join("_", $"Boletins_{classroom}".Replace(' ', '_').Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        return $"{safe}.pdf";
    }

    public string GetSuggestedPdfFileName()
    {
        string rawName = _loadedDossierStudent?.FullName ?? "aluno";
        string safeName = string.Join("_", rawName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        return $"Ficha_{safeName.Replace(' ', '_')}.pdf";
    }

    private static StudentDossierReportData BuildDossierReportData(
        Student student,
        AcademicYear? year,
        string classroomName,
        string? avatarPath,
        AttendanceSummary attendanceSummary,
        IReadOnlyList<SubjectGradeReportItem> subjectGrades,
        IReadOnlyList<ObservationReportItem> observations)
    {
        LocalizationService loc = LocalizationService.Instance;
        var labels = new PdfReportLabels(
            SpecialNeedsBanner: loc.GetString("PdfSpecialNeedsBanner"),
            AttendanceSummaryTitle: loc.GetString("AttendanceSummaryTitle"),
            TotalLessonsLabel: loc.GetString("TotalLessonsLabel"),
            PresentLabel: loc.GetString("PresentLessonsLabel"),
            UnexcusedLabel: loc.GetString("UnexcusedAbsencesLabel"),
            JustifiedLabel: loc.GetString("JustifiedAbsencesLabel"),
            AttendanceRateLabel: loc.GetString("AttendanceRateLabel"),
            AbsencesRateLabel: loc.GetString("AbsencePercentageLabel"),
            StatusLabel: loc.GetString("StatusColumnHeader"),
            AlertStatusDisplay: loc.GetAlertStatusName(attendanceSummary.AlertStatus),
            AcademicPerformanceTitle: loc.GetString("GradesSummaryTitle"),
            SubjectColumn: loc.GetString("SubjectLabel"),
            FinalColumn: loc.GetString("FinalGradeLabel"),
            TermShortPrefix: loc.GetPdfTermPrefix(year?.GradingSystem ?? GradingSystem.Semester),
            NoSubjectsText: loc.GetString("PdfNoSubjectsText"),
            ObservationsTitle: loc.GetString("ObservationsTimelineTitle"),
            NoObservationsText: loc.GetString("NoObservationsText"));

        return new StudentDossierReportData(
            SchoolName: year?.SchoolName ?? "Diário Livre",
            Year: year?.Year ?? DateTime.Now.Year,
            ClassroomName: classroomName,
            Student: student,
            AvatarFilePath: avatarPath,
            AttendanceSummary: attendanceSummary,
            SubjectGrades: subjectGrades,
            Observations: observations,
            ReportTitle: loc.GetString("DossierTitle"),
            GeneratedAtLabel: loc.GetString("PdfGeneratedAtLabel"),
            Labels: labels);
    }

    private void SelectObservationFilter(ObservationCategory? category)
    {
        _activeObservationFilter = category;
        foreach (ObservationFilterPillViewModel pill in ObservationFilterPills)
        {
            pill.IsSelected = pill.Category == category;
        }

        ApplyObservationFilter();
    }

    private void ApplyObservationFilter()
    {
        Observations.Clear();
        IEnumerable<ObservationItemViewModel> matches = _activeObservationFilter.HasValue
            ? _allObservations.Where(o => o.Category == _activeObservationFilter.Value)
            : _allObservations;

        foreach (ObservationItemViewModel item in matches)
        {
            Observations.Add(item);
        }

        OnPropertyChanged(nameof(HasNoObservations));
    }

    private async Task LoadClassroomStudentsAsync(Guid? preserveSelectionId = null)
    {
        _isLoading = true;
        try
        {
            _allClassroomStudents.Clear();
            if (SelectedClassroom is not null)
            {
                IReadOnlyList<Student> students = await _studentRepository.GetByClassroomAsync(SelectedClassroom.Id);
                _allClassroomStudents = students
                    .Select(s => new StudentListItemViewModel(s, _avatarService.GetAvatarFullPath(s.PhotoFileName)))
                    .ToList();
            }

            ApplyStudentFilter();

            Guid? targetId = preserveSelectionId ?? _appStateService.SelectedStudent?.Id;
            StudentListItemViewModel? preferred = targetId.HasValue
                ? FilteredStudents.FirstOrDefault(s => s.Id == targetId.Value)
                : null;

            SelectedStudentItem = preferred ?? FilteredStudents.FirstOrDefault();
        }
        finally
        {
            _isLoading = false;
        }

        if (SelectedStudentItem is not null)
        {
            await LoadDossierDetailsAsync(SelectedStudentItem.Id);
        }
        else
        {
            ClearDossier();
        }
    }

    private void ApplyStudentFilter()
    {
        FilteredStudents.Clear();
        string query = SearchQuery.Trim();

        IEnumerable<StudentListItemViewModel> matches = string.IsNullOrEmpty(query)
            ? _allClassroomStudents
            : _allClassroomStudents.Where(s => s.FullName.Contains(query, StringComparison.OrdinalIgnoreCase));

        foreach (StudentListItemViewModel item in matches)
        {
            FilteredStudents.Add(item);
        }

        OnPropertyChanged(nameof(HasNoStudents));
    }

    private async Task LoadDossierDetailsAsync(Guid studentId)
    {
        StatusMessage = null;
        ConfirmingDeleteStudent = false;
        Student? student = await _studentRepository.GetDossierAsync(studentId);
        if (student is null)
        {
            ClearDossier();
            return;
        }

        _loadedDossierStudent = student;
        _appStateService.SelectedStudent = student;

        StudentFullName = student.FullName;
        StudentRollNumberValue = student.RollNumber;
        StudentRollDisplay = $"{student.RollNumber:D2}";
        StudentPhotoPath = _avatarService.GetAvatarFullPath(student.PhotoFileName);
        HasSpecialNeeds = student.HasSpecialNeeds;
        GeneralNotes = student.GeneralNotes ?? string.Empty;

        AcademicYear? year = student.Classroom?.AcademicYear ?? _appStateService.SelectedAcademicYear;
        GradingSystem grading = year?.GradingSystem ?? GradingSystem.Semester;
        RebuildAttendancePeriodPills(grading);
        RecalculateDossierAttendance();

        ClearSubjectGrades();
        if (student.Classroom is not null && year is not null)
        {
            var options = new GradeCalculationOptions(
                year.GradeCalculationMode,
                year.RecoveryPolicy,
                year.PassingGrade);

            foreach (Subject subject in student.Classroom.Subjects.OrderBy(s => s.Name))
            {
                FinalGradeResult finalGrade = _gradeCalculator.CalculateFinalGrade(
                    year.GradingSystem,
                    subject.Assessments,
                    student.Grades,
                    options);

                SubjectGrades.Add(new SubjectDossierGradeViewModel(subject.Name, year.GradingSystem, finalGrade, year.PassingGrade));
            }
        }

        ClearObservations();
        foreach (StudentObservation obs in student.Observations.OrderByDescending(o => o.Date))
        {
            _allObservations.Add(new ObservationItemViewModel(obs, DeleteObservationAsync));
        }

        ApplyObservationFilter();
        OnPropertyChanged(nameof(HasSelectedStudent));
    }

    private void RebuildAttendancePeriodPills(GradingSystem grading)
    {
        ClearAttendancePeriodPills();
        _selectedAttendanceTermFilter = null;

        AttendancePeriodPills.Add(new TermPillViewModel(
            null,
            loc => loc.GetString("AllYearPeriodOption"),
            true,
            SelectAttendancePeriod));

        int count = grading.GetTermCount();
        for (int i = 1; i <= count; i++)
        {
            int termIndex = i;
            AttendancePeriodPills.Add(new TermPillViewModel(
                termIndex,
                loc => loc.GetTermName(grading, termIndex),
                false,
                SelectAttendancePeriod));
        }
    }

    private void SelectAttendancePeriod(int? termNumber)
    {
        _selectedAttendanceTermFilter = termNumber;
        foreach (TermPillViewModel pill in AttendancePeriodPills)
        {
            pill.IsSelected = pill.TermNumber == termNumber;
        }

        RecalculateDossierAttendance();
    }

    private void RecalculateDossierAttendance()
    {
        if (_loadedDossierStudent is null)
        {
            return;
        }

        AcademicYear? year = _loadedDossierStudent.Classroom?.AcademicYear ?? _appStateService.SelectedAcademicYear;
        decimal minAttendance = year?.MinAttendancePercentage ?? 75.0m;

        var records = _loadedDossierStudent.AttendanceEntries
            .Where(e => e.AttendanceSession is not null
                        && (!_selectedAttendanceTermFilter.HasValue
                            || e.AttendanceSession!.TermNumber == _selectedAttendanceTermFilter.Value))
            .Select(e => new AttendanceRecord(e.AttendanceSession!.LessonCount, e.Status))
            .ToList();

        AttendanceSummary summary = _attendanceCalculator.Calculate(records, minAttendance);
        _currentAttendanceSummary = summary;

        TotalLessons = summary.TotalLessons;
        PresentLessons = summary.PresentLessons;
        UnexcusedAbsences = summary.UnexcusedAbsences;
        JustifiedAbsences = summary.JustifiedAbsences;
        AttendanceRateValue = (double)summary.AttendancePercentage;
        AttendancePercentageText = $"{summary.AttendancePercentage:F1}%";
        AbsencePercentageText = $"{summary.AbsencePercentage:F1}%";
        AlertStatus = summary.AlertStatus;
        OnPropertyChanged(nameof(AlertLabel));
    }

    private void ClearAttendancePeriodPills()
    {
        foreach (TermPillViewModel pill in AttendancePeriodPills)
        {
            pill.Dispose();
        }
        AttendancePeriodPills.Clear();
    }

    private void ClearSubjectGrades()
    {
        foreach (SubjectDossierGradeViewModel existing in SubjectGrades)
        {
            existing.Dispose();
        }
        SubjectGrades.Clear();
    }

    private void ClearObservations()
    {
        foreach (ObservationItemViewModel existing in _allObservations)
        {
            existing.Dispose();
        }
        _allObservations.Clear();
        Observations.Clear();
    }

    private void ClearDossier()
    {
        _loadedDossierStudent = null;
        _currentAttendanceSummary = null;
        ConfirmingDeleteStudent = false;
        StudentFullName = "—";
        StudentRollNumberValue = 1m;
        StudentRollDisplay = "00";
        StudentPhotoPath = null;
        HasSpecialNeeds = false;
        GeneralNotes = string.Empty;
        TotalLessons = 0;
        PresentLessons = 0;
        UnexcusedAbsences = 0;
        JustifiedAbsences = 0;
        AttendanceRateValue = 100.0;
        AttendancePercentageText = "100.0%";
        AbsencePercentageText = "0.0%";
        AlertStatus = AttendanceAlertStatus.Normal;
        ClearAttendancePeriodPills();
        ClearSubjectGrades();
        ClearObservations();
        OnPropertyChanged(nameof(HasSelectedStudent));
        OnPropertyChanged(nameof(HasNoObservations));
        OnPropertyChanged(nameof(AlertLabel));
    }
}
