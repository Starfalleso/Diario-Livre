namespace DiarioLivre.Core.Enums;

/// <summary>
/// Defines the academic grading division for an academic year.
/// </summary>
public enum GradingSystem
{
    Bimonthly = 1,
    Trimester = 2,
    Semester = 3
}

/// <summary>
/// Extension helpers for <see cref="GradingSystem"/>.
/// </summary>
public static class GradingSystemExtensions
{
    /// <summary>
    /// Returns the number of academic terms per year for the specified grading system.
    /// </summary>
    public static int GetTermCount(this GradingSystem gradingSystem) => gradingSystem switch
    {
        GradingSystem.Bimonthly => 4,
        GradingSystem.Trimester => 3,
        GradingSystem.Semester => 2,
        _ => throw new ArgumentOutOfRangeException(nameof(gradingSystem), gradingSystem, "Unsupported grading system.")
    };

    /// <summary>
    /// Returns the calendar-based default term number (1-based) for the given date and grading system.
    /// </summary>
    public static int GetDefaultTermForDate(this GradingSystem gradingSystem, DateOnly date) => gradingSystem switch
    {
        GradingSystem.Semester => date.Month <= 6 ? 1 : 2,
        GradingSystem.Trimester => date.Month <= 4 ? 1 : date.Month <= 8 ? 2 : 3,
        GradingSystem.Bimonthly => date.Month <= 3 ? 1 : date.Month <= 6 ? 2 : date.Month <= 9 ? 3 : 4,
        _ => throw new ArgumentOutOfRangeException(nameof(gradingSystem), gradingSystem, "Unsupported grading system.")
    };
}
