using Aiursoft.DbTools;
using Aiursoft.DbTools.MySql;
using Microsoft.Extensions.DependencyInjection;

namespace MoongladePure.Data.MySql;

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
