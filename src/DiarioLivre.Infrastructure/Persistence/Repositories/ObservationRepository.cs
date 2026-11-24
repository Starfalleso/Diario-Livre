using DiarioLivre.Core.Entities;
using DiarioLivre.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DiarioLivre.Infrastructure.Persistence.Repositories;

internal sealed class ObservationRepository : IObservationRepository
{
    private readonly IDbContextFactory<DiarioLivreDbContext> _contextFactory;

    public ObservationRepository(IDbContextFactory<DiarioLivreDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<StudentObservation>> GetByStudentAsync(Guid studentId, CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.StudentObservations
            .AsNoTracking()
            .Where(o => o.StudentId == studentId)
            .OrderByDescending(o => o.Date)
            .ToListAsync(ct);
    }

    public async Task AddAsync(StudentObservation observation, CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);
        context.StudentObservations.Add(observation);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid observationId, CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);
        await context.StudentObservations
            .Where(o => o.Id == observationId)
            .ExecuteDeleteAsync(ct);
    }
}
