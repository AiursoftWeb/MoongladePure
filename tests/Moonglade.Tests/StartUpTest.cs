using Microsoft.AspNetCore.Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MoongladePure.Tests;

[TestClass]
public class StartUpTest
{
    [TestMethod]
    public void StartTest()
    {
        var builder = WebApplication.CreateBuilder();

        Program.ConfigureServices(builder.Services, builder.Configuration);

        _ = builder.Build();
    }
}
