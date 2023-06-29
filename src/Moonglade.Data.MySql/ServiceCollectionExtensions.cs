using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MoongladePure.Data.Infrastructure;
using MoongladePure.Data.MySql.Infrastructure;
using Aiursoft.DbTools;

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
            // TODO: Aiursoft.DbTools support Aiursoft.DbTools.MySQL
            services.AddDbContext<MySqlBlogDbContext>(optionsAction => optionsAction
                .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), builder =>
                {
                    builder.EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null);
                })
                .EnableDetailedErrors());
        }

        return services;
    }
}