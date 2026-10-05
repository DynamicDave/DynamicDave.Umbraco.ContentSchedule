using DynamicDave.Umbraco.ContentSchedule.Models;
using Umbraco.Cms.Core.Actions;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace DynamicDave.Umbraco.ContentSchedule.Services;

public class ScheduleReader(
    IContentService contentService,
    IEntityService entityService,
    IContentPermissionService contentPermissionService,
    AppCaches appCaches)
{
    internal const string SnapshotCacheKey = "DynamicDave.ContentSchedule.Snapshot";

    // Upper bound only: ScheduleCacheInvalidator clears the snapshot as soon as content changes on any server.
    private static readonly TimeSpan SnapshotDuration = TimeSpan.FromMinutes(5);

    // Keeps the IN (...) clause of the schedule lookup below SQL Server's parameter limit.
    private const int KeyBatchSize = 1000;

    /// <summary>
    /// Returns the schedule entries in <paramref name="range"/> plus the number of entries in every range,
    /// so the dashboard can show counts on all filters from a single request.
    /// </summary>
    public async Task<ScheduleItemsResponse> ReadAsync(ScheduleRange range, IUser? user, DateTime nowUtc, TimeZoneInfo timeZone)
    {
        var all = await ReadAllAsync(user, nowUtc);
        return new ScheduleItemsResponse
        {
            Items = all.Where(i => ScheduleFilter.IsInRange(i.ScheduledAt, nowUtc, range, timeZone)).ToList(),
            Counts = ScheduleFilter.Count(all.Select(i => i.ScheduledAt).ToList(), nowUtc, timeZone),
        };
    }

    private async Task<List<ScheduleItemModel>> ReadAllAsync(IUser? user, DateTime nowUtc)
    {
        if (user is null) return [];

        var snapshot = GetSnapshot();
        if (snapshot.Count == 0) return [];

        // HasContentRootAccess is not publicly available in 17.7; root access is signalled by start node -1.
        var calculated = user.CalculateContentStartNodeIds(entityService, appCaches);
        var startNodes = StartNodeAccess.Resolve(calculated?.Contains(-1) == true, calculated);
        var inStartNodes = snapshot.Where(d => StartNodeAccess.CanSee(d.Path, startNodes)).ToList();
        if (inStartNodes.Count == 0) return [];

        // Same rule as the content tree: only documents the user's groups may browse.
        var browsable = await contentPermissionService.FilterAuthorizedAccessAsync(
            user, inStartNodes.Select(d => d.Key), new HashSet<string> { ActionBrowse.ActionLetter });

        return inStartNodes
            .Where(d => browsable.Contains(d.Key))
            .SelectMany(d => d.Entries.Select(e => new ScheduleItemModel
            {
                Key = d.Key,
                Name = e.Name,
                Action = e.Action,
                Culture = e.Culture,
                ScheduledAt = e.ScheduledAtUtc,
                Status = e.ScheduledAtUtc < nowUtc ? "overdue" : "scheduled",
            }))
            .OrderBy(i => i.ScheduledAt)
            .ToList();
    }

    // All scheduled documents, shared by every user and filtered per request. Loading them means hydrating every
    // scheduled IContent, so the result is cached instead of reloaded on every dashboard tab click.
    private IReadOnlyList<ScheduledDocument> GetSnapshot()
        => appCaches.RuntimeCache.GetCacheItem(SnapshotCacheKey, LoadSnapshot, SnapshotDuration) ?? [];

    private IReadOnlyList<ScheduledDocument> LoadSnapshot()
    {
        var horizon = DateTime.UtcNow.AddYears(50); // GetContentFor* returns everything scheduled up to this date, including the past
        var candidates = contentService.GetContentForRelease(horizon)
            .Concat(contentService.GetContentForExpiration(horizon))
            .Where(c => !c.Trashed)
            .GroupBy(c => c.Key).Select(g => g.First()).ToList();
        if (candidates.Count == 0) return [];

        var schedules = candidates.Select(c => c.Key).Chunk(KeyBatchSize)
            .SelectMany(contentService.GetContentSchedulesByKeys)
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        var documents = new List<ScheduledDocument>();
        foreach (var content in candidates)
        {
            if (!schedules.TryGetValue(content.Key, out var entries)) continue;
            documents.Add(new ScheduledDocument(content.Key, content.Path, entries.Select(entry => new ScheduledEntry(
                Name: (entry.Culture is { Length: > 0 } ? content.GetCultureName(entry.Culture) : null) ?? content.Name ?? string.Empty,
                Action: entry.Action == ContentScheduleAction.Release ? "publish" : "unpublish",
                Culture: string.IsNullOrEmpty(entry.Culture) || entry.Culture == "*" ? null : entry.Culture,
                ScheduledAtUtc: DateTime.SpecifyKind(entry.Date, DateTimeKind.Utc))).ToList()));
        }

        return documents;
    }

    private sealed record ScheduledDocument(Guid Key, string Path, IReadOnlyList<ScheduledEntry> Entries);

    private sealed record ScheduledEntry(string Name, string Action, string? Culture, DateTime ScheduledAtUtc);
}
