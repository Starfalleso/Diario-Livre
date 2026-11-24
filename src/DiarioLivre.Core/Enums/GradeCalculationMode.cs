namespace DiarioLivre.Core.Enums;

/// <summary>
/// Specifies how scores from multiple assessments are combined into a term grade.
/// </summary>
public enum GradeCalculationMode
{
    SimpleSum = 1,
    WeightedAverage = 2
}
