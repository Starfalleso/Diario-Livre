using DiarioLivre.Core.Enums;

namespace DiarioLivre.Core.Models;

/// <summary>
/// Summarizes attendance metrics and LDB 75% alert status for a student.
/// </summary>
public sealed record AttendanceSummary(
    int TotalLessons,
    int PresentLessons,
    int UnexcusedAbsences,
    int JustifiedAbsences,
    decimal AttendancePercentage,
    decimal AbsencePercentage,
    AttendanceAlertStatus AlertStatus,
    bool MeetsLegalMinimum);
