using CommunityToolkit.Mvvm.ComponentModel;
using DiarioLivre.Core.Entities;

namespace DiarioLivre.Desktop.Services;

public sealed record AttendancePrefillRequest(
    Guid ClassroomId,
    Guid? SubjectId,
    DateOnly Date,
    int LessonCount,
    string? TopicCovered = null,
    string? DailyActivity = null,
    string? Homework = null);

/// <summary>
/// Holds shared navigation context (active academic year, classroom, student, term, and prefill requests) across views.
/// </summary>
public sealed class AppStateService : ObservableObject
{
    private AcademicYear? _selectedAcademicYear;
    private Classroom? _selectedClassroom;
    private Student? _selectedStudent;
    private int _selectedTermNumber = 1;

    public AttendancePrefillRequest? PendingAttendancePrefill { get; set; }

    public AcademicYear? SelectedAcademicYear
    {
        get => _selectedAcademicYear;
        set => SetProperty(ref _selectedAcademicYear, value);
    }

    public int SelectedTermNumber
    {
        get => _selectedTermNumber;
        set => SetProperty(ref _selectedTermNumber, Math.Max(1, value));
    }

    public Classroom? SelectedClassroom
    {
        get => _selectedClassroom;
        set
        {
            if (SetProperty(ref _selectedClassroom, value))
            {
                OnPropertyChanged(nameof(ActiveClassroomName));
            }
        }
    }

    public Student? SelectedStudent
    {
        get => _selectedStudent;
        set => SetProperty(ref _selectedStudent, value);
    }

    public string ActiveClassroomName => SelectedClassroom?.Name ?? "—";
}
