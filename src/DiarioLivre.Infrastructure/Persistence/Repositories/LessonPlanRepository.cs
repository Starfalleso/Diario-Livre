using DiarioLivre.Core.Entities;
using DiarioLivre.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DiarioLivre.Infrastructure.Persistence.Repositories;

internal sealed class LessonPlanRepository : ILessonPlanRepository
{
    private readonly IDbContextFactory<DiarioLivreDbContext> _contextFactory;

    public LessonPlanRepository(IDbContextFactory<DiarioLivreDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<LessonPlan>> GetByClassroomAsync(
        Guid classroomId,
        CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.LessonPlans
            .AsNoTracking()
            .Include(lp => lp.Subject)
            .Where(lp => lp.ClassroomId == classroomId)
            .OrderBy(lp => lp.TermNumber)
            .ThenBy(lp => lp.PlannedDate)
            .ThenBy(lp => lp.Title)
            .ToListAsync(ct);
    }

    public async Task AddAsync(LessonPlan lessonPlan, CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);
        context.LessonPlans.Add(lessonPlan);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(LessonPlan lessonPlan, CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);
        await context.LessonPlans
            .Where(lp => lp.Id == lessonPlan.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(lp => lp.SubjectId, lessonPlan.SubjectId)
                .SetProperty(lp => lp.TermNumber, lessonPlan.TermNumber)
                .SetProperty(lp => lp.PlannedDate, lessonPlan.PlannedDate)
                .SetProperty(lp => lp.Title, lessonPlan.Title)
                .SetProperty(lp => lp.BnccCode, lessonPlan.BnccCode)
                .SetProperty(lp => lp.Objectives, lessonPlan.Objectives)
                .SetProperty(lp => lp.PlannedActivity, lessonPlan.PlannedActivity)
                .SetProperty(lp => lp.Homework, lessonPlan.Homework)
                .SetProperty(lp => lp.Status, lessonPlan.Status),
                ct);
    }

    public async Task DeleteAsync(Guid lessonPlanId, CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);
        await context.LessonPlans
            .Where(lp => lp.Id == lessonPlanId)
            .ExecuteDeleteAsync(ct);
    }
}
