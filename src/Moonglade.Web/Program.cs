using Aiursoft.SDK;
using Microsoft.EntityFrameworkCore;
using MoongladePure.Core.AiFeature;
using MoongladePure.Data.Entities;
using MoongladePure.Data.MySql;
using static Aiursoft.WebTools.Extends;

namespace MoongladePure.Web;

public class Program
{
    public static async Task Main(string[] args)
    {
        await (await (await App<Startup>(args)
            .Update<MySqlBlogDbContext>()
            .SeedAsync())
            .GenerateAiCommentAsync())
            .RunAsync();
    }

    // For EF
    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return BareApp<Startup>(args);
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
        var context = services.GetRequiredService<MySqlBlogDbContext>();
        var bc = services.GetRequiredService<IBlogConfig>();

        bool isNew = !await context.BlogConfiguration.AnyAsync();
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

    public static async Task<IHost> GenerateAiCommentAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var openAi = services.GetRequiredService<OpenAiService>();
        var logger = services.GetRequiredService<ILogger<Startup>>();
        var context = services.GetRequiredService<MySqlBlogDbContext>();
        var posts = await context.Post
            .Include(p => p.Comments)
            .OrderByDescending(p => p.PubDateUtc)
            .ToListAsync();

        foreach (var post in posts)
        {
            if (post.Comments.All(c => c.Username != "ChatGPT"))
            {
                try
                {
                    var newComment = await openAi.GenerateComment(post.PostContent);
                    await context.Comment.AddAsync(new CommentEntity
                    {

                        PostId = post.Id,
                        IPAddress = "127.0.0.1",
                        Email = "chatgpt@domain.com",
                        IsApproved = true,
                        CommentContent = newComment,
                        CreateTimeUtc = DateTime.UtcNow,
                        Username = "ChatGPT"
                    });
                    await context.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    logger.LogCritical(e, "Failed to generate OpenAi comment!");
                }
            }
        }

        return host;
    }
}