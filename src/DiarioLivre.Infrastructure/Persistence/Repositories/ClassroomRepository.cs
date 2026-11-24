using DiarioLivre.Core.Entities;
using DiarioLivre.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DiarioLivre.Infrastructure.Persistence.Repositories;

internal sealed class ClassroomRepository : IClassroomRepository
{
    private readonly IDbContextFactory<DiarioLivreDbContext> _contextFactory;

    public ClassroomRepository(IDbContextFactory<DiarioLivreDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<Classroom>> GetByAcademicYearAsync(Guid academicYearId, CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.Classrooms
            .AsNoTracking()
            .AsSplitQuery()
            .Include(c => c.AcademicYear)
            .Include(c => c.Subjects)
            .Include(c => c.Students)
            .Include(c => c.AttendanceSessions)
                .ThenInclude(s => s.Entries)
            .Where(c => c.AcademicYearId == academicYearId)
            .OrderBy(c => c.Shift)
            .ThenBy(c => c.Name)
            .ToListAsync(ct);
    }

    public async Task<Classroom?> GetWithDetailsAsync(Guid classroomId, CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.Classrooms
            .AsNoTracking()
            .AsSplitQuery()
            .Include(c => c.AcademicYear)
            .Include(c => c.Subjects.OrderBy(s => s.Name))
                .ThenInclude(s => s.Assessments)
            .Include(c => c.Students.OrderBy(s => s.RollNumber))
            .Include(c => c.AttendanceSessions)
                .ThenInclude(s => s.Entries)
            .FirstOrDefaultAsync(c => c.Id == classroomId, ct);
    }

    public async Task<Classroom?> GetForCouncilReportAsync(Guid classroomId, CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.Classrooms
            .AsNoTracking()
            .AsSplitQuery()
            .Include(c => c.AcademicYear)
            .Include(c => c.Subjects.OrderBy(s => s.Name))
                .ThenInclude(s => s.Assessments)
            .Include(c => c.Students.OrderBy(s => s.RollNumber))
                .ThenInclude(st => st.Grades)
            .Include(c => c.Students.OrderBy(s => s.RollNumber))
                .ThenInclude(st => st.Observations)
            .Include(c => c.AttendanceSessions)
                .ThenInclude(s => s.Entries)
            .FirstOrDefaultAsync(c => c.Id == classroomId, ct);
    }

    public async Task AddAsync(Classroom classroom, CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);
        context.Classrooms.Add(classroom);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Classroom classroom, CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);
        context.Classrooms.Update(classroom);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateSeatingGridAsync(
        Guid classroomId,
        int seatRows,
        int seatColumns,
        IReadOnlyList<(Guid StudentId, int? SeatRow, int? SeatColumn)> seats,
        CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);

        await context.Classrooms
            .Where(c => c.Id == classroomId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.SeatRows, Math.Clamp(seatRows, 2, 10))
                .SetProperty(c => c.SeatColumns, Math.Clamp(seatColumns, 2, 10)),
                ct);

        foreach ((Guid studentId, int? seatRow, int? seatColumn) in seats)
        {
            await context.Students
                .Where(s => s.Id == studentId && s.ClassroomId == classroomId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(s => s.SeatRow, seatRow)
                    .SetProperty(s => s.SeatColumn, seatColumn),
                    ct);
        }
    }

    public async Task AddSubjectAsync(Subject subject, CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);
        context.Subjects.Add(subject);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid classroomId, CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);
        await context.Classrooms
            .Where(c => c.Id == classroomId)
            .ExecuteDeleteAsync(ct);
    }
}
