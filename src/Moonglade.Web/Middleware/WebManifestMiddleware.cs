using System.Text.Json.Serialization;

namespace MoongladePure.Web.Middleware;

public class WebManifestMiddleware
{
    private readonly RequestDelegate _next;

    public static WebManifestMiddlewareOptions Options { get; set; } = new();

    public WebManifestMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(
        HttpContext context, IBlogConfig blogConfig)
    {
        if (context.Request.Path == "/manifest.webmanifest")
        {
            var model = new ManifestModel
            {
                ShortName = blogConfig.GeneralSettings.SiteTitle,
                Name = blogConfig.GeneralSettings.SiteTitle,
                Description = blogConfig.GeneralSettings.ShortDescription,
                StartUrl = "/",
                Icons = new List<ManifestIcon>
                {
                    new()
                    {
                        Src = "/avatar",
                        Sizes = "300x300",
                    }
                },
                BackgroundColor = Options.ThemeColor,
                ThemeColor = Options.ThemeColor,
                Display = "standalone",
                Orientation = "portrait",
            };

            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "application/manifest+json";
            context.Response.Headers.TryAdd("cache-control", "public,max-age=3600");

            // Do not use `WriteAsJsonAsync` because it will override ContentType header
            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(model), context.RequestAborted);
        }
        else
        {
            await _next(context);
        }
    }
}

// Credits: https://github.com/Anduin2017/Blog
public class ManifestModel
{
    [JsonPropertyName("short_name")]
    public string ShortName { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("start_url")]
    public string StartUrl { get; set; }

    [JsonPropertyName("icons")]
    public IEnumerable<ManifestIcon> Icons { get; set; }

    [JsonPropertyName("background_color")]
    public string BackgroundColor { get; set; }

    [JsonPropertyName("theme_color")]
    public string ThemeColor { get; set; }

    [JsonPropertyName("display")]
    public string Display { get; set; }
    
    [JsonPropertyName("orientation")]
    public string Orientation { get; set; }
}

public class ManifestIcon
{
    [JsonPropertyName("src")]
    public string Src { get; set; }
    
    [JsonPropertyName("sizes")]
    public string Sizes { get; set; }
    
    [JsonPropertyName("type")]
    public string Type => "image/png";
}

public static partial class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseManifest(this IApplicationBuilder app, Action<WebManifestMiddlewareOptions> options)
    {
        options(WebManifestMiddleware.Options);
        return app.UseMiddleware<WebManifestMiddleware>();
    }
}

public class WebManifestMiddlewareOptions
{
    public string ThemeColor { get; set; }

    public WebManifestMiddlewareOptions() => ThemeColor = "#333333";
}