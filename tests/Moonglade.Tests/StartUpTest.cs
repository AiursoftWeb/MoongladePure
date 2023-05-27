using System.Net.Http;
using System.Threading.Tasks;
using Aiursoft.Handler.Attributes;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MoongladePure.Web;
using Aiursoft.SDK;
using MoongladePure.Data.MySql;
using static Aiursoft.WebTools.Extends;
using AngleSharp.Html.Dom;

namespace MoongladePure.Tests;

[TestClass]
public class StartUpTest
{
    private readonly string _endpointUrl;
    private readonly int _port;
    private HttpClient _http;
    private IHost _server;

    public StartUpTest()
    {
        _port = Network.GetAvailablePort();
        _endpointUrl = $"http://localhost:{_port}";
    }

    [TestInitialize]
    public async Task CreateServer()
    {
        _server = await App<Startup>(port: _port).Update<MySqlBlogDbContext>().SeedAsync();
        _http = new HttpClient();
        await _server.StartAsync();
    }

    [TestCleanup]
    public async Task CleanServer()
    {
        LimitPerMin.ClearMemory();
        if (_server != null)
        {
            await _server.StopAsync();
            _server.Dispose();
        }
    }

    [TestMethod]
    public async Task GetHome()
    {
        var response = await _http.GetAsync(_endpointUrl);
        await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode(); // Status Code 200-299
        Assert.AreEqual("text/html; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        var doc = await HtmlHelpers.GetDocumentAsync(response);
        var p = (IHtmlElement)doc.QuerySelector(".post-summary-title a");
        if (p != null)
            Assert.AreEqual(
                "Welcome to MoongladePure",
                p.InnerHtml.Trim());
    }

    [TestMethod]
    public async Task HealthCheck()
    {
        var http = new HttpClient();
        var response = await http.GetAsync($"{_endpointUrl}/health");
        response.EnsureSuccessStatusCode(); // Status Code 200-299
    }
}
