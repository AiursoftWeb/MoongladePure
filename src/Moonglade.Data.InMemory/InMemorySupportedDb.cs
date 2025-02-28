using Aiursoft.DbTools;
using Aiursoft.DbTools.InMemory;
using Microsoft.Extensions.DependencyInjection;

namespace MoongladePure.Data.InMemory;

public class InMemorySupportedDb : SupportedDatabaseType<BlogDbContext>
{
    public override string DbType => "InMemory";

    public override IServiceCollection RegisterFunction(IServiceCollection services, string connectionString)
    {
        return services.AddAiurInMemoryDb<InMemoryContext>();
    }

    public override BlogDbContext ContextResolver(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<InMemoryContext>();
    }
}