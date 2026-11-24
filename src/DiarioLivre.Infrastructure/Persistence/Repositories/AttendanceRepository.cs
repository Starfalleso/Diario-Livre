using DiarioLivre.Core.Entities;
using DiarioLivre.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DiarioLivre.Infrastructure.Persistence.Repositories;

internal sealed class AttendanceRepository : IAttendanceRepository
{
    private readonly IDbContextFactory<DiarioLivreDbContext> _contextFactory;

    public AttendanceRepository(IDbContextFactory<DiarioLivreDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<AttendanceSession>> GetSessionsAsync(
        Guid classroomId,
        Guid? subjectId = null,
        CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);

        IQueryable<AttendanceSession> query = context.AttendanceSessions
            .AsNoTracking()
            .Include(s => s.Entries)
            .Where(s => s.ClassroomId == classroomId);

        if (subjectId.HasValue)
        {
            query = query.Where(s => s.SubjectId == subjectId.Value);
        }

        return await query
            .OrderByDescending(s => s.Date)
            .ToListAsync(ct);
    }

    public async Task<AttendanceSession?> GetSessionByDateAsync(
        Guid classroomId,
        Guid? subjectId,
        DateOnly date,
        CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.AttendanceSessions
            .AsNoTracking()
            .Include(s => s.Entries)
            .FirstOrDefaultAsync(
                s => s.ClassroomId == classroomId
                     && s.SubjectId == subjectId
                     && s.Date == date,
                ct);
    }

    public async Task SaveSessionAsync(AttendanceSession session, CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);

        AttendanceSession? existing = await context.AttendanceSessions
            .Include(s => s.Entries)
            .FirstOrDefaultAsync(
                s => s.Id == session.Id ||
                     (s.ClassroomId == session.ClassroomId
                      && s.SubjectId == session.SubjectId
                      && s.Date == session.Date),
                ct);

        if (existing is null)
        {
            var newSession = new AttendanceSession
            {
                Id = session.Id == Guid.Empty ? Guid.NewGuid() : session.Id,
                ClassroomId = session.ClassroomId,
                SubjectId = session.SubjectId,
                Date = session.Date,
                TermNumber = Math.Max(1, session.TermNumber),
                LessonCount = session.LessonCount,
                TopicCovered = session.TopicCovered,
                DailyActivity = session.DailyActivity,
                Homework = session.Homework,
                LessonNotes = session.LessonNotes
            };

            foreach (AttendanceEntry entry in session.Entries)
            {
                newSession.Entries.Add(new AttendanceEntry
                {
                    Id = entry.Id == Guid.Empty ? Guid.NewGuid() : entry.Id,
                    AttendanceSessionId = newSession.Id,
                    StudentId = entry.StudentId,
                    Status = entry.Status
                });
            }

            context.AttendanceSessions.Add(newSession);
        }
        else
        {
            existing.TermNumber = Math.Max(1, session.TermNumber);
            existing.LessonCount = session.LessonCount;
            existing.TopicCovered = session.TopicCovered;
            existing.DailyActivity = session.DailyActivity;
            existing.Homework = session.Homework;
            existing.LessonNotes = session.LessonNotes;
            existing.Date = session.Date;

            var existingEntries = existing.Entries.ToDictionary(e => e.StudentId);
            foreach (AttendanceEntry incoming in session.Entries)
            {
                if (existingEntries.TryGetValue(incoming.StudentId, out AttendanceEntry? existingEntry))
                {
                    existingEntry.Status = incoming.Status;
                }
                else
                {
                    context.AttendanceEntries.Add(new AttendanceEntry
                    {
                        Id = incoming.Id == Guid.Empty ? Guid.NewGuid() : incoming.Id,
                        AttendanceSessionId = existing.Id,
                        StudentId = incoming.StudentId,
                        Status = incoming.Status
                    });
                }
            }
        }

        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);
        await context.AttendanceSessions
            .Where(s => s.Id == sessionId)
            .ExecuteDeleteAsync(ct);
    }
}
