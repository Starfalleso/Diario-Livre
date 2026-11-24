using DiarioLivre.Core.Enums;

namespace DiarioLivre.Core.Entities;

/// <summary>
/// Represents an individual student's attendance status during an attendance session.
/// </summary>
public class AttendanceEntry
{
    public Guid Id { get; set; }

    public Guid AttendanceSessionId { get; set; }

    public Guid StudentId { get; set; }

    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;

    public AttendanceSession? AttendanceSession { get; set; }

    public Student? Student { get; set; }
}
