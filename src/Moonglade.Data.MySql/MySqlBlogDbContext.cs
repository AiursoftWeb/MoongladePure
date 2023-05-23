using Microsoft.EntityFrameworkCore;
using MoongladePure.Data.MySql.Configurations;

namespace MoongladePure.Data.MySql;


public class MySqlBlogDbContext : BlogDbContext
{
    public MySqlBlogDbContext()
    {
    }

    public MySqlBlogDbContext(DbContextOptions options)
        : base(options)
    {
    }

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