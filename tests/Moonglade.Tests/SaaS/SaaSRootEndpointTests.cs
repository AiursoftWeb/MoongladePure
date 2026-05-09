using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MoongladePure.Data.Entities;
using MoongladePure.Data.InMemory;
using MoongladePure.SaaS.Hosting;
using MoongladePure.SaaS.Identity;
using MoongladePure.SaaS.Registration;

namespace MoongladePure.Tests.SaaS;

[TestClass]
public class SaaSRootEndpointTests
{
    private static readonly Guid SiteId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [TestMethod]
    public async Task HandleAsyncReturnsPortalForPortalHost()
    {
        await using var context = CreateContext();
        var endpoint = CreateEndpoint(context);
        var httpContext = CreateHttpContext("example.com");

        var result = await endpoint.HandleAsync(httpContext);
        await result.ExecuteAsync(httpContext);

        Assert.AreEqual(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        StringAssert.Contains(await ReadBodyAsync(httpContext), "<h1>MoongladePure</h1>");
    }

    [TestMethod]
    public async Task HandleAsyncReturnsSiteForVerifiedCustomDomain()
    {
        await using var context = CreateContext();
        await AddSiteAsync(context);
        await context.SiteDomain.AddAsync(CreateDomain("blog.customer.com", SiteDomainVerificationStatus.Verified));
        await context.SaveChangesAsync();
        var endpoint = CreateEndpoint(context);
        var httpContext = CreateHttpContext("blog.customer.com");

        var result = await endpoint.HandleAsync(httpContext);
        await result.ExecuteAsync(httpContext);

        Assert.AreEqual(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        StringAssert.Contains(await ReadBodyAsync(httpContext), SiteId.ToString());
    }

    [TestMethod]
    public async Task HandleAsyncReturnsSiteForRegisteredUserSubdomain()
    {
        await using var context = CreateContext();
        var provisioning = new SaaSSiteProvisioningService(context, new UsernamePolicy());
        var site = await provisioning.ProvisionAsync(new SaaSSiteProvisioningRequest("Alice", "app.example.com"));
        var endpoint = CreateEndpoint(context);
        var httpContext = CreateHttpContext("alice.app.example.com");

        var result = await endpoint.HandleAsync(httpContext);
        await result.ExecuteAsync(httpContext);

        Assert.AreEqual(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        StringAssert.Contains(await ReadBodyAsync(httpContext), site.SiteId.ToString());
    }

    [TestMethod]
    public async Task HandleAsyncReturnsNotFoundForMissingUserSubdomain()
    {
        await using var context = CreateContext();
        var endpoint = CreateEndpoint(context);
        var httpContext = CreateHttpContext("alice.app.example.com");

        var result = await endpoint.HandleAsync(httpContext);
        await result.ExecuteAsync(httpContext);

        Assert.AreEqual(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
        StringAssert.Contains(await ReadBodyAsync(httpContext), "not registered");
    }

    [TestMethod]
    public async Task HandleAsyncReturnsNotFoundForPendingCustomDomain()
    {
        await using var context = CreateContext();
        await AddSiteAsync(context);
        await context.SiteDomain.AddAsync(CreateDomain("blog.customer.com", SiteDomainVerificationStatus.PendingVerification));
        await context.SaveChangesAsync();
        var endpoint = CreateEndpoint(context);
        var httpContext = CreateHttpContext("blog.customer.com");

        var result = await endpoint.HandleAsync(httpContext);
        await result.ExecuteAsync(httpContext);

        Assert.AreEqual(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
        StringAssert.Contains(await ReadBodyAsync(httpContext), "not registered");
    }

    private static InMemoryContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InMemoryContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new InMemoryContext(options);
    }

    private static SaaSRootEndpoint CreateEndpoint(InMemoryContext context) =>
        new(
            Options.Create(new SaaSOptions
            {
                PortalHosts = ["example.com", "www.example.com"],
                SiteSubdomainRoot = "app.example.com"
            }),
            new SaaSHostClassifier(new UsernamePolicy()),
            new CustomDomainSiteResolver(context),
            new UserSubdomainSiteResolver(context));

    private static DefaultHttpContext CreateHttpContext(string host)
    {
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString(host);
        context.RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<string> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
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
