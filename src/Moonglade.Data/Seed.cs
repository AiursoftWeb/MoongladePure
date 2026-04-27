using Microsoft.Extensions.Logging;
using MoongladePure.Data.Entities;

namespace MoongladePure.Data;

public class Seed
{
    public static async Task SeedAsync(BlogDbContext dbContext, ILogger logger, int retry = 0)
    {
        var retryForAvailability = retry;

        try
        {
            await dbContext.Tenant.AddRangeAsync(GetTenants());
            await dbContext.Site.AddRangeAsync(GetSites());
            await dbContext.BlogConfiguration.AddRangeAsync(GetBlogConfiguration());
            await dbContext.LocalAccount.AddRangeAsync(GetLocalAccounts());
            await dbContext.SiteMembership.AddRangeAsync(GetSiteMemberships());
            await dbContext.BlogTheme.AddRangeAsync(GetThemes());
            await dbContext.Category.AddRangeAsync(GetCategories());
            await dbContext.Tag.AddRangeAsync(GetTags());
            await dbContext.FriendLink.AddRangeAsync(GetFriendLinks());
            await dbContext.Menu.AddRangeAsync(GetMenus());
            await dbContext.CustomPage.AddRangeAsync(GetPages());

            // Add example post
            var content =
                "MoongladePure removes some dependencies from the original Moonglade and can be deployed completely on-premises without coupling to any particular cloud.";

            var postId = Guid.NewGuid();
            var now = DateTime.UtcNow;
            var post = new PostEntity
            {
                Id = postId,
                SiteId = SystemIds.DefaultSiteId,
                Title = "Welcome to MoongladePure",
                Slug = "welcome-to-moonglade-pure",
                Author = "admin",
                RawContent = content,
                CommentEnabled = true,
                CreateTimeUtc = now,
                ContentAbstractZh = content,
                ContentAbstractEn = content,
                IsPublished = true,
                IsFeatured = true,
                IsFeedIncluded = true,
                LastModifiedUtc = now,
                PubDateUtc = now,
                ContentLanguageCode = "en-us",
                HashCheckSum = -1688639577,
                IsOriginal = true,
                PostExtension = new()
                {
                    Hits = 0,
                    Likes = 0
                },
                Tags = dbContext.Tag.ToList(),
                PostCategory = dbContext.PostCategory.ToList()
            };

            await dbContext.Post.AddAsync(post);
            await dbContext.PostContent.AddAsync(new()
            {
                SiteId = SystemIds.DefaultSiteId,
                PostId = postId,
                CultureCode = "en-US",
                ContentKind = PostContentKind.RawMarkdown,
                Body = content,
                Abstract = content,
                IsOriginal = true,
                CreatedAtUtc = now
            });
            await dbContext.PostRoute.AddAsync(new()
            {
                SiteId = SystemIds.DefaultSiteId,
                PostId = postId,
                RouteDate = now.Date,
                Slug = "welcome-to-moonglade-pure",
                HashCheckSum = -1688639577,
                IsCanonical = true,
                CreatedAtUtc = now
            });

            await dbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            if (retryForAvailability >= 10) throw;

            retryForAvailability++;

            logger.LogError(e, "Failed to seed!");
            await SeedAsync(dbContext, logger, retryForAvailability);
            throw;
        }
    }

