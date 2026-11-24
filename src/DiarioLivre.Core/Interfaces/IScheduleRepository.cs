using DiarioLivre.Core.Entities;

namespace DiarioLivre.Core.Interfaces;

/// <summary>
/// Repository contract for managing weekly teacher timetable slots ("Grade Horária").
/// </summary>
public interface IScheduleRepository
{
    Task<IReadOnlyList<ScheduleSlot>> GetByAcademicYearAsync(Guid academicYearId, CancellationToken ct = default);

    Task AddAsync(ScheduleSlot slot, CancellationToken ct = default);

    Task DeleteAsync(Guid slotId, CancellationToken ct = default);
}
