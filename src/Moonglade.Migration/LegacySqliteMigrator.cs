using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Sqlite;
using MoongladePure.Utils;

namespace MoongladePure.Migration;

internal static class LegacySqliteMigrator
{
    public static LegacySqliteMigrationResult Migrate(MigrationOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.TargetPath))
        {
            throw new InvalidOperationException("Target path is required.");
        }

        PrepareTargetFile(options.TargetPath, options.Overwrite);

        using var source = LegacySqliteDatabase.OpenReadOnly(options.SourcePath);
        using var target = CreateTargetContext(options.TargetPath);
        target.Database.Migrate();

        var result = new LegacySqliteMigrationResult(options.SourcePath, options.TargetPath, DateTimeOffset.UtcNow);
        var now = DateTime.UtcNow;
        var accountIds = MigratePlatform(target, source, now, result);
        var categoryIds = MigrateCategories(target, source, result);
        var tagIds = MigrateTags(target, source, result);

        MigrateSettings(target, source, now, result);
        MigrateThemes(target, source, result);
        MigrateAssets(target, source, result);
        MigrateFriendLinks(target, source, result);
        var menuIds = MigrateMenus(target, source, result);
        MigrateSubMenus(target, source, menuIds, result);
        MigratePages(target, source, now, result);
        target.SaveChanges();

        var postIds = MigratePosts(target, source, now, result);
        target.SaveChanges();

        MigratePostCategories(target, source, postIds, categoryIds, result);
        MigratePostTags(target, source, postIds, tagIds, result);
        MigrateComments(target, source, postIds, result);
        target.SaveChanges();

        var commentIds = target.Comment.Select(c => c.Id).ToHashSet();
        MigrateCommentReplies(target, source, commentIds, result);
        target.SaveChanges();

        result.AddInfo("AdminAccounts", accountIds.Count);
        result.AddInfo("TargetRows", CountTargetRows(target));
        return result;
    }

    private static void PrepareTargetFile(string targetPath, bool overwrite)
    {
        if (File.Exists(targetPath) && !overwrite)
        {
            throw new IOException($"Target database already exists. Use --overwrite to replace it: {targetPath}");
        }

        if (File.Exists(targetPath))
        {
            File.Delete(targetPath);
        }

        var directory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static SqliteContext CreateTargetContext(string targetPath)
    {
        var options = new DbContextOptionsBuilder<SqliteContext>()
            .UseSqlite($"Data Source={targetPath}")
            .Options;

        return new SqliteContext(options);
    }

    private static Dictionary<Guid, Guid> MigratePlatform(SqliteContext target, LegacySqliteDatabase source, DateTime now, LegacySqliteMigrationResult result)
    {
        var generalSettings = ReadGeneralSettings(source);
        var siteName = GetJsonString(generalSettings, "SiteTitle") ?? "MoongladePure";
        var timeZoneId = GetJsonString(generalSettings, "TimeZoneId") ?? "UTC";

        target.Tenant.Add(new TenantEntity
        {
            Id = SystemIds.DefaultTenantId,
            Name = "Default Tenant",
            Status = TenantStatus.Active,
            CreatedAtUtc = now
        });
        target.Site.Add(new SiteEntity
        {
            Id = SystemIds.DefaultSiteId,
            TenantId = SystemIds.DefaultTenantId,
            Name = siteName,
            Slug = "default",
            Status = SiteStatus.Active,
            DefaultCulture = "en-US",
            TimeZoneId = timeZoneId,
            CreatedAtUtc = now
        });

        var accountIds = MigrateAccounts(target, source, generalSettings, now, result);
        return accountIds;
    }

    private static Dictionary<Guid, Guid> MigrateAccounts(
        SqliteContext target,
        LegacySqliteDatabase source,
        JsonElement? generalSettings,
        DateTime now,
        LegacySqliteMigrationResult result)
    {
        var rows = source.ReadRows("LocalAccount");
        var map = new Dictionary<Guid, Guid>();
        var firstAccount = true;

        foreach (var row in rows)
        {
            var oldId = row.GetGuid("Id") ?? Guid.NewGuid();
            var newId = firstAccount ? SystemIds.DefaultAdminUserId : oldId;
            var username = NormalizeRequired(row.GetString("Username", "UserName", "Name"), "admin");

            target.LocalAccount.Add(new LocalAccountEntity
            {
                Id = newId,
                TenantId = SystemIds.DefaultTenantId,
                Username = TrimToMax(username, 32),
                NormalizedUsername = TrimToMax(row.GetString("NormalizedUsername") ?? username.ToLowerInvariant(), 32),
                Email = TrimToMax(row.GetString("Email"), 128),
                NormalizedEmail = TrimToMax(row.GetString("NormalizedEmail") ?? row.GetString("Email")?.ToLowerInvariant(), 128),
                PasswordSalt = row.GetString("PasswordSalt"),
                PasswordHash = TrimToMax(row.GetString("PasswordHash"), 256),
                LastLoginTimeUtc = row.GetDateTime("LastLoginTimeUtc", "LastLoginTime"),
                LastLoginIp = TrimToMax(row.GetString("LastLoginIp", "LastLoginIP"), 64),
                CreateTimeUtc = row.GetDateTime("CreateTimeUtc", "CreatedTimeUtc", "CreateTime") ?? now
            });
            target.SiteMembership.Add(new SiteMembershipEntity
            {
                Id = firstAccount ? Guid.Parse("33333333-3333-3333-3333-333333333333") : Guid.NewGuid(),
                SiteId = SystemIds.DefaultSiteId,
                UserId = newId,
                Role = firstAccount ? SiteRole.Owner : SiteRole.Admin,
                DisplayName = TrimToMax(username, 64),
                CreatedAtUtc = now
            });

            map[oldId] = newId;
            firstAccount = false;
            result.Increment("LocalAccount");
        }

        if (rows.Count > 0)
        {
            return map;
        }

        var ownerName = GetJsonString(generalSettings, "OwnerName") ?? "admin";
        target.LocalAccount.Add(new LocalAccountEntity
        {
            Id = SystemIds.DefaultAdminUserId,
            TenantId = SystemIds.DefaultTenantId,
            Username = TrimToMax(ownerName, 32),
            NormalizedUsername = TrimToMax(ownerName.ToLowerInvariant(), 32),
            PasswordHash = "JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=",
            CreateTimeUtc = now
        });
        target.SiteMembership.Add(new SiteMembershipEntity
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            SiteId = SystemIds.DefaultSiteId,
            UserId = SystemIds.DefaultAdminUserId,
            Role = SiteRole.Owner,
            DisplayName = TrimToMax(ownerName, 64),
            CreatedAtUtc = now
        });
        result.Increment("LocalAccount");
        return new Dictionary<Guid, Guid> { [SystemIds.DefaultAdminUserId] = SystemIds.DefaultAdminUserId };
    }

    private static Dictionary<Guid, Guid> MigrateCategories(SqliteContext target, LegacySqliteDatabase source, LegacySqliteMigrationResult result)
    {
        var map = new Dictionary<Guid, Guid>();

        foreach (var row in source.ReadRows("Category"))
        {
            var id = row.GetGuid("Id") ?? Guid.NewGuid();
            target.Category.Add(new CategoryEntity
            {
                Id = id,
                SiteId = SystemIds.DefaultSiteId,
                RouteName = TrimToMax(NormalizeRequired(row.GetString("RouteName"), $"category-{id:N}"[..20]), 64),
                DisplayName = TrimToMax(NormalizeRequired(row.GetString("DisplayName", "Name"), "Untitled"), 64),
                Note = TrimToMax(row.GetString("Note", "Description"), 128)
            });
            map[id] = id;
            result.Increment("Category");
        }

        return map;
    }

    private static Dictionary<int, int> MigrateTags(SqliteContext target, LegacySqliteDatabase source, LegacySqliteMigrationResult result)
    {
        var map = new Dictionary<int, int>();

        foreach (var row in source.ReadRows("Tag"))
        {
            var id = row.GetInt32("Id") ?? 0;
            var displayName = TrimToMax(NormalizeRequired(row.GetString("DisplayName", "Name"), "tag"), 32) ?? "tag";
            var normalizedName = TrimToMax(NormalizeRequired(row.GetString("NormalizedName"), displayName.ToLowerInvariant()), 32) ?? displayName.ToLowerInvariant();
            var entity = new TagEntity
            {
                SiteId = SystemIds.DefaultSiteId,
                DisplayName = displayName,
                NormalizedName = normalizedName
            };

            if (id > 0)
            {
                entity.Id = id;
                map[id] = id;
            }

            target.Tag.Add(entity);
            result.Increment("Tag");
        }

        return map;
    }

    private static void MigrateSettings(SqliteContext target, LegacySqliteDatabase source, DateTime now, LegacySqliteMigrationResult result)
    {
        var rows = source.ReadRows("BlogConfiguration");
        if (rows.Count == 0)
        {
            AddDefaultSettings(target, now, result);
            return;
        }

        foreach (var row in rows)
        {
            target.BlogConfiguration.Add(new BlogConfigurationEntity
            {
                Id = row.GetInt32("Id") ?? 0,
                SiteId = SystemIds.DefaultSiteId,
                CfgKey = TrimToMax(NormalizeRequired(row.GetString("CfgKey", "Key"), "Unknown"), 64),
                CfgValue = row.GetString("CfgValue", "Value") ?? string.Empty,
                SchemaVersion = row.GetInt32("SchemaVersion") ?? 1,
                LastModifiedTimeUtc = row.GetDateTime("LastModifiedTimeUtc", "UpdateTimeUtc") ?? now
            });
            result.Increment("BlogConfiguration");
        }
    }

    private static void AddDefaultSettings(SqliteContext target, DateTime now, LegacySqliteMigrationResult result)
    {
        var settings = new Dictionary<string, string>
        {
            ["ContentSettings"] = "{\"EnableComments\":true,\"RequireCommentReview\":false,\"EnableWordFilter\":false,\"PostListPageSize\":10,\"HotTagAmount\":10,\"DisharmonyWords\":\"fuck|shit\",\"ShowCalloutSection\":false,\"CalloutSectionHtmlPitch\":\"\"}",
            ["NotificationSettings"] = "{\"EnableEmailSending\":false,\"EnableSsl\":true,\"SendEmailOnCommentReply\":true,\"SendEmailOnNewComment\":true,\"SmtpServerPort\":587,\"AdminEmail\":\"\",\"EmailDisplayName\":\"MoongladePure\",\"SmtpPassword\":\"\",\"SmtpServer\":\"\",\"SmtpUserName\":\"\",\"BannedMailDomain\":\"\"}",
            ["FeedSettings"] = "{\"RssItemCount\":20,\"RssCopyright\":\"(c) {year} MoongladePure\",\"RssDescription\":\"Latest posts from MoongladePure\",\"RssTitle\":\"MoongladePure\",\"AuthorName\":\"Admin\",\"UseFullContent\":false}",
            ["GeneralSettings"] = "{\"OwnerName\":\"Admin\",\"OwnerEmail\":\"admin@sample.email\",\"Description\":\"MoongladePure Admin\",\"ShortDescription\":\"MoongladePure Admin\",\"AvatarBase64\":\"\",\"SiteTitle\":\"MoongladePure\",\"LogoText\":\"moongladepure\",\"MetaKeyword\":\"moongladepure\",\"MetaDescription\":\"Just another .NET blog system\",\"Copyright\":\"[c] 2023 - [year] MoongladePure\",\"SideBarCustomizedHtmlPitch\":\"\",\"FooterCustomizedHtmlPitch\":\"\",\"UserTimeZoneBaseUtcOffset\":\"08:00:00\",\"TimeZoneId\":\"China Standard Time\",\"AutoDarkLightTheme\":true,\"ThemeId\":1}",
            ["ImageSettings"] = "{\"IsWatermarkEnabled\":true,\"KeepOriginImage\":false,\"WatermarkFontSize\":20,\"WatermarkText\":\"MoongladePure\",\"UseFriendlyNotFoundImage\":true}",
            ["AdvancedSettings"] = "{\"DNSPrefetchEndpoint\":\"\",\"EnableOpenSearch\":true,\"WarnExternalLink\":false,\"AllowScriptsInPage\":false,\"ShowAdminLoginButton\":true,\"EnablePostRawEndpoint\":true}",
            ["CustomStyleSheetSettings"] = "{\"EnableCustomCss\":false,\"CssCode\":\"\"}"
        };

        foreach (var setting in settings)
        {
            target.BlogConfiguration.Add(new BlogConfigurationEntity
            {
                SiteId = SystemIds.DefaultSiteId,
                CfgKey = setting.Key,
                CfgValue = setting.Value,
                SchemaVersion = 1,
                LastModifiedTimeUtc = now
            });
            result.Increment("BlogConfiguration");
        }
    }

    private static void MigrateThemes(SqliteContext target, LegacySqliteDatabase source, LegacySqliteMigrationResult result)
    {
        foreach (var row in source.ReadRows("BlogTheme"))
        {
            target.BlogTheme.Add(new BlogThemeEntity
            {
                Id = row.GetInt32("Id") ?? 0,
                SiteId = row.HasValue("SiteId") ? row.GetGuid("SiteId") : null,
                ThemeName = TrimToMax(NormalizeRequired(row.GetString("ThemeName"), "Theme"), 32),
                CssRules = row.GetString("CssRules") ?? string.Empty,
                AdditionalProps = row.GetString("AdditionalProps") ?? string.Empty,
                ThemeType = (ThemeType)(row.GetInt32("ThemeType") ?? 0)
            });
            result.Increment("BlogTheme");
        }
    }

    private static void MigrateAssets(SqliteContext target, LegacySqliteDatabase source, LegacySqliteMigrationResult result)
    {
        foreach (var row in source.ReadRows("BlogAsset"))
        {
            target.BlogAsset.Add(new BlogAssetEntity
            {
                Id = row.GetGuid("Id") ?? Guid.NewGuid(),
                SiteId = SystemIds.DefaultSiteId,
                Base64Data = row.GetString("Base64Data") ?? string.Empty,
                LastModifiedTimeUtc = row.GetDateTime("LastModifiedTimeUtc") ?? DateTime.UtcNow
            });
            result.Increment("BlogAsset");
        }
    }

    private static void MigrateFriendLinks(SqliteContext target, LegacySqliteDatabase source, LegacySqliteMigrationResult result)
    {
        foreach (var row in source.ReadRows("FriendLink"))
        {
            target.FriendLink.Add(new FriendLinkEntity
            {
                Id = row.GetGuid("Id") ?? Guid.NewGuid(),
                SiteId = SystemIds.DefaultSiteId,
                Title = TrimToMax(NormalizeRequired(row.GetString("Title", "Name"), "Link"), 64),
                LinkUrl = TrimToMax(NormalizeRequired(row.GetString("LinkUrl", "Url"), "#"), 512)
            });
            result.Increment("FriendLink");
        }
    }

    private static Dictionary<Guid, Guid> MigrateMenus(SqliteContext target, LegacySqliteDatabase source, LegacySqliteMigrationResult result)
    {
        var map = new Dictionary<Guid, Guid>();

        foreach (var row in source.ReadRows("Menu"))
        {
            var id = row.GetGuid("Id") ?? Guid.NewGuid();
            target.Menu.Add(new MenuEntity
            {
                Id = id,
                SiteId = SystemIds.DefaultSiteId,
                Title = TrimToMax(NormalizeRequired(row.GetString("Title"), "Menu"), 64),
                Url = TrimToMax(row.GetString("Url"), 256),
                Icon = TrimToMax(row.GetString("Icon"), 64),
                DisplayOrder = row.GetInt32("DisplayOrder") ?? 0,
                IsOpenInNewTab = row.GetBool(false, "IsOpenInNewTab", "OpenInNewTab")
            });
            map[id] = id;
            result.Increment("Menu");
        }

        return map;
    }

    private static void MigrateSubMenus(SqliteContext target, LegacySqliteDatabase source, Dictionary<Guid, Guid> menuIds, LegacySqliteMigrationResult result)
    {
        foreach (var row in source.ReadRows("SubMenu"))
        {
            var oldMenuId = row.GetGuid("MenuId");
            if (!oldMenuId.HasValue || !menuIds.TryGetValue(oldMenuId.Value, out var menuId))
            {
                result.Skip("SubMenu", "Parent menu is missing.");
                continue;
            }

            target.SubMenu.Add(new SubMenuEntity
            {
                Id = row.GetGuid("Id") ?? Guid.NewGuid(),
                SiteId = SystemIds.DefaultSiteId,
                MenuId = menuId,
                Title = TrimToMax(NormalizeRequired(row.GetString("Title"), "Sub menu"), 64),
                Url = TrimToMax(row.GetString("Url"), 256),
                IsOpenInNewTab = row.GetBool(false, "IsOpenInNewTab", "OpenInNewTab")
            });
            result.Increment("SubMenu");
        }
    }

    private static void MigratePages(SqliteContext target, LegacySqliteDatabase source, DateTime now, LegacySqliteMigrationResult result)
    {
        foreach (var row in source.ReadRows("CustomPage"))
        {
            var id = row.GetGuid("Id") ?? Guid.NewGuid();
            target.CustomPage.Add(new PageEntity
            {
                Id = id,
                SiteId = SystemIds.DefaultSiteId,
                Title = TrimToMax(NormalizeRequired(row.GetString("Title"), "Untitled"), 128),
                Slug = TrimToMax(NormalizeRequired(row.GetString("Slug"), $"page-{id:N}"[..20]), 128),
                MetaDescription = TrimToMax(row.GetString("MetaDescription"), 256),
                HtmlContent = row.GetString("HtmlContent", "Content") ?? string.Empty,
                CssContent = row.GetString("CssContent", "Css") ?? string.Empty,
                HideSidebar = row.GetBool(false, "HideSidebar"),
                IsPublished = row.GetBool(true, "IsPublished"),
                CreateTimeUtc = row.GetDateTime("CreateTimeUtc", "CreatedTimeUtc") ?? now,
                UpdateTimeUtc = row.GetDateTime("UpdateTimeUtc", "LastModifiedTimeUtc")
            });
            result.Increment("CustomPage");
        }
    }

    private static Dictionary<Guid, Guid> MigratePosts(SqliteContext target, LegacySqliteDatabase source, DateTime now, LegacySqliteMigrationResult result)
    {
        var map = new Dictionary<Guid, Guid>();

        foreach (var row in source.ReadRows("Post"))
        {
            var id = row.GetGuid("Id") ?? Guid.NewGuid();
            var slug = TrimToMax(NormalizeRequired(row.GetString("Slug"), $"post-{id:N}"[..20]), 128) ?? $"post-{id:N}"[..20];
            var pubDateUtc = row.GetDateTime("PubDateUtc", "PublishTimeUtc");
            var hashCheckSum = row.GetInt32("HashCheckSum") ?? ComputePostCheckSum(slug, pubDateUtc);
            var rawContent = row.GetString("RawContent", "PostContent", "Content") ?? string.Empty;
            var language = TrimToMax(row.GetString("ContentLanguageCode", "LanguageCode") ?? "en-US", 16);

            target.Post.Add(new PostEntity
            {
                Id = id,
                SiteId = SystemIds.DefaultSiteId,
                Title = TrimToMax(NormalizeRequired(row.GetString("Title"), "Untitled"), 128),
                Slug = slug,
                Author = TrimToMax(row.GetString("Author"), 64),
                RawContent = rawContent,
                LocalizedChineseContent = row.GetString("LocalizedChineseContent") ?? string.Empty,
                LocalizedEnglishContent = row.GetString("LocalizedEnglishContent") ?? string.Empty,
                CommentEnabled = row.GetBool(true, "CommentEnabled", "EnableComment"),
                CreateTimeUtc = row.GetDateTime("CreateTimeUtc", "CreatedTimeUtc") ?? now,
                ContentAbstractZh = TrimToMax(row.GetString("ContentAbstractZh", "ContentAbstract") ?? "...", 1024),
                ContentAbstractEn = TrimToMax(row.GetString("ContentAbstractEn", "ContentAbstract") ?? "...", 1024),
                ContentLanguageCode = language,
                IsFeedIncluded = row.GetBool(true, "IsFeedIncluded", "FeedIncluded"),
                LocalizeJobRunAt = row.GetDateTime("LocalizeJobRunAt"),
                PubDateUtc = pubDateUtc,
                LastModifiedUtc = row.GetDateTime("LastModifiedUtc", "UpdateTimeUtc"),
                IsPublished = row.GetBool(false, "IsPublished"),
                IsDeleted = row.GetBool(false, "IsDeleted"),
                IsOriginal = row.GetBool(true, "IsOriginal"),
                OriginLink = TrimToMax(row.GetString("OriginLink"), 512),
                HeroImageUrl = TrimToMax(row.GetString("HeroImageUrl"), 512),
                InlineCss = TrimToMax(row.GetString("InlineCss"), 2048),
                IsFeatured = row.GetBool(false, "IsFeatured"),
                HashCheckSum = hashCheckSum,
                PostExtension = new PostExtensionEntity
                {
                    SiteId = SystemIds.DefaultSiteId,
                    PostId = id,
                    Hits = 0,
                    Likes = 0
                }
            });
            target.PostContent.Add(new PostContentEntity
            {
                SiteId = SystemIds.DefaultSiteId,
                PostId = id,
                CultureCode = language,
                ContentKind = PostContentKind.RawMarkdown,
                Body = rawContent,
                Abstract = row.GetString("ContentAbstractEn", "ContentAbstract"),
                IsOriginal = true,
                CreatedAtUtc = row.GetDateTime("CreateTimeUtc", "CreatedTimeUtc") ?? now,
                UpdatedAtUtc = row.GetDateTime("LastModifiedUtc", "UpdateTimeUtc")
            });
            AddLegacyAiArtifacts(target, row, id, now);

            if (pubDateUtc.HasValue)
            {
                target.PostRoute.Add(new PostRouteEntity
                {
                    SiteId = SystemIds.DefaultSiteId,
                    PostId = id,
                    RouteDate = pubDateUtc.Value.Date,
                    Slug = slug,
                    HashCheckSum = hashCheckSum,
                    IsCanonical = true,
                    CreatedAtUtc = row.GetDateTime("CreateTimeUtc", "CreatedTimeUtc") ?? now
                });
            }

            map[id] = id;
            result.Increment("Post");
        }

        MigratePostMetrics(target, source, map, result);
        return map;
    }

    private static void AddLegacyAiArtifacts(SqliteContext target, LegacyRow row, Guid postId, DateTime now)
    {
        AddLegacyAiArtifact(target, row, postId, AiArtifactType.Summary, "zh-CN", row.GetString("ContentAbstractZh"), "ContentAbstractZh", now);
        AddLegacyAiArtifact(target, row, postId, AiArtifactType.Summary, "en-US", row.GetString("ContentAbstractEn"), "ContentAbstractEn", now);
        AddLegacyAiArtifact(target, row, postId, AiArtifactType.Translation, "zh-CN", row.GetString("LocalizedChineseContent"), "LocalizedChineseContent", now);
        AddLegacyAiArtifact(target, row, postId, AiArtifactType.Translation, "en-US", row.GetString("LocalizedEnglishContent"), "LocalizedEnglishContent", now);
    }

    private static void AddLegacyAiArtifact(
        SqliteContext target,
        LegacyRow row,
        Guid postId,
        AiArtifactType artifactType,
        string cultureCode,
        string? content,
        string legacyColumn,
        DateTime now)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        target.AiArtifact.Add(new AiArtifactEntity
        {
            SiteId = SystemIds.DefaultSiteId,
            TargetEntityType = nameof(PostEntity),
            TargetEntityId = postId,
            ArtifactType = artifactType,
            CultureCode = cultureCode,
            Content = content,
            MetadataJson = JsonSerializer.Serialize(new
            {
                source = "LegacySqliteMigrator",
                legacyColumn
            }),
            CreatedAtUtc = row.GetDateTime("LocalizeJobRunAt") ?? row.GetDateTime("LastModifiedUtc", "UpdateTimeUtc") ?? now
        });
    }

    private static void MigratePostMetrics(SqliteContext target, LegacySqliteDatabase source, Dictionary<Guid, Guid> postIds, LegacySqliteMigrationResult result)
    {
        foreach (var row in source.ReadRows("PostExtension"))
        {
            var oldPostId = row.GetGuid("PostId");
            if (!oldPostId.HasValue || !postIds.TryGetValue(oldPostId.Value, out var postId))
            {
                result.Skip("PostExtension", "Post is missing.");
                continue;
            }

            var metric = target.Post.Local.FirstOrDefault(p => p.Id == postId)?.PostExtension;
            if (metric is null)
            {
                continue;
            }

            metric.Hits = row.GetInt32("Hits") ?? 0;
            metric.Likes = row.GetInt32("Likes") ?? 0;
            result.Increment("PostExtension");
        }
    }

    private static void MigratePostCategories(
        SqliteContext target,
        LegacySqliteDatabase source,
        Dictionary<Guid, Guid> postIds,
        Dictionary<Guid, Guid> categoryIds,
        LegacySqliteMigrationResult result)
    {
        foreach (var row in source.ReadRows("PostCategory"))
        {
            var oldPostId = row.GetGuid("PostId");
            var oldCategoryId = row.GetGuid("CategoryId");
            if (!oldPostId.HasValue || !oldCategoryId.HasValue ||
                !postIds.TryGetValue(oldPostId.Value, out var postId) ||
                !categoryIds.TryGetValue(oldCategoryId.Value, out var categoryId))
            {
                result.Skip("PostCategory", "Post or category is missing.");
                continue;
            }

            target.PostCategory.Add(new PostCategoryEntity
            {
                SiteId = SystemIds.DefaultSiteId,
                PostId = postId,
                CategoryId = categoryId
            });
            result.Increment("PostCategory");
        }
    }

    private static void MigratePostTags(
        SqliteContext target,
        LegacySqliteDatabase source,
        Dictionary<Guid, Guid> postIds,
        Dictionary<int, int> tagIds,
        LegacySqliteMigrationResult result)
    {
        foreach (var row in source.ReadRows("PostTag"))
        {
            var oldPostId = row.GetGuid("PostId");
            var oldTagId = row.GetInt32("TagId");
            if (!oldPostId.HasValue || !oldTagId.HasValue ||
                !postIds.TryGetValue(oldPostId.Value, out var postId) ||
                !tagIds.TryGetValue(oldTagId.Value, out var tagId))
            {
                result.Skip("PostTag", "Post or tag is missing.");
                continue;
            }

            target.PostTag.Add(new PostTagEntity
            {
                SiteId = SystemIds.DefaultSiteId,
                PostId = postId,
                TagId = tagId
            });
            result.Increment("PostTag");
        }
    }

    private static void MigrateComments(SqliteContext target, LegacySqliteDatabase source, Dictionary<Guid, Guid> postIds, LegacySqliteMigrationResult result)
    {
        foreach (var row in source.ReadRows("Comment"))
        {
            var oldPostId = row.GetGuid("PostId");
            if (!oldPostId.HasValue || !postIds.TryGetValue(oldPostId.Value, out var postId))
            {
                result.Skip("Comment", "Post is missing.");
                continue;
            }

            target.Comment.Add(new CommentEntity
            {
                Id = row.GetGuid("Id") ?? Guid.NewGuid(),
                SiteId = SystemIds.DefaultSiteId,
                PostId = postId,
                Username = TrimToMax(row.GetString("Username", "UserName", "Name"), 64),
                Email = TrimToMax(row.GetString("Email"), 128),
                IPAddress = TrimToMax(row.GetString("IPAddress", "IP"), 64),
                CreateTimeUtc = row.GetDateTime("CreateTimeUtc", "CreatedTimeUtc") ?? DateTime.UtcNow,
                CommentContent = row.GetString("CommentContent", "Content") ?? string.Empty,
                IsApproved = row.GetBool(true, "IsApproved")
            });
            result.Increment("Comment");
        }
    }

    private static void MigrateCommentReplies(SqliteContext target, LegacySqliteDatabase source, HashSet<Guid> commentIds, LegacySqliteMigrationResult result)
    {
        foreach (var row in source.ReadRows("CommentReply"))
        {
            var commentId = row.GetGuid("CommentId");
            if (!commentId.HasValue || !commentIds.Contains(commentId.Value))
            {
                result.Skip("CommentReply", "Comment is missing.");
                continue;
            }

            target.CommentReply.Add(new CommentReplyEntity
            {
                Id = row.GetGuid("Id") ?? Guid.NewGuid(),
                SiteId = SystemIds.DefaultSiteId,
                CommentId = commentId,
                ReplyContent = row.GetString("ReplyContent", "Content") ?? string.Empty,
                CreateTimeUtc = row.GetDateTime("CreateTimeUtc", "CreatedTimeUtc") ?? DateTime.UtcNow,
                Source = CommentSource.Admin
            });
            result.Increment("CommentReply");
        }
    }

    private static JsonElement? ReadGeneralSettings(LegacySqliteDatabase source)
    {
        foreach (var row in source.ReadRows("BlogConfiguration"))
        {
            if (!string.Equals(row.GetString("CfgKey", "Key"), "GeneralSettings", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = row.GetString("CfgValue", "Value");
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            using var document = JsonDocument.Parse(value);
            return document.RootElement.Clone();
        }

        return null;
    }

    private static string? GetJsonString(JsonElement? element, string propertyName)
    {
        if (!element.HasValue || !element.Value.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() : null;
    }

    private static int ComputePostCheckSum(string slug, DateTime? pubDateUtc)
    {
        return Helper.ComputeCheckSum($"{slug}#{pubDateUtc.GetValueOrDefault():yyyyMMdd}");
    }

    private static Dictionary<string, int> CountTargetRows(SqliteContext target)
    {
        return new Dictionary<string, int>
        {
            ["Tenant"] = target.Tenant.Count(),
            ["Site"] = target.Site.Count(),
            ["User"] = target.LocalAccount.Count(),
            ["AiArtifact"] = target.AiArtifact.Count(),
            ["AiJob"] = target.AiJob.Count(),
            ["Category"] = target.Category.Count(),
            ["Tag"] = target.Tag.Count(),
            ["Post"] = target.Post.Count(),
            ["PostContent"] = target.PostContent.Count(),
            ["PostRoute"] = target.PostRoute.Count(),
            ["PostCategory"] = target.PostCategory.Count(),
            ["PostTag"] = target.PostTag.Count(),
            ["Comment"] = target.Comment.Count(),
            ["CommentReply"] = target.CommentReply.Count(),
            ["Page"] = target.CustomPage.Count(),
            ["Menu"] = target.Menu.Count(),
            ["SubMenu"] = target.SubMenu.Count(),
            ["FriendLink"] = target.FriendLink.Count(),
            ["SiteSetting"] = target.BlogConfiguration.Count(),
            ["Theme"] = target.BlogTheme.Count(),
            ["SiteBinaryAsset"] = target.BlogAsset.Count()
        };
    }

    private static string NormalizeRequired(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string? TrimToMax(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength];
    }
}

internal sealed class LegacySqliteMigrationResult(string sourcePath, string targetPath, DateTimeOffset generatedAtUtc)
{
    private readonly Dictionary<string, int> _migratedRows = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _skippedRows = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, object> _info = new(StringComparer.OrdinalIgnoreCase);

    public string SourcePath { get; } = sourcePath;
    public string TargetPath { get; } = targetPath;
    public DateTimeOffset GeneratedAtUtc { get; } = generatedAtUtc;
    public IReadOnlyDictionary<string, int> MigratedRows => _migratedRows;
    public IReadOnlyDictionary<string, int> SkippedRows => _skippedRows;
    public IReadOnlyDictionary<string, object> Info => _info;
    public List<LegacyIssue> Errors { get; } = [];

    public void Increment(string tableName)
    {
        _migratedRows[tableName] = _migratedRows.GetValueOrDefault(tableName) + 1;
    }

    public void Skip(string tableName, string reason)
    {
        _skippedRows[tableName] = _skippedRows.GetValueOrDefault(tableName) + 1;
        _info[$"LastSkipped{tableName}Reason"] = reason;
    }

    public void AddInfo(string name, object value)
    {
        _info[name] = value;
    }
}

internal static class LegacySqliteMigrationReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static void WriteText(LegacySqliteMigrationResult result, TextWriter writer)
    {
        writer.WriteLine("MoongladePure legacy SQLite migration report");
        writer.WriteLine($"Source: {result.SourcePath}");
        writer.WriteLine($"Target: {result.TargetPath}");
        writer.WriteLine($"Generated UTC: {result.GeneratedAtUtc:O}");
        writer.WriteLine();
        writer.WriteLine("Migrated rows:");

        foreach (var item in result.MigratedRows.OrderBy(static item => item.Key))
        {
            writer.WriteLine($"  {item.Key}: {item.Value}");
        }

        writer.WriteLine();
        writer.WriteLine("Skipped rows:");

        foreach (var item in result.SkippedRows.OrderBy(static item => item.Key))
        {
            writer.WriteLine($"  {item.Key}: {item.Value}");
        }

        if (result.Info.TryGetValue("TargetRows", out var targetRows) && targetRows is Dictionary<string, int> rows)
        {
            writer.WriteLine();
            writer.WriteLine("Target rows:");

            foreach (var item in rows.OrderBy(static item => item.Key))
            {
                writer.WriteLine($"  {item.Key}: {item.Value}");
            }
        }

        writer.WriteLine();
        writer.WriteLine($"Errors: {result.Errors.Count}");

        foreach (var error in result.Errors)
        {
            writer.WriteLine($"  [{error.Code}] {error.Message}");
        }
    }

    public static void WriteJson(LegacySqliteMigrationResult result, string jsonPath)
    {
        var directory = Path.GetDirectoryName(jsonPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(jsonPath, JsonSerializer.Serialize(result, JsonOptions));
    }
}
