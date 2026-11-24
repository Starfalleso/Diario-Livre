using DiarioLivre.Core.Entities;
using DiarioLivre.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DiarioLivre.Infrastructure.Persistence.Repositories;

internal sealed class ScheduleRepository : IScheduleRepository
{
    private readonly IDbContextFactory<DiarioLivreDbContext> _contextFactory;

    public ScheduleRepository(IDbContextFactory<DiarioLivreDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<ScheduleSlot>> GetByAcademicYearAsync(
        Guid academicYearId,
        CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);
        List<ScheduleSlot> items = await context.ScheduleSlots
            .AsNoTracking()
            .Include(s => s.Classroom)
            .Include(s => s.Subject)
            .Where(s => s.AcademicYearId == academicYearId)
            .ToListAsync(ct);

        return items
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .ToList();
    }

    public async Task AddAsync(ScheduleSlot slot, CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);
        context.ScheduleSlots.Add(slot);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid slotId, CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);
        await context.ScheduleSlots
            .Where(s => s.Id == slotId)
            .ExecuteDeleteAsync(ct);
    }
}
