using DiarioLivre.Core.Entities;
using DiarioLivre.Core.Enums;
using DiarioLivre.Core.Models;
using DiarioLivre.Core.Services;

namespace DiarioLivre.Core.Tests;

public class SchoolDaysCalculatorTests
{
    private readonly SchoolDaysCalculator _calculator = new();

    [Fact]
    public void CalculateRecordedProgress_MultipleSubjectsOnSameDate_CountAsSingleSchoolDay()
    {
        var day1 = new DateOnly(2026, 3, 2);
        var day2 = new DateOnly(2026, 3, 3);
        var sessions = new[]
        {
            new AttendanceSession { Date = day1, TermNumber = 1, LessonCount = 2 },
            new AttendanceSession { Date = day1, TermNumber = 1, LessonCount = 1 },
            new AttendanceSession { Date = day2, TermNumber = 1, LessonCount = 2 }
        };

        SchoolDaysSummary summary = _calculator.CalculateRecordedProgress(
            sessions,
            Array.Empty<SchoolCalendarEvent>(),
            targetAnnualSchoolDays: 200,
            gradingSystem: GradingSystem.Semester,
            termFilter: null);

        Assert.Equal(2, summary.RecordedDays);
        Assert.Equal(200, summary.TargetDays);
        Assert.Equal(198, summary.RemainingDays);
        Assert.Equal(1.0m, summary.ProgressPercentage);
        Assert.False(summary.MeetsTarget);
    }

    [Fact]
    public void CalculateRecordedProgress_SemesterFilter_SplitsTargetAndFiltersSessionsAndEvents()
    {
        Guid yearId = Guid.NewGuid();
        var sessions = new[]
        {
            new AttendanceSession { Date = new DateOnly(2026, 3, 2), TermNumber = 1 },
            new AttendanceSession { Date = new DateOnly(2026, 3, 4), TermNumber = 1 },
            new AttendanceSession { Date = new DateOnly(2026, 8, 5), TermNumber = 2 }
        };

        var events = new[]
        {
            new SchoolCalendarEvent { AcademicYearId = yearId, Date = new DateOnly(2026, 3, 14), Type = SchoolCalendarEventType.SchoolSaturday, TermNumber = 1, Title = "Sábado Letivo" },
            new SchoolCalendarEvent { AcademicYearId = yearId, Date = new DateOnly(2026, 4, 21), Type = SchoolCalendarEventType.Holiday, TermNumber = 1, Title = "Tiradentes" },
            new SchoolCalendarEvent { AcademicYearId = yearId, Date = new DateOnly(2026, 9, 7), Type = SchoolCalendarEventType.Holiday, TermNumber = 2, Title = "Independência" }
        };

        SchoolDaysSummary sem1 = _calculator.CalculateRecordedProgress(
            sessions,
            events,
            targetAnnualSchoolDays: 200,
            gradingSystem: GradingSystem.Semester,
            termFilter: 1);

        Assert.Equal(2, sem1.RecordedDays);
        Assert.Equal(100, sem1.TargetDays);
        Assert.Equal(98, sem1.RemainingDays);
        Assert.Equal(2.0m, sem1.ProgressPercentage);
        Assert.Equal(1, sem1.SchoolSaturdaysCount);
        Assert.Equal(1, sem1.NonSchoolDaysCount);
    }

    [Fact]
    public void CountCalendarSchoolDays_ExcludesHolidaysAndWeekends_IncludesSchoolSaturdays()
    {
        // April 20 (Mon) to April 26 (Sun) 2026:
        // Mon 20 (school), Tue 21 (Holiday - Tiradentes), Wed 22 (school), Thu 23 (school),
        // Fri 24 (PedagogicalDay - non-school), Sat 25 (SchoolSaturday - counts!), Sun 26 (weekend)
        // Total = 4 planned school days.
        Guid yearId = Guid.NewGuid();
        var events = new[]
        {
            new SchoolCalendarEvent { AcademicYearId = yearId, Date = new DateOnly(2026, 4, 21), Type = SchoolCalendarEventType.Holiday, Title = "Tiradentes" },
            new SchoolCalendarEvent { AcademicYearId = yearId, Date = new DateOnly(2026, 4, 24), Type = SchoolCalendarEventType.PedagogicalDay, Title = "Conselho" },
            new SchoolCalendarEvent { AcademicYearId = yearId, Date = new DateOnly(2026, 4, 25), Type = SchoolCalendarEventType.SchoolSaturday, Title = "Sábado Letivo" }
        };

        int planned = _calculator.CountCalendarSchoolDays(
            new DateOnly(2026, 4, 20),
            new DateOnly(2026, 4, 26),
            events);

        Assert.Equal(4, planned);
        Assert.True(_calculator.IsPlannedSchoolDay(new DateOnly(2026, 4, 20), events));
        Assert.False(_calculator.IsPlannedSchoolDay(new DateOnly(2026, 4, 21), events));
        Assert.False(_calculator.IsPlannedSchoolDay(new DateOnly(2026, 4, 24), events));
        Assert.True(_calculator.IsPlannedSchoolDay(new DateOnly(2026, 4, 25), events));
        Assert.False(_calculator.IsPlannedSchoolDay(new DateOnly(2026, 4, 26), events));
    }

    [Fact]
    public void CreateBrazilianNationalHolidays_ReturnsAllNineFixedHolidaysWithValidTerms()
    {
        Guid yearId = Guid.NewGuid();
        IReadOnlyList<SchoolCalendarEvent> holidays = _calculator.CreateBrazilianNationalHolidays(
            yearId,
            2026,
            GradingSystem.Semester);

        Assert.Equal(9, holidays.Count);
        Assert.All(holidays, h =>
        {
            Assert.Equal(SchoolCalendarEventType.Holiday, h.Type);
            Assert.Equal(2026, h.Date.Year);
            Assert.InRange(h.TermNumber, 1, 2);
        });
        Assert.Contains(holidays, h => h.Date == new DateOnly(2026, 4, 21) && h.TermNumber == 1);
        Assert.Contains(holidays, h => h.Date == new DateOnly(2026, 11, 20) && h.TermNumber == 2);
    }
}