    private static IEnumerable<BlogConfigurationEntity> GetBlogConfiguration()
    {
        return new List<BlogConfigurationEntity>
        {
            new()
            {
                Id = 1,
                SiteId = SystemIds.DefaultSiteId,
                CfgKey = "ContentSettings",
                CfgValue = "{\"EnableComments\":true,\"RequireCommentReview\":false,\"EnableWordFilter\":false,\"PostListPageSize\":10,\"HotTagAmount\":10,\"DisharmonyWords\":\"fuck|shit\",\"ShowCalloutSection\":false,\"CalloutSectionHtmlPitch\":\"\"}",
                LastModifiedTimeUtc = DateTime.UtcNow
            },
            new()
            {
                Id = 2,
                SiteId = SystemIds.DefaultSiteId,
                CfgKey = "NotificationSettings",
                CfgValue = "{\"EnableEmailSending\":false,\"EnableSsl\":true,\"SendEmailOnCommentReply\":true,\"SendEmailOnNewComment\":true,\"SmtpServerPort\":587,\"AdminEmail\":\"\",\"EmailDisplayName\":\"MoongladePure\",\"SmtpPassword\":\"\",\"SmtpServer\":\"\",\"SmtpUserName\":\"\",\"BannedMailDomain\":\"\"}",
                LastModifiedTimeUtc = DateTime.UtcNow
            },
            new()
            {
                Id = 3,
                SiteId = SystemIds.DefaultSiteId,
                CfgKey = "FeedSettings",
                CfgValue = "{\"RssItemCount\":20,\"RssCopyright\":\"(c) {year} MoongladePure\",\"RssDescription\":\"Latest posts from MoongladePure\",\"RssTitle\":\"MoongladePure\",\"AuthorName\":\"Admin\",\"UseFullContent\":false}",
                LastModifiedTimeUtc = DateTime.UtcNow
            },
            new()
            {
                Id = 4,
                SiteId = SystemIds.DefaultSiteId,
                CfgKey = "GeneralSettings",
                CfgValue = "{\"OwnerName\":\"Admin\",\"OwnerEmail\":\"admin@sample.email\",\"Description\":\"MoongladePure Admin\",\"ShortDescription\":\"MoongladePure Admin\",\"AvatarBase64\":\"\",\"SiteTitle\":\"MoongladePure\",\"LogoText\":\"moongladepure\",\"MetaKeyword\":\"moongladepure\",\"MetaDescription\":\"Just another .NET blog system\",\"Copyright\":\"[c] 2023 - [year] MoongladePure\",\"SideBarCustomizedHtmlPitch\":\"\",\"FooterCustomizedHtmlPitch\":\"\",\"UserTimeZoneBaseUtcOffset\":\"08:00:00\",\"TimeZoneId\":\"China Standard Time\",\"AutoDarkLightTheme\":true,\"ThemeId\":1}",
                LastModifiedTimeUtc = DateTime.UtcNow
            },
            new()
            {
                Id = 5,
                SiteId = SystemIds.DefaultSiteId,
                CfgKey = "ImageSettings",
                CfgValue = "{\"IsWatermarkEnabled\":true,\"KeepOriginImage\":false,\"WatermarkFontSize\":20,\"WatermarkText\":\"MoongladePure\",\"UseFriendlyNotFoundImage\":true}",
                LastModifiedTimeUtc = DateTime.UtcNow
            },
            new()
            {
                Id = 6,
                SiteId = SystemIds.DefaultSiteId,
                CfgKey = "AdvancedSettings",
                CfgValue = "{\"DNSPrefetchEndpoint\":\"\",\"EnableOpenSearch\":true,\"WarnExternalLink\":false,\"AllowScriptsInPage\":false,\"ShowAdminLoginButton\":true,\"EnablePostRawEndpoint\":true}",
                LastModifiedTimeUtc = DateTime.UtcNow
            },
            new()
            {
                Id = 7,
                SiteId = SystemIds.DefaultSiteId,
                CfgKey = "CustomStyleSheetSettings",
                CfgValue = "{\"EnableCustomCss\":false,\"CssCode\":\"\"}",
                LastModifiedTimeUtc = DateTime.UtcNow
            }
        };
    }

    private static IEnumerable<TenantEntity> GetTenants()
    {
        return new List<TenantEntity>
        {
            new()
            {
                Id = SystemIds.DefaultTenantId,
                Name = "Default Tenant",
                Status = TenantStatus.Active,
                CreatedAtUtc = DateTime.UtcNow
            }
        };
    }

    private static IEnumerable<SiteEntity> GetSites()
    {
        return new List<SiteEntity>
        {
            new()
            {
                Id = SystemIds.DefaultSiteId,
                TenantId = SystemIds.DefaultTenantId,
                Name = "MoongladePure",
                Slug = "default",
                Status = SiteStatus.Active,
                DefaultCulture = "en-US",
                TimeZoneId = "China Standard Time",
                CreatedAtUtc = DateTime.UtcNow
            }
        };
    }

    private static IEnumerable<LocalAccountEntity> GetLocalAccounts()
    {
        return new List<LocalAccountEntity>
        {
            new()
            {
                Id = SystemIds.DefaultAdminUserId,
                TenantId = SystemIds.DefaultTenantId,
                Username = "admin",
                NormalizedUsername = "admin",
                PasswordHash = "JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=",
                CreateTimeUtc = DateTime.UtcNow
            }
        };
    }

