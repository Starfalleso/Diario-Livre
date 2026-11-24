using DiarioLivre.Core.Entities;
using DiarioLivre.Core.Models;

namespace DiarioLivre.Core.Interfaces;

/// <summary>
/// Pure domain service for computing lesson-weighted attendance percentages and LDB alerts.
/// </summary>
public interface IAttendanceCalculator
{
    /// <summary>
    /// Calculates attendance summary and alert severity from a collection of attendance records.
    /// </summary>
    AttendanceSummary Calculate(IEnumerable<AttendanceRecord> records, decimal minAttendancePercentage = 75.0m);

    /// <summary>
    /// Calculates attendance summary for a specific student across a collection of sessions.
    /// </summary>
    AttendanceSummary CalculateForStudent(
        Guid studentId,
        IEnumerable<AttendanceSession> sessions,
        decimal minAttendancePercentage = 75.0m);
}
