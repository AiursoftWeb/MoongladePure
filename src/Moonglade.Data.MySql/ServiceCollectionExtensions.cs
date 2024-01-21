using Microsoft.Extensions.DependencyInjection;
using MoongladePure.Data.Infrastructure;
using MoongladePure.Data.MySql.Infrastructure;
using Aiursoft.DbTools.InMemory;
using Aiursoft.DbTools.MySql;
using Aiursoft.DbTools.Sqlite;

namespace MoongladePure.Data.MySql;

public enum DbType
{
    MySql,
    Sqlite,
    InMemory
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, 
        string connectionString, 
        DbType dbType,
        bool allowCache)
    {
        services.AddScoped(typeof(IRepository<>), typeof(MySqlDbContextRepository<>));

        switch (dbType)
        {
            case DbType.InMemory:
                services.AddAiurInMemoryDb<MySqlBlogDbContext>();
                break;
            case DbType.Sqlite:
                services.AddAiurSqliteWithCache<MySqlBlogDbContext>(connectionString, allowCache);
                break;
            case DbType.MySql:
                // Don't allow cache to avoid multi-instance conflict
                services.AddAiurMySqlWithCache<MySqlBlogDbContext>(connectionString, allowCache);
                break;
            default:
                throw new NotSupportedException($"Database type {dbType} is not supported!");
        }

        return services;
    }
}