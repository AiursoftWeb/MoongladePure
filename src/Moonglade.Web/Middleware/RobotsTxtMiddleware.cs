namespace MoongladePure.Web.Middleware;

public class RobotsTxtMiddleware
{
    private readonly RequestDelegate _next;

    public RobotsTxtMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext httpContext, IBlogConfig blogConfig)
    {
        // Double check path to prevent user from wrong usage like adding the middleware manually without MapWhen
        if (httpContext.Request.Path == "/robots.txt")
        {
            var robotsTxtContent = blogConfig.AdvancedSettings.RobotsTxtContent;
            
            robotsTxtContent += "\n\nsitemap: " + Helper.ResolveRootUrl(httpContext) + "/sitemap.xml";
            httpContext.Response.ContentType = "text/plain";
            await httpContext.Response.WriteAsync(robotsTxtContent, Encoding.UTF8, httpContext.RequestAborted);
        }
        else
        {
            await _next(httpContext);
        }
    }
}

public static partial class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseRobotsTxt(this IApplicationBuilder builder) =>
        builder.MapWhen(
            context => context.Request.Path == "/robots.txt",
            p => p.UseMiddleware<RobotsTxtMiddleware>());
}