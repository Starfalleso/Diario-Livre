using DiarioLivre.Core.Entities;

namespace DiarioLivre.Core.Interfaces;

/// <summary>
/// Repository contract for managing academic calendar events (holidays, recesses, school Saturdays).
/// </summary>
public interface ISchoolCalendarRepository
{
    Task<IReadOnlyList<SchoolCalendarEvent>> GetByAcademicYearAsync(Guid academicYearId, CancellationToken ct = default);

    Task AddAsync(SchoolCalendarEvent calendarEvent, CancellationToken ct = default);

    Task AddRangeAsync(IEnumerable<SchoolCalendarEvent> events, CancellationToken ct = default);

    Task DeleteAsync(Guid eventId, CancellationToken ct = default);
}
