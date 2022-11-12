using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MoongladePure.Comments.Moderators;

namespace MoongladePure.Comments;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddComments(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICommentModerator, LocalWordFilterModerator>();
        return services;
    }
}