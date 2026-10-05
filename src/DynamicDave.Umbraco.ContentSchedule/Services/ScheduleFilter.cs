using DynamicDave.Umbraco.ContentSchedule.Models;

namespace DynamicDave.Umbraco.ContentSchedule.Services;

internal static class ScheduleFilter
{
    public static bool IsInRange(DateTime scheduledUtc, DateTime nowUtc, ScheduleRange range, TimeZoneInfo timeZone)
    {
        switch (range)
        {
            case ScheduleRange.Overdue:
                return scheduledUtc < nowUtc;
            case ScheduleRange.Today:
                if (scheduledUtc < nowUtc) return false;
                var localNow = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, timeZone);
                var localScheduled = TimeZoneInfo.ConvertTimeFromUtc(scheduledUtc, timeZone);
                return localScheduled.Date == localNow.Date;
            case ScheduleRange.Next7Days:
                return scheduledUtc >= nowUtc && scheduledUtc <= nowUtc.AddDays(7);
            case ScheduleRange.Next30Days:
                return scheduledUtc >= nowUtc && scheduledUtc <= nowUtc.AddDays(30);
            default:
                return false;
        }
    }

    public static ScheduleCountsModel Count(IReadOnlyCollection<DateTime> scheduledUtc, DateTime nowUtc, TimeZoneInfo timeZone)
    {
        int CountFor(ScheduleRange range) => scheduledUtc.Count(d => IsInRange(d, nowUtc, range, timeZone));

        return new ScheduleCountsModel
        {
            Today = CountFor(ScheduleRange.Today),
            Next7Days = CountFor(ScheduleRange.Next7Days),
            Next30Days = CountFor(ScheduleRange.Next30Days),
            Overdue = CountFor(ScheduleRange.Overdue),
        };
    }
}
