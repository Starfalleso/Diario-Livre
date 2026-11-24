namespace DiarioLivre.Core.Entities;

/// <summary>
/// Represents a student's score on an assessment. A null score indicates a pending evaluation.
/// </summary>
public class Grade
{
    public Guid Id { get; set; }

    public Guid AssessmentId { get; set; }

    public Guid StudentId { get; set; }

    public decimal? Score { get; set; }

    public Assessment? Assessment { get; set; }

    public Student? Student { get; set; }
}
