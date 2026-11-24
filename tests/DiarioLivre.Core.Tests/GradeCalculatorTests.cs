using DiarioLivre.Core.Entities;
using DiarioLivre.Core.Enums;
using DiarioLivre.Core.Models;
using DiarioLivre.Core.Services;

namespace DiarioLivre.Core.Tests;

public sealed class GradeCalculatorTests
{
    private readonly GradeCalculator _calculator = new();
    private static readonly Guid SubjectId = Guid.NewGuid();
    private static readonly Guid StudentId = Guid.NewGuid();

    [Fact]
    public void CalculateFinalGrade_SimpleSumBimonthly_PassingAndFailing()
    {
        var assessments = new List<Assessment>
        {
            CreateAssessment(1, "B1 Exam", 15m),
            CreateAssessment(1, "B1 Project", 10m),
            CreateAssessment(2, "B2 Exam", 25m),
            CreateAssessment(3, "B3 Exam", 25m),
            CreateAssessment(4, "B4 Exam", 25m)
        };

        var passingGrades = new List<Grade>
        {
            CreateGrade(assessments[0].Id, 10m),
            CreateGrade(assessments[1].Id, 8m),
            CreateGrade(assessments[2].Id, 17m),
            CreateGrade(assessments[3].Id, 15m),
            CreateGrade(assessments[4].Id, 16m)
        };

        var options = new GradeCalculationOptions(
            Mode: GradeCalculationMode.SimpleSum,
            RecoveryPolicy: RecoveryPolicy.ReplaceLowest,
            PassingGrade: 60.0m);

        FinalGradeResult passingResult = _calculator.CalculateFinalGrade(
            GradingSystem.Bimonthly, assessments, passingGrades, options);

        Assert.Equal(66.0m, passingResult.FinalScore);
        Assert.True(passingResult.IsPassing);
        Assert.True(passingResult.HasAllTermsGraded);
        Assert.Equal(4, passingResult.TermResults.Count);
        Assert.Equal(18.0m, passingResult.TermResults[0].Score);

        var failingGrades = new List<Grade>
        {
            CreateGrade(assessments[0].Id, 8m),
            CreateGrade(assessments[1].Id, 5m),
            CreateGrade(assessments[2].Id, 12m),
            CreateGrade(assessments[3].Id, 14m),
            CreateGrade(assessments[4].Id, 15m)
        };

        FinalGradeResult failingResult = _calculator.CalculateFinalGrade(
            GradingSystem.Bimonthly, assessments, failingGrades, options);

        Assert.Equal(54.0m, failingResult.FinalScore);
        Assert.False(failingResult.IsPassing);
        Assert.True(failingResult.HasAllTermsGraded);
    }

    [Fact]
    public void CalculateTermGrade_WeightedAverageWithUnequalWeights_ComputesCorrectly()
    {
        var assessments = new List<Assessment>
        {
            CreateAssessment(1, "Quiz", 10m, weight: 1m),
            CreateAssessment(1, "Lab", 10m, weight: 2m),
            CreateAssessment(1, "Final Exam", 10m, weight: 3m)
        };

        var grades = new List<Grade>
        {
            CreateGrade(assessments[0].Id, 6m),
            CreateGrade(assessments[1].Id, 9m),
            CreateGrade(assessments[2].Id, 8m)
        };

        var options = new GradeCalculationOptions(
            Mode: GradeCalculationMode.WeightedAverage,
            PassingGrade: 6.0m);

        TermGradeResult result = _calculator.CalculateTermGrade(1, assessments, grades, options);

        // (6*1 + 9*2 + 8*3) / (1 + 2 + 3) = 48 / 6 = 8.00
        Assert.Equal(8.00m, result.Score);
        Assert.False(result.HasPendingAssessments);
        Assert.False(result.RecoveryApplied);
        Assert.True(result.IsPassing);
    }

