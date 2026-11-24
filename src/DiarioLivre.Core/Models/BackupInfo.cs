namespace DiarioLivre.Core.Models;

/// <summary>
/// Snapshot of offline SQLite database metrics and local media storage usage.
/// </summary>
public sealed record BackupInfo(
    string DatabasePath,
    long DatabaseSizeBytes,
    int AvatarFilesCount,
    int AcademicYearsCount,
    int ClassroomsCount,
    int StudentsCount,
    int GradesCount,
    int AttendanceSessionsCount,
    int ObservationsCount);
