using DiarioLivre.Core.Entities;

namespace DiarioLivre.Core.Interfaces;

/// <summary>
/// Repository contract for querying and persisting students and full student dossiers.
/// </summary>
public interface IStudentRepository
{
    Task<IReadOnlyList<Student>> GetByClassroomAsync(Guid classroomId, CancellationToken ct = default);

    Task<Student?> GetDossierAsync(Guid studentId, CancellationToken ct = default);

    Task AddAsync(Student student, CancellationToken ct = default);

    Task AddRangeAsync(IEnumerable<Student> students, CancellationToken ct = default);

    Task UpdateAsync(Student student, CancellationToken ct = default);

    Task DeleteAsync(Guid studentId, CancellationToken ct = default);
}
