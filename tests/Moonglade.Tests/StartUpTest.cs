using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MoongladePure.Tests;

[TestClass]
public class StartUpTest
{
    [TestMethod]
    public async Task StartTest()
    {
        var builder = WebApplication.CreateBuilder();

        Program.ConfigureServices(builder.Services, builder.Configuration, isTest: true);

        var app = builder.Build();

        await Program.FirstRun(app);

        Program.ConfigureMiddleware(app);

        await app.StartAsync();
    }
}