    private static IEnumerable<SiteMembershipEntity> GetSiteMemberships()
    {
        return new List<SiteMembershipEntity>
        {
            new()
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                SiteId = SystemIds.DefaultSiteId,
                UserId = SystemIds.DefaultAdminUserId,
                Role = SiteRole.Owner,
                DisplayName = "admin",
                CreatedAtUtc = DateTime.UtcNow
            }
        };
    }

    private static IEnumerable<BlogThemeEntity> GetThemes()
    {
        return new List<BlogThemeEntity>
        {
            new ()
            {
                SiteId = null, ThemeName = "Word Blue", CssRules = "{\"--accent-color1\": \"#2a579a\",\"--accent-color2\": \"#1a365f\",\"--accent-color3\": \"#3e6db5\"}", ThemeType = 0
            },
            new ()
            {
                SiteId = null, ThemeName = "Excel Green", CssRules = "{\"--accent-color1\": \"#165331\",\"--accent-color2\": \"#0E351F\",\"--accent-color3\": \"#0E703A\"}", ThemeType = 0
            },
            new ()
            {
                SiteId = null, ThemeName = "PowerPoint Orange", CssRules = "{\"--accent-color1\": \"#983B22\",\"--accent-color2\": \"#622616\",\"--accent-color3\": \"#C43E1C\"}", ThemeType = 0
            },
            new ()
            {
                SiteId = null, ThemeName = "OneNote Purple", CssRules = "{\"--accent-color1\": \"#663276\",\"--accent-color2\": \"#52285E\",\"--accent-color3\": \"#7719AA\"}", ThemeType = 0
            },
            new ()
            {
                SiteId = null, ThemeName = "Outlook Blue", CssRules = "{\"--accent-color1\": \"#035AA6\",\"--accent-color2\": \"#032B51\",\"--accent-color3\": \"#006CBF\"}", ThemeType = 0
            },
            new ()
            {
                SiteId = null, ThemeName = "China Red", CssRules = "{\"--accent-color1\": \"#800900\",\"--accent-color2\": \"#5d120d\",\"--accent-color3\": \"#c5170a\"}", ThemeType = 0
            },
            new ()
            {
                SiteId = null, ThemeName = "Indian Curry", CssRules = "{\"--accent-color1\": \"rgb(128 84 3)\",\"--accent-color2\": \"rgb(95 62 0)\",\"--accent-color3\": \"rgb(208 142 19)\"}", ThemeType = 0
            },
            new ()
            {
                SiteId = null, ThemeName = "Metal Blue", CssRules = "{\"--accent-color1\": \"#4E5967\",\"--accent-color2\": \"#333942\",\"--accent-color3\": \"#6e7c8e\"}", ThemeType = 0
            }
        };
    }

    private static IEnumerable<CategoryEntity> GetCategories()
    {
        return new List<CategoryEntity>
        {
            new()
            {
                Id = Guid.Parse("b0c15707-dfc8-4b09-9aa0-5bfca744c50b"),
                SiteId = SystemIds.DefaultSiteId,
                DisplayName = "Default",
                Note = "Default Category",
                RouteName = "default"
            }
        };
    }

    private static IEnumerable<TagEntity> GetTags()
    {
        return new List<TagEntity>
        {
            new() { DisplayName = "MoongladePure", NormalizedName = "moongladepure" },
            new() { DisplayName = ".NET", NormalizedName = "dot-net" }
        };
    }

    private static IEnumerable<FriendLinkEntity> GetFriendLinks()
    {
        return new List<FriendLinkEntity>
        {
            new()
            {
                Id = Guid.NewGuid(),
                SiteId = SystemIds.DefaultSiteId,
                Title = "Anduin.Xue",
                LinkUrl = "https://anduin.aiursoft.com"
            }
        };
    }

    private static IEnumerable<MenuEntity> GetMenus()
    {
        return new List<MenuEntity>
        {
            new()
            {
                Id = Guid.NewGuid(),
                SiteId = SystemIds.DefaultSiteId,
                DisplayOrder = 0,
                IsOpenInNewTab = false,
                Icon = "icon-star-full",
                Title = "About",
                Url = "/page/about"
            }
        };
    }

    private static IEnumerable<PageEntity> GetPages()
    {
        return new List<PageEntity>
        {
            new()
            {
                Id = Guid.NewGuid(),
                SiteId = SystemIds.DefaultSiteId,
                Title = "About",
                Slug = "about",
                MetaDescription = "An Empty About Page",
                HtmlContent = "<h3>An Empty About Page</h3>",
                HideSidebar = true,
                IsPublished = true,
                CreateTimeUtc = DateTime.UtcNow,
                UpdateTimeUtc = DateTime.UtcNow
            }
        };
    }
}
