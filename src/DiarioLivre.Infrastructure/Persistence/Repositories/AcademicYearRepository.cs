using DiarioLivre.Core.Entities;
using DiarioLivre.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DiarioLivre.Infrastructure.Persistence.Repositories;

internal sealed class AcademicYearRepository : IAcademicYearRepository
{
    private readonly IDbContextFactory<DiarioLivreDbContext> _contextFactory;

    public AcademicYearRepository(IDbContextFactory<DiarioLivreDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<AcademicYear>> GetAllAsync(CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.AcademicYears
            .AsNoTracking()
            .OrderByDescending(y => y.Year)
            .ThenBy(y => y.SchoolName)
            .ToListAsync(ct);
    }

    public async Task<AcademicYear?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.AcademicYears
            .AsNoTracking()
            .FirstOrDefaultAsync(y => y.Id == id, ct);
    }

    public async Task AddAsync(AcademicYear academicYear, CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);
        context.AcademicYears.Add(academicYear);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(AcademicYear academicYear, CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);
        context.AcademicYears.Update(academicYear);
        await context.SaveChangesAsync(ct);
    }
}
