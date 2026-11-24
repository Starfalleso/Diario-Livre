using DiarioLivre.Core.Entities;

namespace DiarioLivre.Core.Models;

/// <summary>
/// Represents a subject's term and final grade breakdown for PDF report generation.
/// </summary>
public sealed record SubjectGradeReportItem(
    string SubjectName,
    FinalGradeResult FinalResult);

/// <summary>
/// Represents a single observation entry formatted for PDF report generation.
/// </summary>
public sealed record ObservationReportItem(
    DateOnly Date,
    string CategoryLabel,
    string Content);

/// <summary>
/// Localized section and column labels rendered inside a student dossier PDF report.
/// </summary>
public sealed record PdfReportLabels(
    string SpecialNeedsBanner = "PDI / Special Educational Needs Flag Active",
    string AttendanceSummaryTitle = "LDB 75% Attendance Summary",
    string TotalLessonsLabel = "Total Lessons",
    string PresentLabel = "Present",
    string UnexcusedLabel = "Unexcused",
    string JustifiedLabel = "Justified",
    string AttendanceRateLabel = "Attendance",
    string AbsencesRateLabel = "Absences",
    string StatusLabel = "Status",
    string? AlertStatusDisplay = null,
    string AcademicPerformanceTitle = "Academic Performance",
    string SubjectColumn = "Subject",
    string FinalColumn = "Final",
    string TermShortPrefix = "T",
    string NoSubjectsText = "No subjects registered.",
    string ObservationsTitle = "Observations Timeline",
    string NoObservationsText = "No observations recorded.")
{
    public static PdfReportLabels Default { get; } = new();
}

/// <summary>
/// Aggregated payload required to render a student dossier PDF report.
/// </summary>
public sealed record StudentDossierReportData(
    string SchoolName,
    int Year,
    string ClassroomName,
    Student Student,
    string? AvatarFilePath,
    AttendanceSummary AttendanceSummary,
    IReadOnlyList<SubjectGradeReportItem> SubjectGrades,
    IReadOnlyList<ObservationReportItem> Observations,
    string ReportTitle = "Student Dossier / Class Council Report",
    string GeneratedAtLabel = "Generated on",
    PdfReportLabels? Labels = null);
