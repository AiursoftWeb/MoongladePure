using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MoongladePure.Data;
using MoongladePure.Data.Entities;
using MoongladePure.SaaS.Hosting;
using MoongladePure.SaaS.Identity;

namespace MoongladePure.SaaS.Registration;

public sealed class SaaSSiteProvisioningService(BlogDbContext dbContext, UsernamePolicy usernamePolicy)
{
    public async Task<SaaSSiteProvisioningResult> ProvisionAsync(
        SaaSSiteProvisioningRequest request,
        CancellationToken ct = default)
    {
        var usernameResult = usernamePolicy.Validate(request.Username);
        if (!usernameResult.Succeeded)
        {
            return SaaSSiteProvisioningResult.Fail(usernameResult.Error);
        }

        var subdomainRoot = SaaSHostClassifier.NormalizeHost(request.SiteSubdomainRoot);
        if (string.IsNullOrWhiteSpace(subdomainRoot))
        {
            return SaaSSiteProvisioningResult.Fail("Site subdomain root is required.");
        }

        var username = usernameResult.NormalizedUsername;
        var host = $"{username}.{subdomainRoot}";

        if (await dbContext.LocalAccount.AnyAsync(user => user.NormalizedUsername == username, ct))
        {
            return SaaSSiteProvisioningResult.Fail("Username is already registered.");
        }

        if (await dbContext.SiteDomain.AnyAsync(domain => domain.Host == host, ct))
        {
            return SaaSSiteProvisioningResult.Fail("Site subdomain is already registered.");
        }

        var now = DateTime.UtcNow;
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var siteId = Guid.NewGuid();
        var displayName = NormalizeDisplayName(request.DisplayName, username);
        var siteName = NormalizeSiteName(request.SiteName, displayName);
        var email = NormalizeOptional(request.Email);

        dbContext.Tenant.Add(new TenantEntity
        {
            Id = tenantId,
            Name = displayName,
            Status = TenantStatus.Active,
            CreatedAtUtc = now
        });

        dbContext.LocalAccount.Add(new LocalAccountEntity
        {
            Id = userId,
            TenantId = tenantId,
            Username = username,
            NormalizedUsername = username,
            Email = TrimToMax(email, 128),
            NormalizedEmail = TrimToMax(email, 128),
            PasswordSalt = NormalizeOptional(request.PasswordSalt),
            PasswordHash = NormalizeOptional(request.PasswordHash),
            CreateTimeUtc = now
        });

        dbContext.Site.Add(new SiteEntity
        {
            Id = siteId,
            TenantId = tenantId,
            Name = siteName,
            Slug = username,
            Status = SiteStatus.Active,
            DefaultCulture = "en-US",
            TimeZoneId = "UTC",
            CreatedAtUtc = now
        });

        dbContext.SiteMembership.Add(new SiteMembershipEntity
        {
            Id = Guid.NewGuid(),
            SiteId = siteId,
            UserId = userId,
            Role = SiteRole.Owner,
            DisplayName = displayName,
            CreatedAtUtc = now
        });

        dbContext.SiteDomain.Add(new SiteDomainEntity
        {
            Id = Guid.NewGuid(),
            SiteId = siteId,
            Host = host,
            IsPrimary = true,
            VerificationStatus = SiteDomainVerificationStatus.Verified,
            LastVerifiedAtUtc = now,
            VerifiedAtUtc = now,
            CreatedAtUtc = now
        });

        var theme = new BlogThemeEntity
        {
            SiteId = siteId,
            ThemeName = "Word Blue",
            CssRules = "{\"--accent-color1\":\"#2a579a\",\"--accent-color2\":\"#1a365f\",\"--accent-color3\":\"#3e6db5\"}",
            ThemeType = ThemeType.User
        };

        dbContext.BlogTheme.Add(theme);
        await dbContext.SaveChangesAsync(ct);

        dbContext.BlogConfiguration.AddRange(CreateDefaultSettings(siteId, siteName, displayName, email, theme.Id, now));
        dbContext.Menu.Add(new MenuEntity
        {
            Id = Guid.NewGuid(),
            SiteId = siteId,
            DisplayOrder = 0,
            IsOpenInNewTab = false,
            Icon = "icon-home",
            Title = "Home",
            Url = "/"
        });

        await dbContext.SaveChangesAsync(ct);

        return SaaSSiteProvisioningResult.Success(tenantId, userId, siteId, host);
    }

