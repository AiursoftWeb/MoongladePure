using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MoongladePure.Tests;

[TestClass]
public class StartUpTest
{
    [CanBeNull] private WebApplication app;
    private int _port;

    [TestInitialize]
    public async Task PrepareServer()
    {
        _port = Network.GetAvailablePort();

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var builder = WebApplication.CreateBuilder();

        builder.WebHost.UseUrls($"http://localhost:{_port}");

        Program.ConfigureServices(builder.Services, builder.Configuration, isTest: true);

        app = builder.Build();

        await Program.FirstRun(app);

        Program.ConfigureMiddleware(app);

        await app.StartAsync();
    }

    [TestCleanup]
    public async Task CleanServer()
    {
        if (app != null)
        {
            await app.StopAsync();
        }
    }

    [TestMethod]
    public async Task HealthCheck()
    {
        var http = new HttpClient();
        var response = await http.GetAsync($"http://localhost:{_port}/health");
        response.EnsureSuccessStatusCode(); // Status Code 200-299
    }
}
