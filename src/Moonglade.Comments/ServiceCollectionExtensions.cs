using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Comments.Moderators;

namespace Moonglade.Comments;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddComments(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICommentModerator, LocalWordFilterModerator>();
        return services;
    }
}