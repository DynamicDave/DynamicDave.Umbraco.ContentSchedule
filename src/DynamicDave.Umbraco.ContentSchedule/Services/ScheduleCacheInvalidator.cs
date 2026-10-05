using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace DynamicDave.Umbraco.ContentSchedule.Services;

/// <summary>
/// Drops the cached schedule snapshot whenever content changes (save, publish, schedule, move, delete).
/// The content cache refresher runs on every server in a load-balanced setup, so each server stays current.
/// </summary>
internal sealed class ScheduleCacheInvalidator(AppCaches appCaches) : INotificationHandler<ContentCacheRefresherNotification>
{
    public void Handle(ContentCacheRefresherNotification notification)
        => appCaches.RuntimeCache.ClearByKey(ScheduleReader.SnapshotCacheKey);
}
