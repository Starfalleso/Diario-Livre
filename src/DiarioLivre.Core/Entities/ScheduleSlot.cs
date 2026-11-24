namespace DiarioLivre.Core.Entities;

/// <summary>
/// Represents a recurring weekly class slot in a teacher's timetable ("Grade Horária").
/// </summary>
public class ScheduleSlot
{
    public Guid Id { get; set; }

    public Guid AcademicYearId { get; set; }

    public Guid ClassroomId { get; set; }

    public Guid? SubjectId { get; set; }

    public DayOfWeek DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public int LessonCount { get; set; } = 1;

    public string? RoomLabel { get; set; }

    public AcademicYear? AcademicYear { get; set; }

    public Classroom? Classroom { get; set; }

    public Subject? Subject { get; set; }
}
