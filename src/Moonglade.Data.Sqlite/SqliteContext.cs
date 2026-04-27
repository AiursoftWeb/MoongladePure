using Microsoft.EntityFrameworkCore;

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
    }
}
