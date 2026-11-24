using DiarioLivre.Core.Entities;

namespace DiarioLivre.Desktop.Services;

public enum AppSection
{
    Dashboard = 0,
    Gradebook = 1,
    Attendance = 2,
    StudentDossier = 3,
    Planner = 4,
    Calendar = 5,
    Settings = 6
}

/// <summary>
/// Coordinates view navigation across the main shell without introducing circular ViewModel dependencies.
/// </summary>
public sealed class NavigationService
{
    private readonly AppStateService _appStateService;

    public NavigationService(AppStateService appStateService)
    {
        _appStateService = appStateService;
    }

    public event Action<AppSection>? NavigationRequested;

    public void NavigateTo(AppSection section) => NavigationRequested?.Invoke(section);

    public void NavigateToStudentDossier(Classroom classroom, Student student)
    {
        _appStateService.SelectedClassroom = classroom;
        _appStateService.SelectedStudent = student;
        NavigateTo(AppSection.StudentDossier);
    }

    public void NavigateToAttendanceWithPrefill(AttendancePrefillRequest prefill)
    {
        _appStateService.PendingAttendancePrefill = prefill;
        NavigateTo(AppSection.Attendance);
    }
}
