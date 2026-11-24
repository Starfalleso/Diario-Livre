using DiarioLivre.Core.Entities;

namespace DiarioLivre.Core.Interfaces;

/// <summary>
/// Repository contract for managing BNCC-tagged lesson plans.
/// </summary>
public interface ILessonPlanRepository
{
    Task<IReadOnlyList<LessonPlan>> GetByClassroomAsync(Guid classroomId, CancellationToken ct = default);

    Task AddAsync(LessonPlan lessonPlan, CancellationToken ct = default);

    Task UpdateAsync(LessonPlan lessonPlan, CancellationToken ct = default);

    Task DeleteAsync(Guid lessonPlanId, CancellationToken ct = default);
}
