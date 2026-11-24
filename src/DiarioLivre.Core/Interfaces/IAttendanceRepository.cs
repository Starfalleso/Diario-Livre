using DiarioLivre.Core.Entities;

namespace DiarioLivre.Core.Interfaces;

/// <summary>
/// Repository contract for managing daily and subject-based attendance sessions.
/// </summary>
public interface IAttendanceRepository
{
    Task<IReadOnlyList<AttendanceSession>> GetSessionsAsync(
        Guid classroomId,
        Guid? subjectId = null,
        CancellationToken ct = default);

    Task<AttendanceSession?> GetSessionByDateAsync(
        Guid classroomId,
        Guid? subjectId,
        DateOnly date,
        CancellationToken ct = default);

    Task SaveSessionAsync(AttendanceSession session, CancellationToken ct = default);

    Task DeleteSessionAsync(Guid sessionId, CancellationToken ct = default);
}
