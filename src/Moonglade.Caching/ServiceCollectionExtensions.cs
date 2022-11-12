using Microsoft.Extensions.DependencyInjection;

namespace MoongladePure.Caching;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBlogCache(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<IBlogCache, BlogMemoryCache>();
        return services;
    }
}