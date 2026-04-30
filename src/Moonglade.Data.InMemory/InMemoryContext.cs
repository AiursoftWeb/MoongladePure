using Microsoft.EntityFrameworkCore;

namespace MoongladePure.Data.InMemory;

public class InMemoryContext(DbContextOptions<InMemoryContext> options) : BlogDbContext(options)
{
    public override async Task MigrateAsync(CancellationToken cancellationToken)
    {
        await Database.EnsureDeletedAsync(cancellationToken);
        await Database.EnsureCreatedAsync(cancellationToken);
    }

    public override Task<bool> CanConnectAsync()
    {
        return Task.FromResult(true);
    }
}
