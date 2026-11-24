namespace DiarioLivre.Core.Entities;

/// <summary>
/// Represents a graded evaluation or recovery exam in a subject term.
/// </summary>
public class Assessment
{
    public Guid Id { get; set; }

    public Guid SubjectId { get; set; }

    public int TermNumber { get; set; }

    public string Title { get; set; } = string.Empty;

    public decimal MaxScore { get; set; }

    public decimal Weight { get; set; } = 1.0m;

    public bool IsRecovery { get; set; }

    public Subject? Subject { get; set; }

    public ICollection<Grade> Grades { get; set; } = new List<Grade>();
}
