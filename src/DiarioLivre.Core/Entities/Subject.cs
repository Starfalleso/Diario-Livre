namespace DiarioLivre.Core.Entities;

/// <summary>
/// Represents a curriculum subject taught within a classroom.
/// </summary>
public class Subject
{
    public Guid Id { get; set; }

    public Guid ClassroomId { get; set; }

    public string Name { get; set; } = string.Empty;

    public Classroom? Classroom { get; set; }

    public ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();

    public ICollection<AttendanceSession> AttendanceSessions { get; set; } = new List<AttendanceSession>();
}
