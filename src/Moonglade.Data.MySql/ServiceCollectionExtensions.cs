using Microsoft.Extensions.DependencyInjection;
using MoongladePure.Data.Infrastructure;
using MoongladePure.Data.MySql.Infrastructure;
using Aiursoft.DbTools.InMemory;

namespace MoongladePure.Data.MySql;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, string connectionString, bool useTestDb)
    {
        services.AddScoped(typeof(IRepository<>), typeof(MySqlDbContextRepository<>));

        if (useTestDb)
        {
            services.AddAiurInMemoryDb<MySqlBlogDbContext>();
        }
        else
        {
            services.AddAiurMySqlWithCache<MySqlBlogDbContext>(connectionString);
        }

        return services;
    }
}