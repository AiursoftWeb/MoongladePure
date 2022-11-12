using Microsoft.Extensions.DependencyInjection;

namespace MoongladePure.Syndication;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSyndication(this IServiceCollection services)
    {
        services.AddScoped<ISyndicationDataSource, SyndicationDataSource>();

        return services;
    }
}