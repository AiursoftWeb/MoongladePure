using Microsoft.EntityFrameworkCore;

namespace MoongladePure.Data.MySql;

public class MySqlContext(DbContextOptions<MySqlContext> options) : BlogDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }

    public override Task MigrateAsync(CancellationToken cancellationToken)
    {
        return Database.MigrateAsync(cancellationToken);
    }
}
