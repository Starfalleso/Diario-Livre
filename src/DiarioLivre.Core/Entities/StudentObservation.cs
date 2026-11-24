using DiarioLivre.Core.Enums;

namespace DiarioLivre.Core.Entities;

/// <summary>
/// Represents a dated pedagogical, behavioral, praise, or family-contact note for a student.
/// </summary>
public class StudentObservation
{
    public Guid Id { get; set; }

    public Guid StudentId { get; set; }

    public DateOnly Date { get; set; }

    public ObservationCategory Category { get; set; } = ObservationCategory.Pedagogical;

    public string Content { get; set; } = string.Empty;

    public Student? Student { get; set; }
}
