using DiarioLivre.Core.Entities;
using DiarioLivre.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DiarioLivre.Infrastructure.Persistence.Repositories;

internal sealed class StudentRepository : IStudentRepository
{
    private readonly IDbContextFactory<DiarioLivreDbContext> _contextFactory;

    public StudentRepository(IDbContextFactory<DiarioLivreDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<Student>> GetByClassroomAsync(Guid classroomId, CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.Students
            .AsNoTracking()
            .Where(s => s.ClassroomId == classroomId)
            .OrderBy(s => s.RollNumber)
            .ThenBy(s => s.FullName)
            .ToListAsync(ct);
    }

    public async Task<Student?> GetDossierAsync(Guid studentId, CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.Students
            .AsNoTracking()
            .AsSplitQuery()
            .Include(s => s.Classroom)
                .ThenInclude(c => c!.AcademicYear)
            .Include(s => s.Classroom)
                .ThenInclude(c => c!.Subjects)
                    .ThenInclude(sub => sub.Assessments)
            .Include(s => s.Grades)
            .Include(s => s.AttendanceEntries)
                .ThenInclude(e => e.AttendanceSession)
            .Include(s => s.Observations.OrderByDescending(o => o.Date))
            .FirstOrDefaultAsync(s => s.Id == studentId, ct);
    }

    public async Task AddAsync(Student student, CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);
        context.Students.Add(student);
        await context.SaveChangesAsync(ct);
    }

    public async Task AddRangeAsync(IEnumerable<Student> students, CancellationToken ct = default)
    {
        List<Student> list = students.ToList();
        if (list.Count == 0)
        {
            return;
        }

        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);
        context.Students.AddRange(list);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Student student, CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);
        Student? existing = await context.Students.FirstOrDefaultAsync(s => s.Id == student.Id, ct);
        if (existing is null)
        {
            return;
        }

        existing.RollNumber = student.RollNumber;
        existing.FullName = student.FullName;
        existing.PhotoFileName = student.PhotoFileName;
        existing.HasSpecialNeeds = student.HasSpecialNeeds;
        existing.GeneralNotes = student.GeneralNotes;

        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid studentId, CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);
        await context.Students
            .Where(s => s.Id == studentId)
            .ExecuteDeleteAsync(ct);
    }
}
