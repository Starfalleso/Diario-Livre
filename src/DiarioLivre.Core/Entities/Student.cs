namespace DiarioLivre.Core.Entities;

/// <summary>
/// Represents an enrolled student in a classroom.
/// </summary>
public class Student
{
    public Guid Id { get; set; }

    public Guid ClassroomId { get; set; }

    public int RollNumber { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string? PhotoFileName { get; set; }

    /// <summary>
    /// Flags students who have an Individual Development Plan (PDI) or medical report.
    /// </summary>
    public bool HasSpecialNeeds { get; set; }

    public int? SeatRow { get; set; }

    public int? SeatColumn { get; set; }

    public string? GeneralNotes { get; set; }

    public Classroom? Classroom { get; set; }

    public ICollection<Grade> Grades { get; set; } = new List<Grade>();

    public ICollection<AttendanceEntry> AttendanceEntries { get; set; } = new List<AttendanceEntry>();

    public ICollection<StudentObservation> Observations { get; set; } = new List<StudentObservation>();
}
