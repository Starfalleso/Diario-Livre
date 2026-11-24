using DiarioLivre.Core.Enums;

namespace DiarioLivre.Core.Models;

/// <summary>
/// Configuration parameters controlling how term and final grades are computed.
/// </summary>
public sealed record GradeCalculationOptions(
    GradeCalculationMode Mode = GradeCalculationMode.WeightedAverage,
    RecoveryPolicy RecoveryPolicy = RecoveryPolicy.ReplaceLowest,
    decimal PassingGrade = 6.0m,
    int DecimalPlaces = 2);
