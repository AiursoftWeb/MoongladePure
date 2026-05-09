using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MoongladePure.Data.Entities;
using MoongladePure.Data.InMemory;
using MoongladePure.SaaS.Identity;
using MoongladePure.SaaS.Registration;

namespace MoongladePure.Tests.SaaS;

[TestClass]
public class SaaSSiteProvisioningServiceTests
{
    [TestMethod]
    public async Task ProvisionAsyncCreatesSiteBaseline()
    {
        await using var context = CreateContext();
        var service = new SaaSSiteProvisioningService(context, new UsernamePolicy());

        var result = await service.ProvisionAsync(new SaaSSiteProvisioningRequest(
            " Alice ",
            "app.example.com",
            "alice@example.com",
            "Alice",
            "Alice Blog"));

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("alice.app.example.com", result.Host);
        Assert.AreEqual(1, await context.Tenant.CountAsync());
        Assert.AreEqual(1, await context.LocalAccount.CountAsync());
        Assert.AreEqual(1, await context.Site.CountAsync());
        Assert.AreEqual(1, await context.SiteMembership.CountAsync());
        Assert.AreEqual(1, await context.SiteDomain.CountAsync());
        Assert.AreEqual(7, await context.BlogConfiguration.CountAsync());
        Assert.AreEqual(1, await context.BlogTheme.CountAsync());
        Assert.AreEqual(1, await context.Menu.CountAsync());

        var user = await context.LocalAccount.SingleAsync();
        Assert.AreEqual(result.UserId, user.Id);
        Assert.AreEqual(result.TenantId, user.TenantId);
        Assert.AreEqual("alice", user.Username);
        Assert.AreEqual("alice", user.NormalizedUsername);
        Assert.AreEqual("alice@example.com", user.Email);

        var site = await context.Site.SingleAsync();
        Assert.AreEqual(result.SiteId, site.Id);
        Assert.AreEqual(result.TenantId, site.TenantId);
        Assert.AreEqual("alice", site.Slug);
        Assert.AreEqual(SiteStatus.Active, site.Status);

        var membership = await context.SiteMembership.SingleAsync();
        Assert.AreEqual(result.SiteId, membership.SiteId);
        Assert.AreEqual(result.UserId, membership.UserId);
        Assert.AreEqual(SiteRole.Owner, membership.Role);

        var domain = await context.SiteDomain.SingleAsync();
        Assert.AreEqual(result.SiteId, domain.SiteId);
        Assert.AreEqual("alice.app.example.com", domain.Host);
        Assert.IsTrue(domain.IsPrimary);
        Assert.AreEqual(SiteDomainVerificationStatus.Verified, domain.VerificationStatus);

        var theme = await context.BlogTheme.SingleAsync();
        Assert.AreEqual(result.SiteId, theme.SiteId);
        Assert.AreEqual("Word Blue", theme.ThemeName);

        var generalSettings = await context.BlogConfiguration.SingleAsync(setting =>
            setting.SiteId == result.SiteId &&
            setting.CfgKey == "GeneralSettings");
        using var document = JsonDocument.Parse(generalSettings.CfgValue);
        Assert.AreEqual(theme.Id, document.RootElement.GetProperty("ThemeId").GetInt32());
    }

    [TestMethod]
    public async Task ProvisionAsyncRejectsDuplicateUsername()
    {
        await using var context = CreateContext();
        var service = new SaaSSiteProvisioningService(context, new UsernamePolicy());
        await service.ProvisionAsync(new SaaSSiteProvisioningRequest("alice", "app.example.com"));

        var result = await service.ProvisionAsync(new SaaSSiteProvisioningRequest("ALICE", "app.example.com"));

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("Username is already registered.", result.Error);
    }

    [TestMethod]
    public async Task ProvisionAsyncRejectsInvalidUsername()
    {
        await using var context = CreateContext();
        var service = new SaaSSiteProvisioningService(context, new UsernamePolicy());

        var result = await service.ProvisionAsync(new SaaSSiteProvisioningRequest("admin", "app.example.com"));

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("Username is reserved.", result.Error);
    }

    private static InMemoryContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InMemoryContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new InMemoryContext(options);
    }
}
