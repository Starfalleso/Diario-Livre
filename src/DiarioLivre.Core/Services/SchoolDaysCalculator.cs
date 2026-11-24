using DiarioLivre.Core.Entities;
using DiarioLivre.Core.Enums;
using DiarioLivre.Core.Interfaces;
using DiarioLivre.Core.Models;

namespace DiarioLivre.Core.Services;

/// <summary>
/// Pure domain service implementing Brazilian LDB school-day (200 dias letivos) and calendar calculations.
/// </summary>
public sealed class SchoolDaysCalculator : ISchoolDaysCalculator
{
    private static readonly (int Month, int Day, string Title)[] BrazilianFixedHolidays =
    {
        (1, 1, "Confraternização Universal"),
        (4, 21, "Tiradentes"),
        (5, 1, "Dia do Trabalho"),
        (9, 7, "Independência do Brasil"),
        (10, 12, "Nossa Senhora Aparecida"),
        (11, 2, "Finados"),
        (11, 15, "Proclamação da República"),
        (11, 20, "Consciência Negra"),
        (12, 25, "Natal")
    };

    /// <inheritdoc />
    public SchoolDaysSummary CalculateRecordedProgress(
        IEnumerable<AttendanceSession> sessions,
        IEnumerable<SchoolCalendarEvent> calendarEvents,
        int targetAnnualSchoolDays,
        GradingSystem gradingSystem,
        int? termFilter = null)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(calendarEvents);

        int normalizedAnnualTarget = Math.Max(0, targetAnnualSchoolDays);
        int termCount = Math.Max(1, gradingSystem.GetTermCount());
        int targetDays = termFilter.HasValue
            ? (int)Math.Ceiling((double)normalizedAnnualTarget / termCount)
            : normalizedAnnualTarget;

        int recordedDays = (termFilter.HasValue
                ? sessions.Where(s => s.TermNumber == termFilter.Value)
                : sessions)
            .Select(s => s.Date)
            .Distinct()
            .Count();

        IEnumerable<SchoolCalendarEvent> scopedEvents = termFilter.HasValue
            ? calendarEvents.Where(e => e.TermNumber == termFilter.Value)
            : calendarEvents;

        int schoolSaturdaysCount = 0;
        int nonSchoolDaysCount = 0;
        foreach (SchoolCalendarEvent ev in scopedEvents)
        {
            if (ev.Type.CountsAsSchoolDay())
            {
                schoolSaturdaysCount++;
            }
            else if (ev.Type.ExcludesRegularWeekday())
            {
                nonSchoolDaysCount++;
            }
        }

        int remainingDays = Math.Max(0, targetDays - recordedDays);
        decimal progressPercentage = targetDays > 0
            ? Math.Min(100.0m, Math.Round((decimal)recordedDays / targetDays * 100.0m, 1, MidpointRounding.AwayFromZero))
            : 100.0m;

        bool meetsTarget = recordedDays >= targetDays;

        return new SchoolDaysSummary(
            RecordedDays: recordedDays,
            TargetDays: targetDays,
            RemainingDays: remainingDays,
            ProgressPercentage: progressPercentage,
            SchoolSaturdaysCount: schoolSaturdaysCount,
            NonSchoolDaysCount: nonSchoolDaysCount,
            MeetsTarget: meetsTarget);
    }

    /// <inheritdoc />
    public int CountCalendarSchoolDays(
        DateOnly startDate,
        DateOnly endDate,
        IEnumerable<SchoolCalendarEvent> calendarEvents)
    {
        ArgumentNullException.ThrowIfNull(calendarEvents);

        if (startDate > endDate)
        {
            return 0;
        }

        Dictionary<DateOnly, List<SchoolCalendarEvent>> lookup = BuildDateLookup(calendarEvents);
        int count = 0;

        for (DateOnly date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (IsPlannedSchoolDayCore(date, lookup))
            {
                count++;
            }
        }

        return count;
    }

    /// <inheritdoc />
    public bool IsPlannedSchoolDay(DateOnly date, IEnumerable<SchoolCalendarEvent> calendarEvents)
    {
        ArgumentNullException.ThrowIfNull(calendarEvents);
        Dictionary<DateOnly, List<SchoolCalendarEvent>> lookup = BuildDateLookup(calendarEvents);
        return IsPlannedSchoolDayCore(date, lookup);
    }

    /// <inheritdoc />
    public IReadOnlyList<SchoolCalendarEvent> CreateBrazilianNationalHolidays(
        Guid academicYearId,
        int year,
        GradingSystem gradingSystem)
    {
        int safeYear = Math.Clamp(year, 1900, 2100);
        var holidays = new List<SchoolCalendarEvent>(BrazilianFixedHolidays.Length);

        foreach ((int month, int day, string title) in BrazilianFixedHolidays)
        {
            var date = new DateOnly(safeYear, month, day);
            holidays.Add(new SchoolCalendarEvent
            {
                Id = Guid.NewGuid(),
                AcademicYearId = academicYearId,
                Date = date,
                Type = SchoolCalendarEventType.Holiday,
                Title = title,
                TermNumber = gradingSystem.GetDefaultTermForDate(date)
            });
        }

        return holidays;
    }

    private static Dictionary<DateOnly, List<SchoolCalendarEvent>> BuildDateLookup(IEnumerable<SchoolCalendarEvent> calendarEvents)
    {
        var lookup = new Dictionary<DateOnly, List<SchoolCalendarEvent>>();
        foreach (SchoolCalendarEvent ev in calendarEvents)
        {
            if (!lookup.TryGetValue(ev.Date, out List<SchoolCalendarEvent>? list))
            {
                list = new List<SchoolCalendarEvent>();
                lookup[ev.Date] = list;
            }
            list.Add(ev);
        }
        return lookup;
    }

    private static bool IsPlannedSchoolDayCore(DateOnly date, Dictionary<DateOnly, List<SchoolCalendarEvent>> lookup)
    {
        if (lookup.TryGetValue(date, out List<SchoolCalendarEvent>? events))
        {
            if (events.Any(e => e.Type.CountsAsSchoolDay()))
            {
                return true;
            }

            if (events.Any(e => e.Type.ExcludesRegularWeekday()))
            {
                return false;
            }
        }

        return date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday;
    }
}
