using DiarioLivre.Core.Enums;

namespace DiarioLivre.Core.Models;

/// <summary>
/// Lightweight value object representing a student's attendance in a single session.
/// </summary>
public readonly record struct AttendanceRecord(int LessonCount, AttendanceStatus Status);
