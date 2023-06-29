using System;
using System.Net.Http;
using System.Threading.Tasks;
using Aiursoft.DbTools;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MoongladePure.Web;
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
        _server = App<Startup>(port: _port);
        await _server.UpdateDbAsync<MySqlBlogDbContext>(UpdateMode.RecreateThenUse);
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
    public async Task HealthCheck()
    {
        var http = new HttpClient();
        var response = await http.GetAsync($"{_endpointUrl}/health");
        response.EnsureSuccessStatusCode(); // Status Code 200-299
    }

    [TestMethod]
    public async Task Ping()
    {
        var http = new HttpClient();
        var response = await http.GetAsync($"{_endpointUrl}/ping");
        response.EnsureSuccessStatusCode(); // Status Code 200-299
    }
}
