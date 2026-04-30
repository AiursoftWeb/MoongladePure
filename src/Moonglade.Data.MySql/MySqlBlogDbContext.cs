using Microsoft.EntityFrameworkCore;

namespace MoongladePure.Data.MySql;

public class MySqlContext(DbContextOptions<MySqlContext> options) : BlogDbContext(options)
{
    public override Task MigrateAsync(CancellationToken cancellationToken)
    {
        return Database.MigrateAsync(cancellationToken);
    }
}
