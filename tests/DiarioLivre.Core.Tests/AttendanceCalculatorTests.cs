using DiarioLivre.Core.Entities;
using DiarioLivre.Core.Enums;
using DiarioLivre.Core.Models;
using DiarioLivre.Core.Services;

namespace DiarioLivre.Core.Tests;

public sealed class AttendanceCalculatorTests
{
    private readonly AttendanceCalculator _calculator = new();

    [Fact]
    public void Calculate_ZeroSessions_ReturnsFullAttendanceAndNormalStatus()
    {
        AttendanceSummary summary = _calculator.Calculate(Array.Empty<AttendanceRecord>());

        Assert.Equal(0, summary.TotalLessons);
        Assert.Equal(100.0m, summary.AttendancePercentage);
        Assert.Equal(0.0m, summary.AbsencePercentage);
        Assert.Equal(AttendanceAlertStatus.Normal, summary.AlertStatus);
        Assert.True(summary.MeetsLegalMinimum);
    }

    [Fact]
    public void Calculate_LessonCountWeighting_AggregatesDoubleLessonsAccurately()
    {
        var records = new[]
        {
            new AttendanceRecord(2, AttendanceStatus.Present),
            new AttendanceRecord(2, AttendanceStatus.Present),
            new AttendanceRecord(2, AttendanceStatus.Absent)
        };

        // 4 present out of 6 lessons -> 2 unexcused absences = 33.33% absence, 66.67% attendance
        AttendanceSummary summary = _calculator.Calculate(records);

        Assert.Equal(6, summary.TotalLessons);
        Assert.Equal(4, summary.PresentLessons);
        Assert.Equal(2, summary.UnexcusedAbsences);
        Assert.Equal(33.33m, summary.AbsencePercentage);
        Assert.Equal(66.67m, summary.AttendancePercentage);
        Assert.Equal(AttendanceAlertStatus.Critical, summary.AlertStatus);
        Assert.False(summary.MeetsLegalMinimum);
    }

    [Fact]
    public void Calculate_JustifiedAbsences_DoNotPenalizeAttendancePercentage()
    {
        var records = new[]
        {
            new AttendanceRecord(5, AttendanceStatus.Present),
            new AttendanceRecord(5, AttendanceStatus.Justified)
        };

        AttendanceSummary summary = _calculator.Calculate(records);

        Assert.Equal(10, summary.TotalLessons);
        Assert.Equal(5, summary.PresentLessons);
        Assert.Equal(0, summary.UnexcusedAbsences);
        Assert.Equal(5, summary.JustifiedAbsences);
        Assert.Equal(100.0m, summary.AttendancePercentage);
        Assert.Equal(0.0m, summary.AbsencePercentage);
        Assert.Equal(AttendanceAlertStatus.Normal, summary.AlertStatus);
        Assert.True(summary.MeetsLegalMinimum);
    }

    [Theory]
    [InlineData(81, 19, 81.00, 19.00, AttendanceAlertStatus.Normal, true)]
    [InlineData(80, 20, 80.00, 20.00, AttendanceAlertStatus.Warning, true)]
    [InlineData(75, 25, 75.00, 25.00, AttendanceAlertStatus.Warning, true)]
    [InlineData(74, 26, 74.00, 26.00, AttendanceAlertStatus.Critical, false)]
    public void Calculate_LdbThresholds_ReturnsExpectedAlertAndCompliance(
        int presentCount,
        int absentCount,
        decimal expectedAttendance,
        decimal expectedAbsence,
        AttendanceAlertStatus expectedAlert,
        bool expectedMeetsMinimum)
    {
        var records = new[]
        {
            new AttendanceRecord(presentCount, AttendanceStatus.Present),
            new AttendanceRecord(absentCount, AttendanceStatus.Absent)
        };

        AttendanceSummary summary = _calculator.Calculate(records, minAttendancePercentage: 75.0m);

        Assert.Equal(expectedAttendance, summary.AttendancePercentage);
        Assert.Equal(expectedAbsence, summary.AbsencePercentage);
        Assert.Equal(expectedAlert, summary.AlertStatus);
        Assert.Equal(expectedMeetsMinimum, summary.MeetsLegalMinimum);
    }

