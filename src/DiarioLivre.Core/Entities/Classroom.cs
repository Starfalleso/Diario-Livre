using DiarioLivre.Core.Enums;

namespace DiarioLivre.Core.Entities;

/// <summary>
/// Represents a school classroom belonging to an academic year.
/// </summary>
public class Classroom
{
    public Guid Id { get; set; }

    public Guid AcademicYearId { get; set; }

    public string Name { get; set; } = string.Empty;

    public Shift Shift { get; set; } = Shift.Morning;

    /// <summary>
    /// Indicates whether the classroom uses a single polyvalent teacher (Fundamental I)
    /// rather than subject-specific specialist teachers (Fundamental II / High School).
    /// </summary>
    public bool IsMultiSubject { get; set; }

    public int SeatRows { get; set; } = 5;

    public int SeatColumns { get; set; } = 6;

    public AcademicYear? AcademicYear { get; set; }

    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();

    public ICollection<Student> Students { get; set; } = new List<Student>();

    public ICollection<AttendanceSession> AttendanceSessions { get; set; } = new List<AttendanceSession>();
}
