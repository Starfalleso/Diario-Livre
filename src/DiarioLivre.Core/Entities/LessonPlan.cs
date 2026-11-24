using DiarioLivre.Core.Enums;

namespace DiarioLivre.Core.Entities;

/// <summary>
/// Represents a planned lesson with optional BNCC alignment that can be applied directly to a roll call.
/// </summary>
public class LessonPlan
{
    public Guid Id { get; set; }

    public Guid ClassroomId { get; set; }

    public Guid? SubjectId { get; set; }

    public int TermNumber { get; set; } = 1;

    public DateOnly? PlannedDate { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? BnccCode { get; set; }

    public string? Objectives { get; set; }

    public string? PlannedActivity { get; set; }

    public string? Homework { get; set; }

    public LessonPlanStatus Status { get; set; } = LessonPlanStatus.Planned;

    public Classroom? Classroom { get; set; }

    public Subject? Subject { get; set; }
}
