using DiarioLivre.Core.Entities;
using DiarioLivre.Core.Enums;
using DiarioLivre.Core.Models;

namespace DiarioLivre.Core.Interfaces;

/// <summary>
/// Pure domain calculator for LDB 200 school days progress, planned calendar days, and Brazilian national holidays.
/// </summary>
public interface ISchoolDaysCalculator
{
    SchoolDaysSummary CalculateRecordedProgress(
        IEnumerable<AttendanceSession> sessions,
        IEnumerable<SchoolCalendarEvent> calendarEvents,
        int targetAnnualSchoolDays,
        GradingSystem gradingSystem,
        int? termFilter = null);

    int CountCalendarSchoolDays(
        DateOnly startDate,
        DateOnly endDate,
        IEnumerable<SchoolCalendarEvent> calendarEvents);

    bool IsPlannedSchoolDay(
        DateOnly date,
        IEnumerable<SchoolCalendarEvent> calendarEvents);

    IReadOnlyList<SchoolCalendarEvent> CreateBrazilianNationalHolidays(
        Guid academicYearId,
        int year,
        GradingSystem gradingSystem);
}
