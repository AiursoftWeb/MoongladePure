using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MoongladePure.Data.InMemory;
using MoongladePure.SaaS.Hosting;
using MoongladePure.SaaS.Identity;
using MoongladePure.SaaS.Registration;

namespace MoongladePure.Tests.SaaS;

[TestClass]
public class SaaSRegistrationEndpointTests
{
    [TestMethod]
    public async Task RegisterAsyncCreatesSiteAndStoresPasswordHash()
    {
        await using var context = CreateContext();
        var endpoint = CreateEndpoint(context);

        var response = await endpoint.RegisterAsync(new SaaSRegistrationInput(
            " Alice ",
            "Password1",
            "alice@example.com",
            "Alice",
            "Alice Blog"));

        Assert.IsTrue(response.Succeeded);
        Assert.AreNotEqual(Guid.Empty, response.TenantId);
        Assert.AreNotEqual(Guid.Empty, response.UserId);
        Assert.AreNotEqual(Guid.Empty, response.SiteId);
        Assert.AreEqual("alice.app.example.com", response.Host);

        var user = await context.LocalAccount.SingleAsync();
        Assert.IsFalse(string.IsNullOrWhiteSpace(user.PasswordSalt));
        Assert.IsFalse(string.IsNullOrWhiteSpace(user.PasswordHash));
        Assert.AreNotEqual("Password1", user.PasswordHash);
    }

    [TestMethod]
    public async Task RegisterAsyncRejectsWeakPassword()
    {
        await using var context = CreateContext();
        var endpoint = CreateEndpoint(context);

        var response = await endpoint.RegisterAsync(new SaaSRegistrationInput("alice", "password"));

        Assert.IsFalse(response.Succeeded);
        Assert.AreEqual("Password must be 8-32 characters and include letters and numbers.", response.Error);
        Assert.AreEqual(0, await context.LocalAccount.CountAsync());
    }

    [TestMethod]
    public async Task RegisterAsyncRejectsDuplicateUsername()
    {
        await using var context = CreateContext();
        var endpoint = CreateEndpoint(context);
        await endpoint.RegisterAsync(new SaaSRegistrationInput("alice", "Password1"));

        var response = await endpoint.RegisterAsync(new SaaSRegistrationInput("ALICE", "Password1"));

        Assert.IsFalse(response.Succeeded);
        Assert.AreEqual("Username is already registered.", response.Error);
        Assert.AreEqual(1, await context.LocalAccount.CountAsync());
    }

    private static InMemoryContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InMemoryContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new InMemoryContext(options);
    }

    private static SaaSRegistrationEndpoint CreateEndpoint(InMemoryContext context) =>
        new(
            Options.Create(new SaaSOptions
            {
                PortalHosts = ["example.com", "www.example.com"],
                SiteSubdomainRoot = "app.example.com"
            }),
            new SaaSSiteProvisioningService(context, new UsernamePolicy()));
}
