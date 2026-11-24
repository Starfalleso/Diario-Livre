namespace DiarioLivre.Core.Enums;

/// <summary>
/// Categorizes an academic calendar event for school-day tracking under Brazilian LDB rules.
/// </summary>
public enum SchoolCalendarEventType
{
    Holiday = 1,
    Recess = 2,
    SchoolSaturday = 3,
    PedagogicalDay = 4,
    ExtraSchoolDay = 5
}

/// <summary>
/// Classification helpers for <see cref="SchoolCalendarEventType"/>.
/// </summary>
public static class SchoolCalendarEventTypeExtensions
{
    public static bool CountsAsSchoolDay(this SchoolCalendarEventType type) => type switch
    {
        SchoolCalendarEventType.SchoolSaturday => true,
        SchoolCalendarEventType.ExtraSchoolDay => true,
        _ => false
    };

    public static bool ExcludesRegularWeekday(this SchoolCalendarEventType type) => type switch
    {
        SchoolCalendarEventType.Holiday => true,
        SchoolCalendarEventType.Recess => true,
        SchoolCalendarEventType.PedagogicalDay => true,
        _ => false
    };
}
