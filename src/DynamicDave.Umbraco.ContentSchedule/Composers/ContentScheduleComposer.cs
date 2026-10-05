using DynamicDave.Umbraco.ContentSchedule.Services;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;

namespace DynamicDave.Umbraco.ContentSchedule.Composers;

public class ContentScheduleComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddScoped<ScheduleReader>();
        builder.AddNotificationHandler<ContentCacheRefresherNotification, ScheduleCacheInvalidator>();
    }
}
