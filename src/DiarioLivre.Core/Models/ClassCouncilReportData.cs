using DiarioLivre.Core.Enums;

namespace DiarioLivre.Core.Models;

/// <summary>
/// Consolidated row for a single student in the end-of-period Class Council ("Ata do Conselho de Classe") PDF report.
/// </summary>
public sealed record ClassCouncilStudentRow(
    int RollNumber,
    string FullName,
    bool HasSpecialNeeds,
    decimal AttendancePercentage,
    AttendanceAlertStatus AlertStatus,
    string AlertLabel,
    IReadOnlyList<string> SubjectScores,
    string OverallAverageText,
    bool IsOverallBelowPassing,
    string NotesSummary);

/// <summary>
/// Localized labels for rendering a Class Council ("Conselho de Classe") PDF report.
/// </summary>
public sealed record ClassCouncilReportLabels(
    string ReportTitle = "Class Council Minutes & Summary",
    string RollColumn = "#",
    string StudentColumn = "Student",
    string PdiColumn = "PDI",
    string AttendanceColumn = "Attendance",
    string OverallColumn = "Overall",
    string NotesColumn = "Pedagogical Summary",
    string YesShort = "Yes",
    string NoShort = "—",
    string PassingRuleLabel = "Passing Grade",
    string MinAttendanceLabel = "Min. Attendance",
    string GeneratedAtLabel = "Generated on")
{
    public static ClassCouncilReportLabels Default { get; } = new();
}

/// <summary>
/// Aggregated payload required to render an A4 Landscape Class Council consolidated PDF sheet.
/// </summary>
public sealed record ClassCouncilReportData(
    string SchoolName,
    int Year,
    string ClassroomName,
    string ShiftDisplay,
    string GradingSystemDisplay,
    decimal PassingGrade,
    decimal MinAttendancePercentage,
    IReadOnlyList<string> SubjectNames,
    IReadOnlyList<ClassCouncilStudentRow> Students,
    ClassCouncilReportLabels? Labels = null);
