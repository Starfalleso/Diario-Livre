using DiarioLivre.Core.Enums;

namespace DiarioLivre.Core.Entities;

/// <summary>
/// Represents a calendar exception or special school day (holiday, recess, school Saturday, etc.).
/// </summary>
public class SchoolCalendarEvent
{
    public Guid Id { get; set; }

    public Guid AcademicYearId { get; set; }

    public DateOnly Date { get; set; }

    public SchoolCalendarEventType Type { get; set; }

    public string Title { get; set; } = string.Empty;

    public int TermNumber { get; set; } = 1;

    public string? Notes { get; set; }

    public AcademicYear? AcademicYear { get; set; }
}
