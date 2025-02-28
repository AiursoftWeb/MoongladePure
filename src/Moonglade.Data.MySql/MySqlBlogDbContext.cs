using Aiursoft.DbTools;
using Aiursoft.DbTools.MySql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MoongladePure.Data.MySql.Configurations;

namespace MoongladePure.Data.MySql;

public class MySqlContext(DbContextOptions<MySqlContext> options) : BlogDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CommentConfiguration());
        modelBuilder.ApplyConfiguration(new CommentReplyConfiguration());
        modelBuilder.ApplyConfiguration(new PostConfiguration());
        modelBuilder.ApplyConfiguration(new PostCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new PostExtensionConfiguration());
        modelBuilder.ApplyConfiguration(new LocalAccountConfiguration());
        modelBuilder.ApplyConfiguration(new BlogThemeConfiguration());
        modelBuilder.ApplyConfiguration(new BlogAssetConfiguration());
        modelBuilder.ApplyConfiguration(new BlogConfigurationConfiguration());
        modelBuilder.ApplyConfiguration(new PageConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}

public class MySqlSupportedDb(bool allowCache, bool splitQuery) : SupportedDatabaseType<BlogDbContext>
{
    public override string DbType => "MySql";

    public override IServiceCollection RegisterFunction(IServiceCollection services, string connectionString)
    {
        return services.AddAiurMySqlWithCache<MySqlContext>(
            connectionString,
            splitQuery: splitQuery,
            allowCache: allowCache);
    }

    public override BlogDbContext ContextResolver(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<MySqlContext>();
    }
}
