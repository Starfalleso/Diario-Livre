using DiarioLivre.Core.Entities;
using DiarioLivre.Core.Enums;
using DiarioLivre.Core.Interfaces;
using DiarioLivre.Core.Models;

namespace DiarioLivre.Core.Services;

/// <summary>
/// Pure domain calculator for term and annual grades supporting simple sums,
/// weighted averages, pending assessments, and configurable recovery policies.
/// </summary>
public sealed class GradeCalculator : IGradeCalculator
{
    /// <inheritdoc />
    public TermGradeResult CalculateTermGrade(
        int termNumber,
        IEnumerable<Assessment> assessments,
        IEnumerable<Grade> studentGrades,
        GradeCalculationOptions options,
        decimal? termPassingGrade = null)
    {
        ArgumentNullException.ThrowIfNull(assessments);
        ArgumentNullException.ThrowIfNull(studentGrades);
        ArgumentNullException.ThrowIfNull(options);

        var termAssessments = assessments
            .Where(a => a.TermNumber == termNumber)
            .ToList();

        var gradeByAssessmentId = studentGrades
            .GroupBy(g => g.AssessmentId)
            .ToDictionary(g => g.Key, g => g.First().Score);

        var regularAssessments = termAssessments.Where(a => !a.IsRecovery).ToList();
        var recoveryAssessments = termAssessments.Where(a => a.IsRecovery).ToList();

        bool hasPendingAssessments = regularAssessments.Any(a =>
            !gradeByAssessmentId.TryGetValue(a.Id, out decimal? score) || !score.HasValue);

        var gradedRegular = new List<(Assessment Assessment, decimal Score)>();
        foreach (Assessment assessment in regularAssessments)
        {
            if (gradeByAssessmentId.TryGetValue(assessment.Id, out decimal? score) && score.HasValue)
            {
                gradedRegular.Add((assessment, score.Value));
            }
        }

        decimal maxPossibleScore = ComputeMaxPossibleScore(regularAssessments, recoveryAssessments, options);

        (Assessment Assessment, decimal Score)? bestRecovery = FindBestRecoveryGrade(recoveryAssessments, gradeByAssessmentId);

        if (gradedRegular.Count == 0)
        {
            if (bestRecovery is null)
            {
                return new TermGradeResult(
                    termNumber,
                    null,
                    RoundScore(maxPossibleScore, options.DecimalPlaces),
                    hasPendingAssessments,
                    RecoveryApplied: false,
                    IsPassing: null);
            }

            decimal recoveryOnlyScore = RoundScore(bestRecovery.Value.Score, options.DecimalPlaces);
            decimal passingThresholdOnly = termPassingGrade ?? options.PassingGrade;

            return new TermGradeResult(
                termNumber,
                recoveryOnlyScore,
                RoundScore(maxPossibleScore, options.DecimalPlaces),
                hasPendingAssessments,
                RecoveryApplied: true,
                IsPassing: recoveryOnlyScore >= passingThresholdOnly);
        }

        decimal currentScore = ComputeRegularScore(gradedRegular, options.Mode);
        bool recoveryApplied = false;

        if (bestRecovery is not null)
        {
            decimal recoveryScore = bestRecovery.Value.Score;

            switch (options.RecoveryPolicy)
            {
                case RecoveryPolicy.ReplaceLowest:
                    if (options.Mode == GradeCalculationMode.SimpleSum)
                    {
                        decimal regularMaxSum = regularAssessments.Sum(a => a.MaxScore);
                        if (bestRecovery.Value.Assessment.MaxScore >= regularMaxSum && regularMaxSum > 0m)
                        {
                            if (recoveryScore > currentScore)
                            {
                                currentScore = recoveryScore;
                                recoveryApplied = true;
                            }
                        }
                        else
                        {
                            decimal? replaced = TryReplaceLowest(gradedRegular, recoveryScore, options.Mode);
                            if (replaced.HasValue && replaced.Value > currentScore)
                            {
                                currentScore = replaced.Value;
                                recoveryApplied = true;
                            }
                        }
                    }
                    else
                    {
                        decimal? replaced = TryReplaceLowest(gradedRegular, recoveryScore, options.Mode);
                        if (replaced.HasValue && replaced.Value > currentScore)
                        {
                            currentScore = replaced.Value;
                            recoveryApplied = true;
                        }
                    }

                    break;

                case RecoveryPolicy.AverageWithRecovery:
                    decimal averaged = (currentScore + recoveryScore) / 2m;
                    if (averaged > currentScore)
                    {
                        currentScore = averaged;
                        recoveryApplied = true;
                    }

                    break;
            }
        }

        decimal roundedScore = RoundScore(currentScore, options.DecimalPlaces);
        decimal passingThreshold = termPassingGrade ?? options.PassingGrade;

        return new TermGradeResult(
            termNumber,
            roundedScore,
            RoundScore(maxPossibleScore, options.DecimalPlaces),
            hasPendingAssessments,
            recoveryApplied,
            IsPassing: roundedScore >= passingThreshold);
    }

