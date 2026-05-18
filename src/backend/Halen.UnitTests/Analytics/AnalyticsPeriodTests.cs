using FluentAssertions;
using Halen.Application.Analytics;

namespace Halen.UnitTests.Analytics;

[TestClass]
public class AnalyticsPeriodTests
{
    [TestMethod]
    public void Parse_7d_ReturnsSevenDayRange()
    {
        var now = new DateTime(2026, 5, 18, 10, 0, 0, DateTimeKind.Utc);
        var (start, end, prevStart, prevEnd) = AnalyticsPeriod.ParsePeriod("7d", now);

        end.Should().Be(now);
        start.Should().Be(now.AddDays(-7));
        prevEnd.Should().Be(start);
        prevStart.Should().Be(start.AddDays(-7));
    }

    [TestMethod]
    public void Parse_30d_ReturnsThirtyDayRange()
    {
        var now = new DateTime(2026, 5, 18, 10, 0, 0, DateTimeKind.Utc);
        var (start, end, prevStart, prevEnd) = AnalyticsPeriod.ParsePeriod("30d", now);

        end.Should().Be(now);
        start.Should().Be(now.AddDays(-30));
        prevEnd.Should().Be(start);
        prevStart.Should().Be(start.AddDays(-30));
    }

    [TestMethod]
    public void Parse_90d_ReturnsNinetyDayRange()
    {
        var now = new DateTime(2026, 5, 18, 10, 0, 0, DateTimeKind.Utc);
        var (start, end, prevStart, prevEnd) = AnalyticsPeriod.ParsePeriod("90d", now);

        end.Should().Be(now);
        start.Should().Be(now.AddDays(-90));
        prevEnd.Should().Be(start);
        prevStart.Should().Be(start.AddDays(-90));
    }

    [TestMethod]
    public void Parse_YTD_ReturnsYearToDateRange()
    {
        var now = new DateTime(2026, 5, 18, 10, 0, 0, DateTimeKind.Utc);
        var (start, end, prevStart, prevEnd) = AnalyticsPeriod.ParsePeriod("ytd", now);

        end.Should().Be(now);
        start.Should().Be(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        prevEnd.Should().Be(start);
        var expectedPrevStart = start - (end - start);
        prevStart.Should().BeCloseTo(expectedPrevStart, TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    public void Parse_InvalidString_DefaultsTo30d()
    {
        var now = new DateTime(2026, 5, 18, 10, 0, 0, DateTimeKind.Utc);
        var (start, end, _, _) = AnalyticsPeriod.ParsePeriod("invalid", now);

        end.Should().Be(now);
        start.Should().Be(now.AddDays(-30));
    }

    [TestMethod]
    public void Parse_CaseInsensitive_WorksForYTD()
    {
        var now = new DateTime(2026, 5, 18, 10, 0, 0, DateTimeKind.Utc);
        var (start1, _, _, _) = AnalyticsPeriod.ParsePeriod("YTD", now);
        var (start2, _, _, _) = AnalyticsPeriod.ParsePeriod("ytd", now);

        start1.Should().Be(start2);
    }

    [TestMethod]
    public void PreviousPeriod_HasCorrectDuration()
    {
        var now = new DateTime(2026, 5, 18, 10, 0, 0, DateTimeKind.Utc);
        var (start, end, prevStart, prevEnd) = AnalyticsPeriod.ParsePeriod("30d", now);

        var currentDuration = (end - start).TotalDays;
        var previousDuration = (prevEnd - prevStart).TotalDays;

        previousDuration.Should().Be(currentDuration);
    }
}
