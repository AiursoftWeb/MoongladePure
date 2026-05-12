using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MoongladePure.Data.Entities;
using MoongladePure.Data.InMemory;
using MoongladePure.SaaS.Domains;

namespace MoongladePure.Tests.SaaS;

[TestClass]
public class SaaSCustomDomainEndpointTests
{
    private static readonly Guid SiteId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    [TestMethod]
    public async Task AddAsyncReturnsCreatedForPendingDomain()
    {
        await using var context = CreateContext();
        await AddSiteAsync(context);
        var endpoint = CreateEndpoint(context);
        var httpContext = CreateHttpContext();

        var result = await endpoint.AddAsync(SiteId, new SaaSCustomDomainRequest("blog.customer.com"));
        await result.ExecuteAsync(httpContext);

        Assert.AreEqual(StatusCodes.Status201Created, httpContext.Response.StatusCode);
        var domain = await context.SiteDomain.SingleAsync();
        Assert.AreEqual($"/api/sites/{SiteId}/domains/{domain.Id}", httpContext.Response.Headers.Location.ToString());
        StringAssert.Contains(await ReadBodyAsync(httpContext), "blog.customer.com");
    }

    [TestMethod]
    public async Task AddAsyncReturnsBadRequestForDuplicateHost()
    {
        await using var context = CreateContext();
        await AddSiteAsync(context);
        await context.SiteDomain.AddAsync(CreateDomain("blog.customer.com"));
        await context.SaveChangesAsync();
        var endpoint = CreateEndpoint(context);
        var httpContext = CreateHttpContext();

        var result = await endpoint.AddAsync(SiteId, new SaaSCustomDomainRequest("BLOG.Customer.COM"));
        await result.ExecuteAsync(httpContext);

        Assert.AreEqual(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
        StringAssert.Contains(await ReadBodyAsync(httpContext), "Host is already registered.");
    }

    [TestMethod]
    public async Task DeleteAsyncReturnsBadRequestForPrimaryDomain()
    {
        await using var context = CreateContext();
        await AddSiteAsync(context);
        var domain = CreateDomain("alice.app.example.com", true);
        await context.SiteDomain.AddAsync(domain);
        await context.SaveChangesAsync();
        var endpoint = CreateEndpoint(context);
        var httpContext = CreateHttpContext();

        var result = await endpoint.DeleteAsync(SiteId, domain.Id);
        await result.ExecuteAsync(httpContext);

        Assert.AreEqual(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
        StringAssert.Contains(await ReadBodyAsync(httpContext), "Primary site domain cannot be deleted.");
    }

    private static SaaSCustomDomainEndpoint CreateEndpoint(InMemoryContext context) =>
        new(new SaaSCustomDomainService(context));

    private static InMemoryContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InMemoryContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new InMemoryContext(options);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider()
        };
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<string> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
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

    private static SiteDomainEntity CreateDomain(string host, bool isPrimary = false) => new()
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
