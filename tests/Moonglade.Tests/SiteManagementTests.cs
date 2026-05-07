using Microsoft.EntityFrameworkCore;
using MoongladePure.Core.SiteFeature;
using MoongladePure.Data;
using MoongladePure.Data.Entities;
using MoongladePure.Data.InMemory;

namespace MoongladePure.Tests;

[TestClass]
public class SiteManagementTests
{
    private static readonly Guid OtherSiteId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    [TestMethod]
    public async Task ListSitesQueryReturnsDomains()
    {
        await using var context = CreateContext();
        await AddSiteAsync(context, SystemIds.DefaultSiteId, "Default Site", "default");
        await AddSiteAsync(context, OtherSiteId, "Other Site", "other");
        await context.SiteDomain.AddRangeAsync(
            CreateDomain(SystemIds.DefaultSiteId, "default.example.com"),
            CreateDomain(OtherSiteId, "other.example.com"));
        await context.SaveChangesAsync();
        var handler = new ListSitesQueryHandler(
            new BlogDbContextRepository<SiteEntity>(context),
            new BlogDbContextRepository<SiteDomainEntity>(context));

        var sites = await handler.Handle(new ListSitesQuery(), CancellationToken.None);

        Assert.AreEqual(2, sites.Count);
        var otherSite = sites.Single(site => site.Id == OtherSiteId);
        Assert.AreEqual("Other Site", otherSite.Name);
        Assert.AreEqual(1, otherSite.Domains.Count);
        Assert.AreEqual("other.example.com", otherSite.Domains[0].Host);
    }

    [TestMethod]
    public async Task AddSiteDomainCommandNormalizesHost()
    {
        await using var context = CreateContext();
        await AddSiteAsync(context, OtherSiteId, "Other Site", "other");
        await context.SaveChangesAsync();
        var handler = new AddSiteDomainCommandHandler(
            new BlogDbContextRepository<SiteEntity>(context),
            new BlogDbContextRepository<SiteDomainEntity>(context));

        var result = await handler.Handle(new AddSiteDomainCommand(OtherSiteId, " BLOG.Example.COM ", true), CancellationToken.None);

        Assert.AreEqual(OperationCode.Done, result);
        var domain = await context.SiteDomain.SingleAsync();
        Assert.AreEqual(OtherSiteId, domain.SiteId);
        Assert.AreEqual("blog.example.com", domain.Host);
        Assert.IsTrue(domain.IsPrimary);
    }

    [TestMethod]
    public async Task AddSiteDomainCommandRejectsDuplicateHost()
    {
        await using var context = CreateContext();
        await AddSiteAsync(context, SystemIds.DefaultSiteId, "Default Site", "default");
        await AddSiteAsync(context, OtherSiteId, "Other Site", "other");
        await context.SiteDomain.AddAsync(CreateDomain(SystemIds.DefaultSiteId, "blog.example.com"));
        await context.SaveChangesAsync();
        var handler = new AddSiteDomainCommandHandler(
            new BlogDbContextRepository<SiteEntity>(context),
            new BlogDbContextRepository<SiteDomainEntity>(context));

        var result = await handler.Handle(new AddSiteDomainCommand(OtherSiteId, "blog.example.com"), CancellationToken.None);

        Assert.AreEqual(OperationCode.Canceled, result);
        Assert.AreEqual(1, await context.SiteDomain.CountAsync());
    }

    [TestMethod]
    public async Task AddSiteDomainCommandRejectsMissingSite()
    {
        await using var context = CreateContext();
        var handler = new AddSiteDomainCommandHandler(
            new BlogDbContextRepository<SiteEntity>(context),
            new BlogDbContextRepository<SiteDomainEntity>(context));

        var result = await handler.Handle(new AddSiteDomainCommand(OtherSiteId, "blog.example.com"), CancellationToken.None);

        Assert.AreEqual(OperationCode.ObjectNotFound, result);
        Assert.AreEqual(0, await context.SiteDomain.CountAsync());
    }

    [TestMethod]
    public async Task DeleteSiteDomainCommandDeletesDomain()
    {
        await using var context = CreateContext();
        await AddSiteAsync(context, OtherSiteId, "Other Site", "other");
        var domain = CreateDomain(OtherSiteId, "blog.example.com");
        await context.SiteDomain.AddAsync(domain);
        await context.SaveChangesAsync();
        var handler = new DeleteSiteDomainCommandHandler(new BlogDbContextRepository<SiteDomainEntity>(context));

        var result = await handler.Handle(new DeleteSiteDomainCommand(domain.Id), CancellationToken.None);

        Assert.AreEqual(OperationCode.Done, result);
        Assert.AreEqual(0, await context.SiteDomain.CountAsync());
    }

    [TestMethod]
    public async Task DeleteSiteDomainCommandReturnsNotFoundForMissingDomain()
    {
        await using var context = CreateContext();
        var handler = new DeleteSiteDomainCommandHandler(new BlogDbContextRepository<SiteDomainEntity>(context));

        var result = await handler.Handle(new DeleteSiteDomainCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.AreEqual(OperationCode.ObjectNotFound, result);
    }

    private static InMemoryContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InMemoryContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new InMemoryContext(options);
    }

    private static Task AddSiteAsync(InMemoryContext context, Guid id, string name, string slug) =>
        context.Site.AddAsync(new SiteEntity
        {
            Id = id,
            TenantId = SystemIds.DefaultTenantId,
            Name = name,
            Slug = slug,
            Status = SiteStatus.Active,
            DefaultCulture = "en-US",
            TimeZoneId = "UTC"
        }).AsTask();

    private static SiteDomainEntity CreateDomain(Guid siteId, string host) => new()
    {
        Id = Guid.NewGuid(),
        SiteId = siteId,
        Host = host,
        IsPrimary = true,
        CreatedAtUtc = DateTime.UtcNow
    };
}
