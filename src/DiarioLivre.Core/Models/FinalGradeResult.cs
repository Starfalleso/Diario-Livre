namespace DiarioLivre.Core.Models;

/// <summary>
/// Represents the consolidated final grade across all terms of an academic year.
/// </summary>
public sealed record FinalGradeResult(
    decimal? FinalScore,
    IReadOnlyList<TermGradeResult> TermResults,
    bool IsPassing,
    bool HasAllTermsGraded);