    /// <inheritdoc />
    public FinalGradeResult CalculateFinalGrade(
        GradingSystem gradingSystem,
        IEnumerable<Assessment> assessments,
        IEnumerable<Grade> studentGrades,
        GradeCalculationOptions options)
    {
        ArgumentNullException.ThrowIfNull(assessments);
        ArgumentNullException.ThrowIfNull(studentGrades);
        ArgumentNullException.ThrowIfNull(options);

        var assessmentList = assessments.ToList();
        var gradeList = studentGrades.ToList();

        int termCount = gradingSystem.GetTermCount();
        decimal termPassingGrade = options.Mode == GradeCalculationMode.SimpleSum
            ? Math.Round(options.PassingGrade / termCount, options.DecimalPlaces, MidpointRounding.AwayFromZero)
            : options.PassingGrade;

        var termResults = new List<TermGradeResult>(termCount);
        for (int term = 1; term <= termCount; term++)
        {
            termResults.Add(CalculateTermGrade(term, assessmentList, gradeList, options, termPassingGrade));
        }

        var gradedTerms = termResults.Where(t => t.Score.HasValue).ToList();
        bool hasAllTermsGraded = gradedTerms.Count == termCount;

        if (gradedTerms.Count == 0)
        {
            return new FinalGradeResult(
                FinalScore: null,
                TermResults: termResults,
                IsPassing: false,
                HasAllTermsGraded: false);
        }

        decimal rawFinal = options.Mode == GradeCalculationMode.SimpleSum
            ? gradedTerms.Sum(t => t.Score!.Value)
            : gradedTerms.Average(t => t.Score!.Value);

        decimal finalScore = RoundScore(rawFinal, options.DecimalPlaces);

        return new FinalGradeResult(
            FinalScore: finalScore,
            TermResults: termResults,
            IsPassing: finalScore >= options.PassingGrade,
            HasAllTermsGraded: hasAllTermsGraded);
    }

    private static decimal ComputeMaxPossibleScore(
        List<Assessment> regularAssessments,
        List<Assessment> recoveryAssessments,
        GradeCalculationOptions options)
    {
        if (regularAssessments.Count == 0)
        {
            return recoveryAssessments.Select(a => a.MaxScore).DefaultIfEmpty(0m).Max();
        }

        if (options.Mode == GradeCalculationMode.SimpleSum)
        {
            return regularAssessments.Sum(a => a.MaxScore);
        }

        decimal totalWeight = regularAssessments.Where(a => a.Weight > 0m).Sum(a => a.Weight);
        if (totalWeight <= 0m)
        {
            return regularAssessments.Average(a => a.MaxScore);
        }

        return regularAssessments
            .Where(a => a.Weight > 0m)
            .Sum(a => a.MaxScore * a.Weight) / totalWeight;
    }

    private static (Assessment Assessment, decimal Score)? FindBestRecoveryGrade(
        List<Assessment> recoveryAssessments,
        Dictionary<Guid, decimal?> gradeByAssessmentId)
    {
        (Assessment Assessment, decimal Score)? best = null;
        foreach (Assessment recovery in recoveryAssessments)
        {
            if (gradeByAssessmentId.TryGetValue(recovery.Id, out decimal? score) && score.HasValue)
            {
                if (best is null || score.Value > best.Value.Score)
                {
                    best = (recovery, score.Value);
                }
            }
        }

        return best;
    }

    private static decimal ComputeRegularScore(
        List<(Assessment Assessment, decimal Score)> gradedItems,
        GradeCalculationMode mode)
    {
        if (mode == GradeCalculationMode.SimpleSum)
        {
            return gradedItems.Sum(x => x.Score);
        }

        decimal totalWeight = gradedItems
            .Where(x => x.Assessment.Weight > 0m)
            .Sum(x => x.Assessment.Weight);

        if (totalWeight <= 0m)
        {
            return gradedItems.Average(x => x.Score);
        }

        return gradedItems
            .Where(x => x.Assessment.Weight > 0m)
            .Sum(x => x.Score * x.Assessment.Weight) / totalWeight;
    }

    private static decimal? TryReplaceLowest(
        List<(Assessment Assessment, decimal Score)> gradedRegular,
        decimal recoveryScore,
        GradeCalculationMode mode)
    {
        if (gradedRegular.Count == 0)
        {
            return null;
        }

        int lowestIndex = 0;
        for (int i = 1; i < gradedRegular.Count; i++)
        {
            var current = gradedRegular[i];
            var lowest = gradedRegular[lowestIndex];

            if (current.Score < lowest.Score ||
                (current.Score == lowest.Score && current.Assessment.Weight > lowest.Assessment.Weight))
            {
                lowestIndex = i;
            }
        }

        if (recoveryScore <= gradedRegular[lowestIndex].Score)
        {
            return null;
        }

        var updated = new List<(Assessment Assessment, decimal Score)>(gradedRegular);
        updated[lowestIndex] = (updated[lowestIndex].Assessment, recoveryScore);

        return ComputeRegularScore(updated, mode);
    }

    private static decimal RoundScore(decimal value, int decimalPlaces)
        => Math.Round(value, decimalPlaces, MidpointRounding.AwayFromZero);
}
