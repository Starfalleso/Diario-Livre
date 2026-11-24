namespace DiarioLivre.Core.Models;

/// <summary>
/// Column header metadata for a single assessment in a class gradebook PDF report.
/// </summary>
public sealed record ClassGradebookAssessmentColumn(
    string Title,
    decimal MaxScore,
    decimal Weight,
    bool IsRecovery);

/// <summary>
/// Row data for a single student in a class gradebook PDF report.
/// </summary>
public sealed record ClassGradebookStudentRow(
    int RollNumber,
    string FullName,
    IReadOnlyList<string> AssessmentScores,
    string TermScoreText,
    string FinalScoreText,
    string StatusText,
    bool IsBelowPassing,
    bool RecoveryApplied);

/// <summary>
/// Localized labels for rendering a class gradebook PDF report.
/// </summary>
public sealed record ClassGradebookReportLabels(
    string ReportTitle = "Class Gradebook Report",
    string RollColumn = "#",
    string StudentColumn = "Student",
    string TermScoreColumn = "Term Score",
    string FinalScoreColumn = "Final",
    string StatusColumn = "Status",
    string ClassAverageLabel = "Class Average",
    string PassingLabel = "Passing",
    string BelowPassingLabel = "Below Passing",
    string PendingLabel = "Pending",
    string GeneratedAtLabel = "Generated on")
{
    public static ClassGradebookReportLabels Default { get; } = new();
}

/// <summary>
/// Aggregated payload required to render a printable landscape class gradebook PDF.
/// </summary>
public sealed record ClassGradebookReportData(
    string SchoolName,
    int Year,
    string ClassroomName,
    string SubjectName,
    string TermLabel,
    string ClassAverageText,
    int PassingCount,
    int BelowPassingCount,
    int PendingCount,
    IReadOnlyList<ClassGradebookAssessmentColumn> Assessments,
    IReadOnlyList<ClassGradebookStudentRow> Rows,
    ClassGradebookReportLabels? Labels = null);
