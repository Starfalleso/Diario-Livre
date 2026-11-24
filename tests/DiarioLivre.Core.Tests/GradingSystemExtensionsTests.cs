using DiarioLivre.Core.Enums;

namespace DiarioLivre.Core.Tests;

public class GradingSystemExtensionsTests
{
    [Theory]
    [InlineData(GradingSystem.Semester, 2)]
    [InlineData(GradingSystem.Trimester, 3)]
    [InlineData(GradingSystem.Bimonthly, 4)]
    public void GetTermCount_ReturnsExpectedNumberOfPeriods(GradingSystem system, int expectedCount)
    {
        Assert.Equal(expectedCount, system.GetTermCount());
    }

    [Theory]
    [InlineData(GradingSystem.Semester, 2026, 2, 15, 1)]
    [InlineData(GradingSystem.Semester, 2026, 6, 30, 1)]
    [InlineData(GradingSystem.Semester, 2026, 7, 1, 2)]
    [InlineData(GradingSystem.Semester, 2026, 11, 20, 2)]
    [InlineData(GradingSystem.Trimester, 2026, 3, 10, 1)]
    [InlineData(GradingSystem.Trimester, 2026, 5, 1, 2)]
    [InlineData(GradingSystem.Trimester, 2026, 9, 15, 3)]
    [InlineData(GradingSystem.Bimonthly, 2026, 3, 31, 1)]
    [InlineData(GradingSystem.Bimonthly, 2026, 4, 1, 2)]
    [InlineData(GradingSystem.Bimonthly, 2026, 8, 12, 3)]
    [InlineData(GradingSystem.Bimonthly, 2026, 10, 1, 4)]
    public void GetDefaultTermForDate_MapsMonthToExpectedTerm(
        GradingSystem system,
        int year,
        int month,
        int day,
        int expectedTerm)
    {
        var date = new DateOnly(year, month, day);

        int term = system.GetDefaultTermForDate(date);

        Assert.Equal(expectedTerm, term);
    }
}
