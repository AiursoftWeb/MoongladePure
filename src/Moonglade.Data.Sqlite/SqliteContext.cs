using Microsoft.EntityFrameworkCore;
using MoongladePure.Data.Entities;

namespace MoongladePure.Data.Sqlite;

public class SqliteContext(DbContextOptions<SqliteContext> options) : BlogDbContext(options)
{
    public override Task<bool> CanConnectAsync()
    {
        return Task.FromResult(true);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<PostCategoryEntity>()
            .HasKey(e => new { e.PostId, e.CategoryId });

        modelBuilder.Entity<PostExtensionEntity>()
            .HasKey(e => e.PostId);
    }
}
