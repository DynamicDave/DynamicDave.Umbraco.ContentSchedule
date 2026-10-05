using DynamicDave.Umbraco.ContentSchedule.Models;
using DynamicDave.Umbraco.ContentSchedule.Services;
using Xunit;

namespace DynamicDave.Umbraco.Tests;

public class ScheduleFilterTests
{
    private static readonly TimeZoneInfo Amsterdam = TimeZoneInfo.CreateCustomTimeZone("test+2", TimeSpan.FromHours(2), "test+2", "test+2");
    private static readonly DateTime Now = new(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc); // 12:00 local

    [Fact]
    public void Today_includes_later_today_local_time()
        => Assert.True(ScheduleFilter.IsInRange(new DateTime(2026, 10, 1, 21, 0, 0, DateTimeKind.Utc), Now, ScheduleRange.Today, Amsterdam));

    [Fact]
    public void Today_excludes_tomorrow_local_time()
        => Assert.False(ScheduleFilter.IsInRange(new DateTime(2026, 10, 1, 22, 30, 0, DateTimeKind.Utc), Now, ScheduleRange.Today, Amsterdam)); // 00:30 local next day

    [Fact]
    public void Today_excludes_the_past()
        => Assert.False(ScheduleFilter.IsInRange(Now.AddMinutes(-1), Now, ScheduleRange.Today, Amsterdam));

    [Fact]
    public void Next7Days_is_inclusive_of_the_boundary()
    {
        Assert.True(ScheduleFilter.IsInRange(Now.AddDays(7), Now, ScheduleRange.Next7Days, Amsterdam));
        Assert.False(ScheduleFilter.IsInRange(Now.AddDays(7).AddSeconds(1), Now, ScheduleRange.Next7Days, Amsterdam));
    }

    [Fact]
    public void Next30Days_includes_day_20_and_excludes_day_31()
    {
        Assert.True(ScheduleFilter.IsInRange(Now.AddDays(20), Now, ScheduleRange.Next30Days, Amsterdam));
        Assert.False(ScheduleFilter.IsInRange(Now.AddDays(31), Now, ScheduleRange.Next30Days, Amsterdam));
    }

    [Fact]
    public void Overdue_is_anything_before_now()
    {
        Assert.True(ScheduleFilter.IsInRange(Now.AddSeconds(-1), Now, ScheduleRange.Overdue, Amsterdam));
        Assert.False(ScheduleFilter.IsInRange(Now.AddSeconds(1), Now, ScheduleRange.Overdue, Amsterdam));
    }

    [Fact]
    public void Count_counts_each_range_independently()
    {
        var dates = new[]
        {
            Now.AddHours(-1),   // overdue
            Now.AddDays(-3),    // overdue
            Now.AddHours(2),    // today, 7, 30
            Now.AddDays(3),     // 7, 30
            Now.AddDays(20),    // 30
            Now.AddDays(60),    // none
        };

        var counts = ScheduleFilter.Count(dates, Now, Amsterdam);

        Assert.Equal(1, counts.Today);
        Assert.Equal(2, counts.Next7Days);
        Assert.Equal(3, counts.Next30Days);
        Assert.Equal(2, counts.Overdue);
    }
}