    private static IReadOnlyList<BlogConfigurationEntity> CreateDefaultSettings(
        Guid siteId,
        string siteName,
        string ownerName,
        string ownerEmail,
        int themeId,
        DateTime now) =>
        [
            CreateSetting(siteId, "ContentSettings", new
            {
                EnableComments = true,
                RequireCommentReview = false,
                EnableWordFilter = false,
                PostListPageSize = 10,
                HotTagAmount = 10,
                DisharmonyWords = string.Empty,
                ShowCalloutSection = false,
                CalloutSectionHtmlPitch = string.Empty
            }, now),
            CreateSetting(siteId, "NotificationSettings", new
            {
                EnableEmailSending = false,
                EnableSsl = true,
                SendEmailOnCommentReply = true,
                SendEmailOnNewComment = true,
                SmtpServerPort = 587,
                AdminEmail = string.Empty,
                EmailDisplayName = "MoongladePure",
                SmtpPassword = string.Empty,
                SmtpServer = string.Empty,
                SmtpUserName = string.Empty,
                BannedMailDomain = string.Empty
            }, now),
            CreateSetting(siteId, "FeedSettings", new
            {
                RssItemCount = 20,
                RssCopyright = "(c) {year} MoongladePure",
                RssDescription = $"Latest posts from {siteName}",
                RssTitle = siteName,
                AuthorName = ownerName,
                UseFullContent = false
            }, now),
            CreateSetting(siteId, "GeneralSettings", new
            {
                OwnerName = TrimToMax(ownerName, 32),
                OwnerEmail = TrimToMax(ownerEmail, 64) ?? string.Empty,
                Description = TrimToMax(siteName, 256),
                ShortDescription = TrimToMax(siteName, 32),
                AvatarBase64 = string.Empty,
                SiteTitle = TrimToMax(siteName, 16),
                LogoText = TrimToMax(siteName, 16),
                MetaKeyword = "moongladepure",
                MetaDescription = "Just another .NET blog system",
                Copyright = "[c] 2023 - [year] MoongladePure",
                SideBarCustomizedHtmlPitch = string.Empty,
                FooterCustomizedHtmlPitch = string.Empty,
                UserTimeZoneBaseUtcOffset = "00:00:00",
                TimeZoneId = "UTC",
                AutoDarkLightTheme = true,
                ThemeId = themeId
            }, now),
            CreateSetting(siteId, "ImageSettings", new
            {
                IsWatermarkEnabled = true,
                KeepOriginImage = false,
                WatermarkFontSize = 20,
                WatermarkText = "MoongladePure",
                UseFriendlyNotFoundImage = true
            }, now),
            CreateSetting(siteId, "AdvancedSettings", new
            {
                DNSPrefetchEndpoint = string.Empty,
                EnableOpenSearch = true,
                WarnExternalLink = false,
                AllowScriptsInPage = false,
                ShowAdminLoginButton = true,
                EnablePostRawEndpoint = true
            }, now),
            CreateSetting(siteId, "CustomStyleSheetSettings", new
            {
                EnableCustomCss = false,
                CssCode = string.Empty
            }, now)
        ];

    private static BlogConfigurationEntity CreateSetting(Guid siteId, string key, object value, DateTime now) => new()
    {
        SiteId = siteId,
        CfgKey = key,
        CfgValue = JsonSerializer.Serialize(value),
        LastModifiedTimeUtc = now
    };

    private static string NormalizeDisplayName(string displayName, string username) =>
        TrimToMax(string.IsNullOrWhiteSpace(displayName) ? username : displayName.Trim(), 64);

    private static string NormalizeSiteName(string siteName, string displayName) =>
        TrimToMax(string.IsNullOrWhiteSpace(siteName) ? $"{displayName} Blog" : siteName.Trim(), 128);

    private static string NormalizeOptional(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string TrimToMax(string value, int maxLength) =>
        value is null || value.Length <= maxLength ? value : value[..maxLength];
}
