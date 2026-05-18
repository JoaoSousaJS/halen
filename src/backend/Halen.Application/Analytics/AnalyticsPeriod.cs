namespace Halen.Application.Analytics;

public static class AnalyticsPeriod
{
    public static (DateTime Start, DateTime End, DateTime PrevStart, DateTime PrevEnd) ParsePeriod(
        string period, DateTime? now = null)
    {
        var end = now ?? DateTime.UtcNow;

        var start = period.ToLowerInvariant() switch
        {
            "7d" => end.AddDays(-7),
            "90d" => end.AddDays(-90),
            "ytd" => new DateTime(end.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            _ => end.AddDays(-30),
        };

        var duration = end - start;
        var prevEnd = start;
        var prevStart = prevEnd - duration;

        return (start, end, prevStart, prevEnd);
    }
}
