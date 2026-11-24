using DiarioLivre.Core.Entities;
using DiarioLivre.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DiarioLivre.Infrastructure.Persistence.Repositories;

internal sealed class SchoolCalendarRepository : ISchoolCalendarRepository
{
    private readonly IDbContextFactory<DiarioLivreDbContext> _contextFactory;

    public SchoolCalendarRepository(IDbContextFactory<DiarioLivreDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<SchoolCalendarEvent>> GetByAcademicYearAsync(
        Guid academicYearId,
        CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.SchoolCalendarEvents
            .AsNoTracking()
            .Where(e => e.AcademicYearId == academicYearId)
            .OrderBy(e => e.Date)
            .ThenBy(e => e.Title)
            .ToListAsync(ct);
    }

    public async Task AddAsync(SchoolCalendarEvent calendarEvent, CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);
        context.SchoolCalendarEvents.Add(calendarEvent);
        await context.SaveChangesAsync(ct);
    }

    public async Task AddRangeAsync(IEnumerable<SchoolCalendarEvent> events, CancellationToken ct = default)
    {
        List<SchoolCalendarEvent> items = events.ToList();
        if (items.Count == 0)
        {
            return;
        }

        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);
        context.SchoolCalendarEvents.AddRange(items);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid eventId, CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);
        await context.SchoolCalendarEvents
            .Where(e => e.Id == eventId)
            .ExecuteDeleteAsync(ct);
    }
}
