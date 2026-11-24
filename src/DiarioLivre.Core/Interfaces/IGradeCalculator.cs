using DiarioLivre.Core.Entities;
using DiarioLivre.Core.Enums;
using DiarioLivre.Core.Models;

namespace DiarioLivre.Core.Interfaces;

/// <summary>
/// Pure domain service for computing term and annual grades with recovery rules.
/// </summary>
public interface IGradeCalculator
{
    /// <summary>
    /// Calculates the grade for a specific term given assessments, student grades, and calculation options.
    /// </summary>
    TermGradeResult CalculateTermGrade(
        int termNumber,
        IEnumerable<Assessment> assessments,
        IEnumerable<Grade> studentGrades,
        GradeCalculationOptions options,
        decimal? termPassingGrade = null);

    /// <summary>
    /// Calculates the consolidated final grade across all terms defined by the grading system.
    /// </summary>
    FinalGradeResult CalculateFinalGrade(
        GradingSystem gradingSystem,
        IEnumerable<Assessment> assessments,
        IEnumerable<Grade> studentGrades,
        GradeCalculationOptions options);
}