    [Fact]
    public void CalculateTermGrade_NullPendingGradeExcludedVsZeroScoreIncluded()
    {
        var assessments = new List<Assessment>
        {
            CreateAssessment(1, "Assessment 1", 10m),
            CreateAssessment(1, "Assessment 2", 10m)
        };

        var pendingGrades = new List<Grade>
        {
            CreateGrade(assessments[0].Id, 8m),
            CreateGrade(assessments[1].Id, null)
        };

        var zeroGrades = new List<Grade>
        {
            CreateGrade(assessments[0].Id, 8m),
            CreateGrade(assessments[1].Id, 0m)
        };

        var options = new GradeCalculationOptions(
            Mode: GradeCalculationMode.WeightedAverage,
            PassingGrade: 6.0m);

        TermGradeResult pendingResult = _calculator.CalculateTermGrade(1, assessments, pendingGrades, options);
        TermGradeResult zeroResult = _calculator.CalculateTermGrade(1, assessments, zeroGrades, options);

        Assert.Equal(8.00m, pendingResult.Score);
        Assert.True(pendingResult.HasPendingAssessments);
        Assert.True(pendingResult.IsPassing);

        Assert.Equal(4.00m, zeroResult.Score);
        Assert.False(zeroResult.HasPendingAssessments);
        Assert.False(zeroResult.IsPassing);
    }

    [Fact]
    public void CalculateTermGrade_ReplaceLowestInWeightedAverage_OnlyReplacesWhenHigher()
    {
        var assessments = new List<Assessment>
        {
            CreateAssessment(1, "Exam 1", 10m, weight: 1m),
            CreateAssessment(1, "Exam 2", 10m, weight: 1m),
            CreateAssessment(1, "Recovery", 10m, weight: 1m, isRecovery: true)
        };

        var improvedGrades = new List<Grade>
        {
            CreateGrade(assessments[0].Id, 3m),
            CreateGrade(assessments[1].Id, 8m),
            CreateGrade(assessments[2].Id, 7m)
        };

        var options = new GradeCalculationOptions(
            Mode: GradeCalculationMode.WeightedAverage,
            RecoveryPolicy: RecoveryPolicy.ReplaceLowest,
            PassingGrade: 6.0m);

        TermGradeResult improved = _calculator.CalculateTermGrade(1, assessments, improvedGrades, options);
        Assert.Equal(7.50m, improved.Score);
        Assert.True(improved.RecoveryApplied);
        Assert.True(improved.IsPassing);

        var lowerRecoveryGrades = new List<Grade>
        {
            CreateGrade(assessments[0].Id, 5m),
            CreateGrade(assessments[1].Id, 8m),
            CreateGrade(assessments[2].Id, 4m)
        };

        TermGradeResult unchanged = _calculator.CalculateTermGrade(1, assessments, lowerRecoveryGrades, options);
        Assert.Equal(6.50m, unchanged.Score);
        Assert.False(unchanged.RecoveryApplied);
    }

    [Fact]
    public void CalculateTermGrade_ReplaceLowestInSimpleSum_SupportsSingleAndFullTermRecovery()
    {
        var singleRecoveryAssessments = new List<Assessment>
        {
            CreateAssessment(1, "Part A", 10m),
            CreateAssessment(1, "Part B", 15m),
            CreateAssessment(1, "Recovery A", 10m, isRecovery: true)
        };

        var singleGrades = new List<Grade>
        {
            CreateGrade(singleRecoveryAssessments[0].Id, 4m),
            CreateGrade(singleRecoveryAssessments[1].Id, 13m),
            CreateGrade(singleRecoveryAssessments[2].Id, 9m)
        };

        var options = new GradeCalculationOptions(
            Mode: GradeCalculationMode.SimpleSum,
            RecoveryPolicy: RecoveryPolicy.ReplaceLowest,
            PassingGrade: 60.0m);

        TermGradeResult singleResult = _calculator.CalculateTermGrade(
            1, singleRecoveryAssessments, singleGrades, options, termPassingGrade: 15.0m);

        Assert.Equal(22.0m, singleResult.Score);
        Assert.True(singleResult.RecoveryApplied);
        Assert.True(singleResult.IsPassing);

        var fullTermAssessments = new List<Assessment>
        {
            CreateAssessment(1, "Part A", 10m),
            CreateAssessment(1, "Part B", 15m),
            CreateAssessment(1, "Full Recovery", 25m, isRecovery: true)
        };

        var fullTermGrades = new List<Grade>
        {
            CreateGrade(fullTermAssessments[0].Id, 4m),
            CreateGrade(fullTermAssessments[1].Id, 8m),
            CreateGrade(fullTermAssessments[2].Id, 18m)
        };

        TermGradeResult fullTermResult = _calculator.CalculateTermGrade(
            1, fullTermAssessments, fullTermGrades, options, termPassingGrade: 15.0m);

        Assert.Equal(18.0m, fullTermResult.Score);
        Assert.True(fullTermResult.RecoveryApplied);
        Assert.True(fullTermResult.IsPassing);
    }

