using DiarioLivre.Core.Enums;

namespace DiarioLivre.Core.Models;

/// <summary>
/// Row data for a single student in a class attendance summary PDF report.
/// </summary>
public sealed record ClassAttendanceStudentRow(
    int RollNumber,
    string FullName,
    int TotalLessons,
    int PresentLessons,
    int UnexcusedAbsences,
    int JustifiedAbsences,
    decimal AttendancePercentage,
    decimal AbsencePercentage,
    AttendanceAlertStatus AlertStatus,
    string AlertLabel);

/// <summary>
/// Summary row for a single recorded lesson session in the attendance log PDF section.
/// </summary>
public sealed record ClassAttendanceSessionItem(
    DateOnly Date,
    int LessonCount,
    string? TopicCovered,
    int PresentCount,
    int AbsentCount,
    int JustifiedCount,
    int TermNumber = 1,
    string? DailyActivity = null,
    string? Homework = null);

/// <summary>
/// Localized labels for rendering a class attendance and lesson log PDF report.
/// </summary>
public sealed record ClassAttendanceReportLabels(
    string ReportTitle = "Class Attendance & Lesson Log Report",
    string StudentsSummaryTitle = "Student Attendance Summary (LDB 75%)",
    string LessonLogTitle = "Lesson & Topic Covered Log",
    string RollColumn = "#",
    string StudentColumn = "Student",
    string TotalLessonsColumn = "Lessons",
    string PresentColumn = "Present",
    string UnexcusedColumn = "Unexcused",
    string JustifiedColumn = "Justified",
    string AttendanceRateColumn = "Attendance",
    string StatusColumn = "Status",
    string DateColumn = "Date",
    string TopicColumn = "Topic Covered",
    string NoSessionsText = "No attendance sessions recorded.",
    string GeneratedAtLabel = "Generated on",
    string DailyActivityColumn = "Daily Activity",
    string HomeworkColumn = "Homework",
    string PeriodLabel = "Period")
{
    public static ClassAttendanceReportLabels Default { get; } = new();
}

/// <summary>
/// Aggregated payload required to render a printable class attendance PDF report.
/// </summary>
public sealed record ClassAttendanceReportData(
    string SchoolName,
    int Year,
    string ClassroomName,
    string SubjectDisplay,
    IReadOnlyList<ClassAttendanceStudentRow> Students,
    IReadOnlyList<ClassAttendanceSessionItem> Sessions,
    ClassAttendanceReportLabels? Labels = null,
    string? PeriodDisplay = null);
