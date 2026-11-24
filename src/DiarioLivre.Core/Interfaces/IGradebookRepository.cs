using DiarioLivre.Core.Entities;

namespace DiarioLivre.Core.Interfaces;

/// <summary>
/// Repository contract for managing assessments and student grades.
/// </summary>
public interface IGradebookRepository
{
    Task<IReadOnlyList<Assessment>> GetAssessmentsAsync(Guid subjectId, int? termNumber = null, CancellationToken ct = default);

    Task<IReadOnlyList<Grade>> GetGradesForSubjectAsync(Guid subjectId, CancellationToken ct = default);

    Task AddAssessmentAsync(Assessment assessment, CancellationToken ct = default);

    Task DeleteAssessmentAsync(Guid assessmentId, CancellationToken ct = default);

    Task UpsertGradesAsync(IEnumerable<Grade> grades, CancellationToken ct = default);
}
