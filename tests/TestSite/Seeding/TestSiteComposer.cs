using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;

namespace TestSite.Seeding;

public class TestSiteComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Development only: never seed or alter background jobs elsewhere.
        var environment = builder.Services
            .FirstOrDefault(d => d.ServiceType == typeof(IHostEnvironment))?.ImplementationInstance as IHostEnvironment;
        if (environment is null || !environment.IsDevelopment())
        {
            return;
        }

        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, TestContentSeeder>();

        // Keep overdue schedules visible for manual testing of the "Overdue" filter.
        // ScheduledPublishingJob is internal, so it is matched by name rather than typeof.
        var scheduled = builder.Services
            .Where(d => d.ImplementationType?.FullName?.EndsWith(".ScheduledPublishingJob", StringComparison.Ordinal) == true)
            .ToList();
        foreach (var descriptor in scheduled)
        {
            builder.Services.Remove(descriptor);
        }
    }
}
