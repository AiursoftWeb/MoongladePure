using Aiursoft.DbTools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MoongladePure.Web;
using static Aiursoft.WebTools.Extends;
using AngleSharp.Html.Dom;
using Microsoft.Extensions.Hosting;
using MoongladePure.Data;

namespace MoongladePure.Tests;

[TestClass]
public class IntegrationTests
{
    private readonly string _endpointUrl;
    private readonly int _port;
    private HttpClient _http;
    private IHost _server;

    public IntegrationTests()
    {
        _port = Network.GetAvailablePort();
        _endpointUrl = $"http://localhost:{_port}";
    }

    [TestInitialize]
    public async Task CreateServer()
    {
        _server = await AppAsync<Startup>(args: Array.Empty<string>(), port: _port);
        await _server.UpdateDbAsync<BlogDbContext>();
        await _server.SeedAsync();
        await _server.StartAsync();
        _http = new HttpClient();
    }

    [TestCleanup]
    public async Task CleanServer()
    {
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
    public async Task GetTags()
    {
        var response = await _http.GetAsync($"{_endpointUrl}/tags");
        await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode(); // Status Code 200-299
    }

    [TestMethod]
    public async Task GetCatagory()
    {
        var response = await _http.GetAsync($"{_endpointUrl}/category/default");
        await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode(); // Status Code 200-299
    }

    [TestMethod]
    public async Task GetArchive()
    {
        var response = await _http.GetAsync($"{_endpointUrl}/archive");
        await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode(); // Status Code 200-299
    }

    [TestMethod]
    public async Task GetPost()
    {
        var response = await _http.GetAsync($"{_endpointUrl}/post/{DateTime.UtcNow.Year}/{DateTime.UtcNow.Month}/{DateTime.UtcNow.Day}/welcome-to-moonglade-pure");
        await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode(); // Status Code 200-299
    }

    [TestMethod]
    public async Task GetAdmin()
    {
        var response = await _http.GetAsync($"{_endpointUrl}/admin");
        await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode(); // Status Code 200-299
    }

    [TestMethod]
    public async Task GetRss()
    {
        var response = await _http.GetAsync($"{_endpointUrl}/rss");
        await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode(); // Status Code 200-299
    }

    [TestMethod]
    public async Task GetFoaF()
    {
        var response = await _http.GetAsync($"{_endpointUrl}/foaf.xml");
        await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode(); // Status Code 200-299
    }

    [TestMethod]
    public async Task GetOpenSearch()
    {
        var response = await _http.GetAsync($"{_endpointUrl}/opensearch");
        await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode(); // Status Code 200-299
    }

    [TestMethod]
    public async Task GetSitemap()
    {
        var response = await _http.GetAsync($"{_endpointUrl}/sitemap.xml");
        await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode(); // Status Code 200-299
    }

    [TestMethod]
    public async Task GetManifest()
    {
        var response = await _http.GetAsync($"{_endpointUrl}/manifest.webmanifest");
        await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode(); // Status Code 200-299
    }
}
