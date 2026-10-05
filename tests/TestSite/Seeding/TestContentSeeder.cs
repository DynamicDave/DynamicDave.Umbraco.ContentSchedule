using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

namespace TestSite.Seeding;

public class TestContentSeeder(
    IRuntimeState runtimeState,
    IContentService contentService,
    IContentTypeService contentTypeService,
    ILanguageService languageService,
    ITemplateService templateService,
    IDomainService domainService,
    IShortStringHelper shortStringHelper,
    IScopeProvider scopeProvider,
    IUserService userService,
    IBackOfficeUserManager userManager,
    IConfiguration configuration,
    ILogger<TestContentSeeder> logger)
    : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private static readonly Guid SuperUser = Constants.Security.SuperUserKey;

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        if (runtimeState.Level != RuntimeLevel.Run || contentService.GetRootContent().Any())
        {
            return; // not ready, or already seeded
        }

        logger.LogInformation("Seeding test content");
        await EnsureLanguagesAsync();
        var type = await EnsureContentTypeAsync();

        var home = Create("Home", "Home", null, type, publish: false);
        await EnsureDomainsAsync(home);
        Publish(home);
        var diensten = Create("Diensten", "Services", home, type, publish: true);
        var dienstenSub = Create("Diensten sub", "Services sub", diensten, type, publish: true);
        Schedule(dienstenSub, "nl-NL", release: null, expire: DateTime.UtcNow.AddDays(5));
        await EnsureRestrictedEditorAsync(diensten);

        var now = DateTime.UtcNow;
        Schedule(Create("Black Friday", "Black Friday", home, type, publish: false), "nl-NL", release: now.AddDays(20), expire: null);
        Schedule(Create("Zomeractie", "Summer sale", home, type, publish: true), "nl-NL", release: null, expire: now.AddDays(3));
        Schedule(Create("News article", "News article", home, type, publish: false), "en-US", release: now.AddMinutes(30), expire: null);
        var overdue = Create("Verlopen actie", "Expired promo", home, type, publish: false);
        Schedule(overdue, "nl-NL", release: now.AddDays(1), expire: null);
        MakeScheduleOverdue(overdue.Id);
    }

    /// <summary>Local test editor restricted to the Diensten branch (start node), to verify start-node filtering.</summary>
    private async Task EnsureRestrictedEditorAsync(IContent diensten)
    {
        const string email = "editor@test.local";
        var password = configuration["TestSite:EditorPassword"];
        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("TestSite:EditorPassword is not configured; skipping the restricted editor");
            return;
        }

        var created = await userService.CreateAsync(
            SuperUser,
            new UserCreateModel
            {
                Email = email,
                UserName = email,
                Name = "Test Editor",
                UserGroupKeys = new HashSet<Guid> { Constants.Security.EditorGroupKey },
            },
            approveUser: true);
        if (!created.Success)
        {
            logger.LogWarning("Create editor user failed: {Status}", created.Status);
            return;
        }

        var identityUser = await userManager.FindByEmailAsync(email);
        if (identityUser is null)
        {
            logger.LogWarning("Editor user not found after creation");
            return;
        }

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(identityUser);
        var passwordResult = await userManager.ResetPasswordAsync(identityUser, resetToken, password);
        if (!passwordResult.Succeeded)
        {
            logger.LogWarning("Setting the editor password failed: {Errors}", string.Join(", ", passwordResult.Errors.Select(e => e.Code)));
        }

        var user = created.Result.CreatedUser!;
        var update = await userService.UpdateAsync(
            SuperUser,
            new UserUpdateModel
            {
                ExistingUserKey = user.Key,
                Email = email,
                UserName = email,
                Name = "Test Editor",
                LanguageIsoCode = "en-US",
                UserGroupKeys = new HashSet<Guid> { Constants.Security.EditorGroupKey },
                ContentStartNodeKeys = new HashSet<Guid> { diensten.Key },
                HasContentRootAccess = false,
                MediaStartNodeKeys = new HashSet<Guid>(),
                HasMediaRootAccess = true,
            });
        LogIfFailed("Restrict editor start node", update);
    }

    private async Task EnsureDomainsAsync(IContent home)
    {
        var result = await domainService.UpdateDomainsAsync(
            home.Key,
            new DomainsUpdateModel
            {
                DefaultIsoCode = "nl-NL",
                Domains =
                [
                    new DomainModel { DomainName = "localhost:44411/nl", IsoCode = "nl-NL" },
                    new DomainModel { DomainName = "localhost:44411/en", IsoCode = "en-US" },
                ],
            });
        LogIfFailed("Assign domains to Home", result);
    }

    private void Publish(IContent content)
    {
        var result = contentService.Publish(content, ["*"]);
        if (!result.Success)
        {
            logger.LogWarning("Publishing {Name} failed: {Result}", content.Name, result.Result);
        }
    }

    private void LogIfFailed(string action, dynamic attempt)
    {
        if (!attempt.Success)
        {
            logger.LogWarning("{Action} failed: {Status}", action, (object?)attempt.Status);
        }
    }

    private async Task EnsureLanguagesAsync()
    {
        if (await languageService.GetAsync("en-US") is null)
        {
            LogIfFailed("Create language en-US", await languageService.CreateAsync(new Language("en-US", "English (United States)") { IsDefault = true }, SuperUser));
        }

        var dutch = await languageService.GetAsync("nl-NL");
        if (dutch is null)
        {
            dutch = new Language("nl-NL", "Dutch (Netherlands)");
            LogIfFailed("Create language nl-NL", await languageService.CreateAsync(dutch, SuperUser));
            dutch = await languageService.GetAsync("nl-NL");
        }

        if (dutch is { IsDefault: false })
        {
            dutch.IsDefault = true;
            LogIfFailed("Update language nl-NL", await languageService.UpdateAsync(dutch, SuperUser));
        }
    }

    private async Task<IContentType> EnsureContentTypeAsync()
    {
        var existing = contentTypeService.Get("testPage");
        if (existing is not null)
        {
            return existing;
        }

        var template = (await templateService.CreateAsync(
            "Test Page", "testPage",
            "@inherits Umbraco.Cms.Web.Common.Views.UmbracoViewPage\n<h1>@Model.Name</h1>",
            SuperUser)).Result;

        var type = new ContentType(shortStringHelper, Constants.System.Root)
        {
            Alias = "testPage",
            Name = "Test Page",
            AllowedAsRoot = true,
            Variations = ContentVariation.Culture,
            AllowedTemplates = template is null ? [] : [template],
        };
        if (template is not null)
        {
            type.SetDefaultTemplate(template);
        }

        type.AllowedContentTypes = [new ContentTypeSort(type.Key, 0, type.Alias)];
        LogIfFailed("Create content type testPage", await contentTypeService.CreateAsync(type, SuperUser));
        return type;
    }


    private IContent Create(string nl, string en, IContent? parent, IContentType type, bool publish)
    {
        var content = parent is null
            ? contentService.Create(nl, Constants.System.Root, type)
            : contentService.Create(nl, parent.Key, type.Alias);
        content.SetCultureName(nl, "nl-NL");
        content.SetCultureName(en, "en-US");
        contentService.Save(content);
        if (publish)
        {
            Publish(content);
        }

        return content;
    }

    private void Schedule(IContent content, string culture, DateTime? release, DateTime? expire)
    {
        var schedule = new ContentScheduleCollection();
        schedule.Add(culture, release, expire);
        contentService.Save(content, Constants.Security.SuperUserId, schedule);
    }

    private void MakeScheduleOverdue(int nodeId)
    {
        using var scope = scopeProvider.CreateScope();
        scope.Database.Execute(
            "UPDATE umbracoContentSchedule SET date = @0 WHERE nodeId = @1",
            DateTime.UtcNow.AddDays(-2), nodeId);
        scope.Complete();
    }
}
