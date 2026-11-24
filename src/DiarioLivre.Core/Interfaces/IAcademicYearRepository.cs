using DiarioLivre.Core.Entities;

namespace DiarioLivre.Core.Interfaces;

/// <summary>
/// Repository contract for managing academic years.
/// </summary>
public interface IAcademicYearRepository
{
    Task<IReadOnlyList<AcademicYear>> GetAllAsync(CancellationToken ct = default);

    Task<AcademicYear?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task AddAsync(AcademicYear academicYear, CancellationToken ct = default);

    Task UpdateAsync(AcademicYear academicYear, CancellationToken ct = default);
}
