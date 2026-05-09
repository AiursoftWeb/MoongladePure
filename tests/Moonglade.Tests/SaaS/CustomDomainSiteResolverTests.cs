using Microsoft.EntityFrameworkCore;
using MoongladePure.Data.Entities;
using MoongladePure.Data.InMemory;
using MoongladePure.SaaS.Hosting;

namespace MoongladePure.Tests.SaaS;

[TestClass]
public class CustomDomainSiteResolverTests
{
    private static readonly Guid SiteId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [TestMethod]
    public async Task ResolveAsyncReturnsSiteForVerifiedCustomDomain()
    {
        await using var context = CreateContext();
        await AddSiteAsync(context);
        await context.SiteDomain.AddAsync(CreateDomain("Blog.Customer.COM", SiteDomainVerificationStatus.Verified));
        await context.SaveChangesAsync();
        var resolver = new CustomDomainSiteResolver(context);

        var result = await resolver.ResolveAsync(" blog.customer.com:443 ");

        Assert.IsNotNull(result);
        Assert.AreEqual(SiteId, result.SiteId);
        Assert.AreEqual("blog.customer.com", result.Host);
    }

    [TestMethod]
    public async Task ResolveAsyncReturnsNullForPendingCustomDomain()
    {
        await using var context = CreateContext();
        await AddSiteAsync(context);
        await context.SiteDomain.AddAsync(CreateDomain("blog.customer.com", SiteDomainVerificationStatus.PendingVerification));
        await context.SaveChangesAsync();
        var resolver = new CustomDomainSiteResolver(context);

        var result = await resolver.ResolveAsync("blog.customer.com");

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task ResolveAsyncReturnsNullForRejectedCustomDomain()
    {
        await using var context = CreateContext();
        await AddSiteAsync(context);
        await context.SiteDomain.AddAsync(CreateDomain("blog.customer.com", SiteDomainVerificationStatus.Rejected));
        await context.SaveChangesAsync();
        var resolver = new CustomDomainSiteResolver(context);

        var result = await resolver.ResolveAsync("blog.customer.com");

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task ResolveAsyncReturnsNullForMissingCustomDomain()
    {
        await using var context = CreateContext();
        await AddSiteAsync(context);
        var resolver = new CustomDomainSiteResolver(context);

        var result = await resolver.ResolveAsync("missing.customer.com");

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task ResolveAsyncReturnsNullForBlankHost()
    {
        await using var context = CreateContext();
        var resolver = new CustomDomainSiteResolver(context);

        var result = await resolver.ResolveAsync(" ");

        Assert.IsNull(result);
    }

    private static InMemoryContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InMemoryContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new InMemoryContext(options);
    }

    private static Task AddSiteAsync(InMemoryContext context) =>
        context.Site.AddAsync(new SiteEntity
        {
            Id = SiteId,
            TenantId = SystemIds.DefaultTenantId,
            Name = "Customer Site",
            Slug = "customer",
            Status = SiteStatus.Active,
            DefaultCulture = "en-US",
            TimeZoneId = "UTC"
        }).AsTask();

    private static SiteDomainEntity CreateDomain(string host, SiteDomainVerificationStatus status) => new()
    {
        Id = Guid.NewGuid(),
        SiteId = SiteId,
        Host = SaaSHostClassifier.NormalizeHost(host),
        VerificationStatus = status,
        CreatedAtUtc = DateTime.UtcNow
    };
}
