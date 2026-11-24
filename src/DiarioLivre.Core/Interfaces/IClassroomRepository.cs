using DiarioLivre.Core.Entities;

namespace DiarioLivre.Core.Interfaces;

/// <summary>
/// Repository contract for querying and persisting classrooms.
/// </summary>
public interface IClassroomRepository
{
    Task<IReadOnlyList<Classroom>> GetByAcademicYearAsync(Guid academicYearId, CancellationToken ct = default);

    Task<Classroom?> GetWithDetailsAsync(Guid classroomId, CancellationToken ct = default);

    Task<Classroom?> GetForCouncilReportAsync(Guid classroomId, CancellationToken ct = default);

    Task AddAsync(Classroom classroom, CancellationToken ct = default);

    Task UpdateAsync(Classroom classroom, CancellationToken ct = default);

    Task UpdateSeatingGridAsync(
        Guid classroomId,
        int seatRows,
        int seatColumns,
        IReadOnlyList<(Guid StudentId, int? SeatRow, int? SeatColumn)> seats,
        CancellationToken ct = default);

    Task AddSubjectAsync(Subject subject, CancellationToken ct = default);

    Task DeleteAsync(Guid classroomId, CancellationToken ct = default);
}
