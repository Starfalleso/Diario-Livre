using DiarioLivre.Core.Models;

namespace DiarioLivre.Core.Interfaces;

/// <summary>
/// Service for exporting printable PDF reports from domain data.
/// </summary>
public interface IReportGenerator
{
    Task GenerateStudentDossierPdfAsync(
        StudentDossierReportData data,
        string outputFilePath,
        CancellationToken ct = default);

    Task GenerateClassGradebookPdfAsync(
        ClassGradebookReportData data,
        string outputFilePath,
        CancellationToken ct = default);

    Task GenerateClassAttendancePdfAsync(
        ClassAttendanceReportData data,
        string outputFilePath,
        CancellationToken ct = default);

    Task GenerateBatchStudentDossiersPdfAsync(
        IReadOnlyList<StudentDossierReportData> dossiers,
        string outputFilePath,
        CancellationToken ct = default);

    Task GenerateClassCouncilPdfAsync(
        ClassCouncilReportData data,
        string outputFilePath,
        CancellationToken ct = default);
}
