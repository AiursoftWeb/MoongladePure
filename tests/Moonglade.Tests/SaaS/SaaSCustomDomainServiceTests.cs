using Microsoft.EntityFrameworkCore;
using MoongladePure.Data.Entities;
using MoongladePure.Data.InMemory;
using MoongladePure.SaaS.Domains;

namespace MoongladePure.Tests.SaaS;

[TestClass]
public class SaaSCustomDomainServiceTests
{
    private static readonly Guid SiteId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [TestMethod]
    public async Task AddPendingAsyncCreatesPendingDomainWithTxtInstructions()
    {
        await using var context = CreateContext();
        await AddSiteAsync(context);
        var service = new SaaSCustomDomainService(context);

        var result = await service.AddPendingAsync(SiteId, new SaaSCustomDomainRequest(" Blog.Customer.COM. "));

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("blog.customer.com", result.Domain.Host);
        Assert.IsFalse(result.Domain.IsPrimary);
        Assert.AreEqual(SiteDomainVerificationStatus.PendingVerification, result.Domain.VerificationStatus);
        Assert.AreEqual(64, result.Domain.VerificationToken.Length);
        Assert.AreEqual("_moonglade.blog.customer.com", result.Domain.TxtRecordName);
        Assert.AreEqual($"moonglade-site-verification={result.Domain.VerificationToken}", result.Domain.TxtRecordValue);
        Assert.AreEqual(1, await context.SiteDomain.CountAsync());
    }

    [TestMethod]
    public async Task AddPendingAsyncRejectsDuplicateHost()
    {
        await using var context = CreateContext();
        await AddSiteAsync(context);
        await context.SiteDomain.AddAsync(CreateDomain("blog.customer.com", false));
        await context.SaveChangesAsync();
        var service = new SaaSCustomDomainService(context);

        var result = await service.AddPendingAsync(SiteId, new SaaSCustomDomainRequest("BLOG.Customer.COM"));

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("Host is already registered.", result.Error);
        Assert.AreEqual(1, await context.SiteDomain.CountAsync());
    }

    [TestMethod]
    public async Task AddPendingAsyncRejectsMissingSite()
    {
        await using var context = CreateContext();
        var service = new SaaSCustomDomainService(context);

        var result = await service.AddPendingAsync(SiteId, new SaaSCustomDomainRequest("blog.customer.com"));

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("Site does not exist.", result.Error);
        Assert.AreEqual(0, await context.SiteDomain.CountAsync());
    }

    [TestMethod]
    public async Task ListAsyncReturnsPrimaryDomainFirst()
    {
        await using var context = CreateContext();
        await AddSiteAsync(context);
        await context.SiteDomain.AddRangeAsync(
            CreateDomain("custom.example.com", false),
            CreateDomain("alice.app.example.com", true));
        await context.SaveChangesAsync();
        var service = new SaaSCustomDomainService(context);

        var domains = await service.ListAsync(SiteId);

        Assert.AreEqual(2, domains.Count);
        Assert.AreEqual("alice.app.example.com", domains[0].Host);
        Assert.IsTrue(domains[0].IsPrimary);
        Assert.AreEqual("custom.example.com", domains[1].Host);
    }

    [TestMethod]
    public async Task DeleteAsyncDeletesNonPrimaryDomain()
    {
        await using var context = CreateContext();
        await AddSiteAsync(context);
        var domain = CreateDomain("blog.customer.com", false);
        await context.SiteDomain.AddAsync(domain);
        await context.SaveChangesAsync();
        var service = new SaaSCustomDomainService(context);

        var result = await service.DeleteAsync(SiteId, domain.Id);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("blog.customer.com", result.Domain.Host);
        Assert.AreEqual(0, await context.SiteDomain.CountAsync());
    }

    [TestMethod]
    public async Task DeleteAsyncRejectsPrimaryDomain()
    {
        await using var context = CreateContext();
        await AddSiteAsync(context);
        var domain = CreateDomain("alice.app.example.com", true);
        await context.SiteDomain.AddAsync(domain);
        await context.SaveChangesAsync();
        var service = new SaaSCustomDomainService(context);

        var result = await service.DeleteAsync(SiteId, domain.Id);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("Primary site domain cannot be deleted.", result.Error);
        Assert.AreEqual(1, await context.SiteDomain.CountAsync());
    }

    private static InMemoryContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InMemoryContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new InMemoryContext(options);
    }

    private static async Task AddSiteAsync(InMemoryContext context)
    {
        await context.Site.AddAsync(new SiteEntity
        {
            Id = SiteId,
            TenantId = SystemIds.DefaultTenantId,
            Name = "Customer Site",
            Slug = "customer",
            Status = SiteStatus.Active,
            DefaultCulture = "en-US",
            TimeZoneId = "UTC"
        });
        await context.SaveChangesAsync();
    }

    private static SiteDomainEntity CreateDomain(string host, bool isPrimary) => new()
    {
        Id = Guid.NewGuid(),
        SiteId = SiteId,
        Host = host,
        IsPrimary = isPrimary,
        VerificationStatus = isPrimary
            ? SiteDomainVerificationStatus.Verified
            : SiteDomainVerificationStatus.PendingVerification,
        VerificationToken = isPrimary ? null : CustomDomainVerification.CreateToken(),
        CreatedAtUtc = DateTime.UtcNow
    };
}
