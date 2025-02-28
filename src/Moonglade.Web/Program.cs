using Aiursoft.DbTools;
using Microsoft.EntityFrameworkCore;
using MoongladePure.Data.MySql;
using static Aiursoft.WebTools.Extends;

namespace MoongladePure.Web;

public class Program
{
    public static async Task Main(string[] args)
    {
        var app = await AppAsync<Startup>(args);
        await app.UpdateDbAsync<BlogDbContext>();
        await app.SeedAsync();
        await app.RunAsync();
    }
}

public static class ProgramExtends
{
    public static async Task<IHost> SeedAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var env = services.GetRequiredService<IWebHostEnvironment>();
        var logger = services.GetRequiredService<ILogger<Startup>>();
        var mediator = services.GetRequiredService<IMediator>();
        var context = services.GetRequiredService<BlogDbContext>();
        var bc = services.GetRequiredService<IBlogConfig>();

        var isNew = !await context.BlogConfiguration.AnyAsync();
        if (isNew)
        {
            await Seed.SeedAsync(context, logger);
        }

        // load configurations into singleton
        var config = await mediator.Send(new GetAllConfigurationsQuery());
        bc.LoadFromConfig(config);

        var iconData = await mediator.Send(new GetAssetQuery(AssetId.SiteIconBase64));
        MemoryStreamIconGenerator.GenerateIcons(iconData, env.WebRootPath, logger);
        return host;
    }
}
