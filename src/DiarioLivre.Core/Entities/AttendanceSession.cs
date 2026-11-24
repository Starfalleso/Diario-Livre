namespace DiarioLivre.Core.Entities;

/// <summary>
/// Represents a roll-call session on a given date, optionally linked to a specific subject.
/// </summary>
public class AttendanceSession
{
    public Guid Id { get; set; }

    public Guid ClassroomId { get; set; }

    /// <summary>
    /// Null when taking a single daily roll call in multi-subject classrooms (Fundamental I).
    /// </summary>
    public Guid? SubjectId { get; set; }

    public DateOnly Date { get; set; }

    public int TermNumber { get; set; } = 1;

    public int LessonCount { get; set; } = 1;

    public string? TopicCovered { get; set; }

    public string? DailyActivity { get; set; }

    public string? Homework { get; set; }

    public string? LessonNotes { get; set; }

    public Classroom? Classroom { get; set; }

    public Subject? Subject { get; set; }

    public ICollection<AttendanceEntry> Entries { get; set; } = new List<AttendanceEntry>();
}
