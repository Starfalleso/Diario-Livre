using DiarioLivre.Core.Entities;
using DiarioLivre.Core.Enums;
using DiarioLivre.Core.Interfaces;
using DiarioLivre.Core.Models;

namespace DiarioLivre.Core.Services;

/// <summary>
/// Pure domain calculator implementing Brazil's LDB 75% minimum attendance rule.
/// </summary>
public sealed class AttendanceCalculator : IAttendanceCalculator
{
    private const decimal WarningAbsenceThreshold = 20.0m;
    private const decimal CriticalAbsenceThreshold = 25.0m;

    /// <inheritdoc />
    public AttendanceSummary Calculate(IEnumerable<AttendanceRecord> records, decimal minAttendancePercentage = 75.0m)
    {
        ArgumentNullException.ThrowIfNull(records);

        int totalLessons = 0;
        int presentLessons = 0;
        int unexcusedAbsences = 0;
        int justifiedAbsences = 0;

        foreach (AttendanceRecord record in records)
        {
            if (record.LessonCount <= 0)
            {
                continue;
            }

            totalLessons += record.LessonCount;

            switch (record.Status)
            {
                case AttendanceStatus.Present:
                    presentLessons += record.LessonCount;
                    break;
                case AttendanceStatus.Absent:
                    unexcusedAbsences += record.LessonCount;
                    break;
                case AttendanceStatus.Justified:
                    justifiedAbsences += record.LessonCount;
                    break;
            }
        }

        if (totalLessons == 0)
        {
            return new AttendanceSummary(
                TotalLessons: 0,
                PresentLessons: 0,
                UnexcusedAbsences: 0,
                JustifiedAbsences: 0,
                AttendancePercentage: 100.0m,
                AbsencePercentage: 0.0m,
                AlertStatus: AttendanceAlertStatus.Normal,
                MeetsLegalMinimum: true);
        }

        // Justified absences do not penalize the student's legal attendance percentage.
        decimal absencePercentage = Math.Round(
            (decimal)unexcusedAbsences / totalLessons * 100.0m,
            2,
            MidpointRounding.AwayFromZero);

        decimal attendancePercentage = Math.Round(
            (decimal)(totalLessons - unexcusedAbsences) / totalLessons * 100.0m,
            2,
            MidpointRounding.AwayFromZero);

        AttendanceAlertStatus alertStatus = absencePercentage switch
        {
            > CriticalAbsenceThreshold => AttendanceAlertStatus.Critical,
            >= WarningAbsenceThreshold => AttendanceAlertStatus.Warning,
            _ => AttendanceAlertStatus.Normal
        };

        bool meetsLegalMinimum = attendancePercentage >= minAttendancePercentage;

        return new AttendanceSummary(
            TotalLessons: totalLessons,
            PresentLessons: presentLessons,
            UnexcusedAbsences: unexcusedAbsences,
            JustifiedAbsences: justifiedAbsences,
            AttendancePercentage: attendancePercentage,
            AbsencePercentage: absencePercentage,
            AlertStatus: alertStatus,
            MeetsLegalMinimum: meetsLegalMinimum);
    }

    /// <inheritdoc />
    public AttendanceSummary CalculateForStudent(
        Guid studentId,
        IEnumerable<AttendanceSession> sessions,
        decimal minAttendancePercentage = 75.0m)
    {
        ArgumentNullException.ThrowIfNull(sessions);

        var records = sessions
            .Select(session => new
            {
                session.LessonCount,
                Entry = session.Entries.FirstOrDefault(e => e.StudentId == studentId)
            })
            .Where(x => x.Entry is not null)
            .Select(x => new AttendanceRecord(x.LessonCount, x.Entry!.Status));

        return Calculate(records, minAttendancePercentage);
    }
}
