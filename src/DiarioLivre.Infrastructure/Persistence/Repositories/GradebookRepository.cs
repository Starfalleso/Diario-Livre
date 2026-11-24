using DiarioLivre.Core.Entities;
using DiarioLivre.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DiarioLivre.Infrastructure.Persistence.Repositories;

internal sealed class GradebookRepository : IGradebookRepository
{
    private readonly IDbContextFactory<DiarioLivreDbContext> _contextFactory;

    public GradebookRepository(IDbContextFactory<DiarioLivreDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<Assessment>> GetAssessmentsAsync(
        Guid subjectId,
        int? termNumber = null,
        CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);

        IQueryable<Assessment> query = context.Assessments
            .AsNoTracking()
            .Where(a => a.SubjectId == subjectId);

        if (termNumber.HasValue)
        {
            query = query.Where(a => a.TermNumber == termNumber.Value);
        }

        return await query
            .OrderBy(a => a.TermNumber)
            .ThenBy(a => a.IsRecovery)
            .ThenBy(a => a.Title)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Grade>> GetGradesForSubjectAsync(Guid subjectId, CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.Grades
            .AsNoTracking()
            .Where(g => g.Assessment != null && g.Assessment.SubjectId == subjectId)
            .ToListAsync(ct);
    }

    public async Task AddAssessmentAsync(Assessment assessment, CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);
        context.Assessments.Add(assessment);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAssessmentAsync(Guid assessmentId, CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);
        await context.Assessments
            .Where(a => a.Id == assessmentId)
            .ExecuteDeleteAsync(ct);
    }

    public async Task UpsertGradesAsync(IEnumerable<Grade> grades, CancellationToken ct = default)
    {
        List<Grade> gradeList = grades.ToList();
        if (gradeList.Count == 0)
        {
            return;
        }

        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);
        List<Guid> assessmentIds = gradeList.Select(g => g.AssessmentId).Distinct().ToList();

        List<Grade> existingGrades = await context.Grades
            .Where(g => assessmentIds.Contains(g.AssessmentId))
            .ToListAsync(ct);

        var existingByKey = existingGrades.ToDictionary(g => (g.AssessmentId, g.StudentId));

        foreach (Grade incoming in gradeList)
        {
            if (existingByKey.TryGetValue((incoming.AssessmentId, incoming.StudentId), out Grade? existing))
            {
                existing.Score = incoming.Score;
            }
            else
            {
                context.Grades.Add(new Grade
                {
                    Id = incoming.Id == Guid.Empty ? Guid.NewGuid() : incoming.Id,
                    AssessmentId = incoming.AssessmentId,
                    StudentId = incoming.StudentId,
                    Score = incoming.Score
                });
            }
        }

        await context.SaveChangesAsync(ct);
    }
}
