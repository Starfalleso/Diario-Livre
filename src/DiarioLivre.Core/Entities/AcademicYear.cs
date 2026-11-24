using DiarioLivre.Core.Enums;

namespace DiarioLivre.Core.Entities;

/// <summary>
/// Represents a school academic year and its grading and attendance configuration.
/// </summary>
public class AcademicYear
{
    public Guid Id { get; set; }

    public int Year { get; set; }

    public string SchoolName { get; set; } = string.Empty;

    public GradingSystem GradingSystem { get; set; } = GradingSystem.Bimonthly;

    public decimal PassingGrade { get; set; }

    public decimal MinAttendancePercentage { get; set; } = 75.0m;

    public int TargetSchoolDays { get; set; } = 200;

    public GradeCalculationMode GradeCalculationMode { get; set; } = GradeCalculationMode.WeightedAverage;

    public RecoveryPolicy RecoveryPolicy { get; set; } = RecoveryPolicy.ReplaceLowest;

    public ICollection<Classroom> Classrooms { get; set; } = new List<Classroom>();

    public ICollection<SchoolCalendarEvent> CalendarEvents { get; set; } = new List<SchoolCalendarEvent>();
}
