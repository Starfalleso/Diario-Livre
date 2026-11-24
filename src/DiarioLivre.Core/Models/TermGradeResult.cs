namespace DiarioLivre.Core.Models;

/// <summary>
/// Represents the computed grade outcome for a single academic term.
/// </summary>
public sealed record TermGradeResult(
    int TermNumber,
    decimal? Score,
    decimal MaxPossibleScore,
    bool HasPendingAssessments,
    bool RecoveryApplied,
    bool? IsPassing);
