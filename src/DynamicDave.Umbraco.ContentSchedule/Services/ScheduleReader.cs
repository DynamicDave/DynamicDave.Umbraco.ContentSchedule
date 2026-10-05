using DynamicDave.Umbraco.ContentSchedule.Models;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace DynamicDave.Umbraco.ContentSchedule.Services;

public class ScheduleReader(IContentService contentService, IEntityService entityService, AppCaches appCaches)
{
    public IReadOnlyList<ScheduleItemModel> Read(ScheduleRange range, IUser? user, DateTime nowUtc, TimeZoneInfo timeZone)
    {
        if (user is null) return [];

        var horizon = nowUtc.AddYears(50); // GetContentFor* returns everything scheduled up to this date, including the past
        var candidates = contentService.GetContentForRelease(horizon)
            .Concat(contentService.GetContentForExpiration(horizon))
            .Where(c => !c.Trashed)
            .GroupBy(c => c.Key).Select(g => g.First()).ToList();
        if (candidates.Count == 0) return [];

        // HasContentRootAccess is not publicly available in 17.7; root access is signalled by start node -1.
        var calculated = user.CalculateContentStartNodeIds(entityService, appCaches);
        var startNodes = StartNodeAccess.Resolve(calculated?.Contains(-1) == true, calculated);
        var visible = candidates.Where(c => StartNodeAccess.CanSee(c.Path, startNodes)).ToList();
        var schedules = contentService.GetContentSchedulesByKeys(visible.Select(c => c.Key).ToArray());

        var items = new List<ScheduleItemModel>();
        foreach (var content in visible)
        {
            if (!schedules.TryGetValue(content.Key, out var entries)) continue;
            foreach (var entry in entries)
            {
                var utc = DateTime.SpecifyKind(entry.Date, DateTimeKind.Utc);
                if (!ScheduleFilter.IsInRange(utc, nowUtc, range, timeZone)) continue;

                items.Add(new ScheduleItemModel
                {
                    Key = content.Key,
                    Name = (entry.Culture is { Length: > 0 } ? content.GetCultureName(entry.Culture) : null) ?? content.Name ?? string.Empty,
                    Action = entry.Action == ContentScheduleAction.Release ? "publish" : "unpublish",
                    Culture = string.IsNullOrEmpty(entry.Culture) || entry.Culture == "*" ? null : entry.Culture,
                    ScheduledAt = utc,
                    Status = utc < nowUtc ? "overdue" : "scheduled",
                });
            }
        }

        return items.OrderBy(i => i.ScheduledAt).ToList();
    }
}