    [Fact]
    public void CalculateTermGrade_AverageWithRecovery_AveragesOnlyWhenBeneficial()
    {
        var assessments = new List<Assessment>
        {
            CreateAssessment(1, "Exam 1", 10m),
            CreateAssessment(1, "Exam 2", 10m),
            CreateAssessment(1, "Recovery", 10m, isRecovery: true)
        };

        var options = new GradeCalculationOptions(
            Mode: GradeCalculationMode.WeightedAverage,
            RecoveryPolicy: RecoveryPolicy.AverageWithRecovery,
            PassingGrade: 6.0m);

        var improvedGrades = new List<Grade>
        {
            CreateGrade(assessments[0].Id, 4m),
            CreateGrade(assessments[1].Id, 6m),
            CreateGrade(assessments[2].Id, 8m)
        };

        // Regular average = 5.0; averaged with 8.0 => 6.50
        TermGradeResult improved = _calculator.CalculateTermGrade(1, assessments, improvedGrades, options);
        Assert.Equal(6.50m, improved.Score);
        Assert.True(improved.RecoveryApplied);
        Assert.True(improved.IsPassing);

        var worseRecoveryGrades = new List<Grade>
        {
            CreateGrade(assessments[0].Id, 7m),
            CreateGrade(assessments[1].Id, 9m),
            CreateGrade(assessments[2].Id, 4m)
        };

        // Regular average = 8.0; averaged with 4.0 would be 6.0 -> never penalizes
        TermGradeResult kept = _calculator.CalculateTermGrade(1, assessments, worseRecoveryGrades, options);
        Assert.Equal(8.00m, kept.Score);
        Assert.False(kept.RecoveryApplied);
    }

    [Theory]
    [InlineData(GradingSystem.Trimester, 3)]
    [InlineData(GradingSystem.Semester, 2)]
    public void CalculateFinalGrade_TrimesterAndSemester_ProducesExpectedTermCountAndAverage(
        GradingSystem gradingSystem,
        int expectedTerms)
    {
        var assessments = Enumerable.Range(1, expectedTerms)
            .Select(term => CreateAssessment(term, $"Exam T{term}", 10m))
            .ToList();

        var grades = assessments
            .Select((a, index) => CreateGrade(a.Id, 7m + index))
            .ToList();

        var options = new GradeCalculationOptions(
            Mode: GradeCalculationMode.WeightedAverage,
            PassingGrade: 6.0m);

        FinalGradeResult result = _calculator.CalculateFinalGrade(gradingSystem, assessments, grades, options);

        Assert.Equal(expectedTerms, result.TermResults.Count);
        Assert.True(result.HasAllTermsGraded);
        Assert.True(result.IsPassing);
        Assert.Equal(Math.Round(grades.Average(g => g.Score!.Value), 2), result.FinalScore);
    }

    [Fact]
    public void CalculateGrades_EmptyAssessmentsOrAllNullGrades_ReturnsNullScoreAndNotPassing()
    {
        var options = new GradeCalculationOptions(
            Mode: GradeCalculationMode.WeightedAverage,
            PassingGrade: 6.0m);

        FinalGradeResult emptyResult = _calculator.CalculateFinalGrade(
            GradingSystem.Bimonthly,
            Array.Empty<Assessment>(),
            Array.Empty<Grade>(),
            options);

        Assert.Null(emptyResult.FinalScore);
        Assert.False(emptyResult.IsPassing);
        Assert.False(emptyResult.HasAllTermsGraded);

        var assessment = CreateAssessment(1, "Exam 1", 10m);
        var nullGrades = new[] { CreateGrade(assessment.Id, null) };

        TermGradeResult termResult = _calculator.CalculateTermGrade(1, new[] { assessment }, nullGrades, options);
        FinalGradeResult allNullFinal = _calculator.CalculateFinalGrade(
            GradingSystem.Bimonthly, new[] { assessment }, nullGrades, options);

        Assert.Null(termResult.Score);
        Assert.Null(termResult.IsPassing);
        Assert.True(termResult.HasPendingAssessments);
        Assert.Null(allNullFinal.FinalScore);
        Assert.False(allNullFinal.IsPassing);
    }