    [Fact]
    public void CalculateForStudent_IgnoresUnrelatedSessionsAndAggregatesStudentEntries()
    {
        Guid targetStudentId = Guid.NewGuid();
        Guid otherStudentId = Guid.NewGuid();
        Guid classroomId = Guid.NewGuid();

        var sessions = new List<AttendanceSession>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ClassroomId = classroomId,
                Date = new DateOnly(2026, 3, 2),
                LessonCount = 2,
                Entries = new List<AttendanceEntry>
                {
                    new() { Id = Guid.NewGuid(), StudentId = targetStudentId, Status = AttendanceStatus.Present },
                    new() { Id = Guid.NewGuid(), StudentId = otherStudentId, Status = AttendanceStatus.Absent }
                }
            },
            new()
            {
                Id = Guid.NewGuid(),
                ClassroomId = classroomId,
                Date = new DateOnly(2026, 3, 4),
                LessonCount = 2,
                Entries = new List<AttendanceEntry>
                {
                    new() { Id = Guid.NewGuid(), StudentId = targetStudentId, Status = AttendanceStatus.Absent }
                }
            },
            new()
            {
                Id = Guid.NewGuid(),
                ClassroomId = classroomId,
                Date = new DateOnly(2026, 3, 6),
                LessonCount = 4,
                Entries = new List<AttendanceEntry>
                {
                    new() { Id = Guid.NewGuid(), StudentId = otherStudentId, Status = AttendanceStatus.Absent }
                }
            },
            new()
            {
                Id = Guid.NewGuid(),
                ClassroomId = classroomId,
                Date = new DateOnly(2026, 3, 9),
                LessonCount = 6,
                Entries = new List<AttendanceEntry>
                {
                    new() { Id = Guid.NewGuid(), StudentId = targetStudentId, Status = AttendanceStatus.Justified }
                }
            }
        };

        // Target student has 2 present + 2 absent + 6 justified = 10 lessons -> 20% absence (Warning, 80% attendance)
        AttendanceSummary summary = _calculator.CalculateForStudent(targetStudentId, sessions);

        Assert.Equal(10, summary.TotalLessons);
        Assert.Equal(2, summary.PresentLessons);
        Assert.Equal(2, summary.UnexcusedAbsences);
        Assert.Equal(6, summary.JustifiedAbsences);
        Assert.Equal(80.0m, summary.AttendancePercentage);
        Assert.Equal(20.0m, summary.AbsencePercentage);
        Assert.Equal(AttendanceAlertStatus.Warning, summary.AlertStatus);
        Assert.True(summary.MeetsLegalMinimum);
    }

    [Fact]
    public void Calculate_CustomMinAttendancePercentage_AffectsMeetsLegalMinimum()
    {
        var records = new[]
        {
            new AttendanceRecord(78, AttendanceStatus.Present),
            new AttendanceRecord(22, AttendanceStatus.Absent)
        };

        AttendanceSummary summary = _calculator.Calculate(records, minAttendancePercentage: 80.0m);

        Assert.Equal(78.00m, summary.AttendancePercentage);
        Assert.Equal(AttendanceAlertStatus.Warning, summary.AlertStatus);
        Assert.False(summary.MeetsLegalMinimum);
    }

    [Fact]
    public void Calculate_NonPositiveLessonCounts_AreSafelyIgnored()
    {
        var records = new[]
        {
            new AttendanceRecord(0, AttendanceStatus.Absent),
            new AttendanceRecord(-2, AttendanceStatus.Absent),
            new AttendanceRecord(4, AttendanceStatus.Present)
        };

        AttendanceSummary summary = _calculator.Calculate(records);

        Assert.Equal(4, summary.TotalLessons);
        Assert.Equal(4, summary.PresentLessons);
        Assert.Equal(0, summary.UnexcusedAbsences);
        Assert.Equal(100.00m, summary.AttendancePercentage);
        Assert.Equal(AttendanceAlertStatus.Normal, summary.AlertStatus);
    }
}
