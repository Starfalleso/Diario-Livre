using DiarioLivre.Core.Entities;

namespace DiarioLivre.Core.Interfaces;

/// <summary>
/// Repository contract for managing student observations.
/// </summary>
public interface IObservationRepository
{
    Task<IReadOnlyList<StudentObservation>> GetByStudentAsync(Guid studentId, CancellationToken ct = default);

    Task AddAsync(StudentObservation observation, CancellationToken ct = default);

    Task DeleteAsync(Guid observationId, CancellationToken ct = default);
}
