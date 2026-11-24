namespace DiarioLivre.Core.Models;

/// <summary>
/// Summary of recorded vs. target school days for an academic year or period under LDB 200-day rules.
/// </summary>
public sealed record SchoolDaysSummary(
    int RecordedDays,
    int TargetDays,
    int RemainingDays,
    decimal ProgressPercentage,
    int SchoolSaturdaysCount,
    int NonSchoolDaysCount,
    bool MeetsTarget);