    [Fact]
    public void CalculateTermGrade_MultipleRecoveryAssessments_SelectsHighestRecoveryScore()
    {
        var assessments = new List<Assessment>
        {
            CreateAssessment(1, "Exam 1", 10m),
            CreateAssessment(1, "Exam 2", 10m),
            CreateAssessment(1, "Recovery 1", 10m, isRecovery: true),
            CreateAssessment(1, "Recovery 2", 10m, isRecovery: true)
        };

        var grades = new List<Grade>
        {
            CreateGrade(assessments[0].Id, 3m),
            CreateGrade(assessments[1].Id, 7m),
            CreateGrade(assessments[2].Id, 6m),
            CreateGrade(assessments[3].Id, 9m)
        };

        var options = new GradeCalculationOptions(
            Mode: GradeCalculationMode.WeightedAverage,
            RecoveryPolicy: RecoveryPolicy.ReplaceLowest,
            PassingGrade: 6.0m);

        TermGradeResult result = _calculator.CalculateTermGrade(1, assessments, grades, options);

        // Best recovery (9) replaces lowest regular (3) -> (9 + 7) / 2 = 8.00
        Assert.Equal(8.00m, result.Score);
        Assert.True(result.RecoveryApplied);
        Assert.True(result.IsPassing);
    }

    [Fact]
    public void CalculateTermGrade_AllRegularPendingWithRecoveryGrade_UsesRecoveryScore()
    {
        var assessments = new List<Assessment>
        {
            CreateAssessment(1, "Exam 1", 10m),
            CreateAssessment(1, "Recovery", 10m, isRecovery: true)
        };

        var grades = new List<Grade>
        {
            CreateGrade(assessments[0].Id, null),
            CreateGrade(assessments[1].Id, 7.5m)
        };

        var options = new GradeCalculationOptions(
            Mode: GradeCalculationMode.WeightedAverage,
            PassingGrade: 6.0m);

        TermGradeResult result = _calculator.CalculateTermGrade(1, assessments, grades, options);

        Assert.Equal(7.50m, result.Score);
        Assert.True(result.HasPendingAssessments);
        Assert.True(result.RecoveryApplied);
        Assert.True(result.IsPassing);
    }

    [Fact]
    public void CalculateTermGrade_ZeroWeightAssessments_FallsBackToSimpleAverageWithoutDivisionByZero()
    {
        var assessments = new List<Assessment>
        {
            CreateAssessment(1, "Activity 1", 10m, weight: 0m),
            CreateAssessment(1, "Activity 2", 10m, weight: 0m)
        };

        var grades = new List<Grade>
        {
            CreateGrade(assessments[0].Id, 6m),
            CreateGrade(assessments[1].Id, 8m)
        };

        var options = new GradeCalculationOptions(
            Mode: GradeCalculationMode.WeightedAverage,
            PassingGrade: 6.0m);

        TermGradeResult result = _calculator.CalculateTermGrade(1, assessments, grades, options);

        Assert.Equal(7.00m, result.Score);
        Assert.Equal(10.00m, result.MaxPossibleScore);
        Assert.True(result.IsPassing);
    }

    private static Assessment CreateAssessment(
        int termNumber,
        string title,
        decimal maxScore,
        decimal weight = 1.0m,
        bool isRecovery = false) => new()
    {
        Id = Guid.NewGuid(),
        SubjectId = SubjectId,
        TermNumber = termNumber,
        Title = title,
        MaxScore = maxScore,
        Weight = weight,
        IsRecovery = isRecovery
    };

    private static Grade CreateGrade(Guid assessmentId, decimal? score) => new()
    {
        Id = Guid.NewGuid(),
        AssessmentId = assessmentId,
        StudentId = StudentId,
        Score = score
    };
}
