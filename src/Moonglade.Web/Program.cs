using AspNetCoreRateLimit;
using Edi.Captcha;
using Microsoft.AspNetCore.HttpOverrides;
using MoongladePure.Data.MySql;
using MoongladePure.Syndication;
using SixLabors.Fonts;
using System.Globalization;
using System.Text.Json.Serialization;
using WilderMinds.MetaWeblog;
using Encoder = MoongladePure.Web.Configuration.Encoder;

public class Program
{
    private static readonly List<CultureInfo> Cultures = new[] { "en-US", "zh-CN" }.Select(p => new CultureInfo(p)).ToList();
    
    public static async Task Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var builder = WebApplication.CreateBuilder(args);

        ConfigureServices(builder.Services, builder.Configuration);

        var app = builder.Build();
        ConfigureMiddleware(app);

        await FirstRun(app);
        await app.RunAsync();
    }

    public static void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        AppDomain.CurrentDomain.Load("MoongladePure.Core");
        AppDomain.CurrentDomain.Load("MoongladePure.FriendLink");
        AppDomain.CurrentDomain.Load("MoongladePure.Menus");
        AppDomain.CurrentDomain.Load("MoongladePure.Theme");
        AppDomain.CurrentDomain.Load("MoongladePure.Configuration");
        AppDomain.CurrentDomain.Load("MoongladePure.Data");

        services.AddMediatR(AppDomain.CurrentDomain.GetAssemblies());

        services.Configure<ForwardedHeadersOptions>(options => options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);

        services.AddOptions()
                .AddHttpContextAccessor()
                .AddRateLimit(config.GetSection("IpRateLimiting"));

        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(20);
            options.Cookie.HttpOnly = true;
        }).AddSessionBasedCaptcha(options => options.FontStyle = FontStyle.Bold);

        services.AddLocalization(options => options.ResourcesPath = "Resources");
        services.AddControllers(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()))
                .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
                .ConfigureApiBehaviorOptions(ConfigureApiBehavior.BlogApiBehavior);
        services.AddRazorPages()
                .AddDataAnnotationsLocalization(options => options.DataAnnotationLocalizerProvider = (_, factory) => factory.Create(typeof(SharedResource)))
                .AddRazorPagesOptions(options =>
                {
                    options.Conventions.AddPageRoute("/Admin/Post", "admin");
                    options.Conventions.AuthorizeFolder("/Admin");
                    options.Conventions.AuthorizeFolder("/Settings");
                });

        // Fix Chinese character being encoded in HTML output
        services.AddSingleton(Encoder.MoongladeHtmlEncoder);

        services.AddAntiforgery(options =>
        {
            const string csrfName = "CSRF-TOKEN-MOONGLADE";
            options.Cookie.Name = $"X-{csrfName}";
            options.FormFieldName = $"{csrfName}-FORM";
            options.HeaderName = "XSRF-TOKEN";
        }).Configure<RequestLocalizationOptions>(options =>
        {
            options.DefaultRequestCulture = new("en-US");
            options.SupportedCultures = Cultures;
            options.SupportedUICultures = Cultures;
        }).Configure<RouteOptions>(options =>
        {
            options.LowercaseUrls = true;
            options.LowercaseQueryStrings = true;
            options.AppendTrailingSlash = false;
        });

        services.AddHealthChecks();
        services.AddSyndication()
                .AddBlogCache()
                .AddMetaWeblog<MoongladePure.Web.MetaWeblogService>()
                .AddScoped<ValidateCaptcha>()
                .AddBlogConfig(config)
                .AddBlogAuthenticaton(config)
                .AddComments(config)
                .AddImageStorage(config)
                .Configure<List<ManifestIcon>>(config.GetSection("ManifestIcons"));

        var connStr = config.GetConnectionString("MoongladeDatabase");
        services.AddDatabase(connStr, useTestDb: false);
    }

    public static async Task FirstRun(WebApplication app)
    {
        try
        {
            var startUpResut = await app.InitStartUp();
            switch (startUpResut)
            {
                case StartupInitResult.DatabaseConnectionFail:
                    app.MapGet("/", () => Results.Problem(
                        detail: "Database connection test failed, please check your connection string and firewall settings, then RESTART MoongladePure manually.",
                        statusCode: 500
                        ));
                    app.Run();
                    return;
                case StartupInitResult.DatabaseSetupFail:
                    app.MapGet("/", () => Results.Problem(
                        detail: "Database setup failed, please check error log, then RESTART MoongladePure manually.",
                        statusCode: 500
                    ));
                    app.Run();
                    return;
            }
        }
        catch (Exception e)
        {
            app.MapGet("/", _ => throw new("Start up failed: " + e.Message));
            await app.RunAsync();
        }
    }

    public static void ConfigureMiddleware(WebApplication app)
    {
        app.UseForwardedHeaders();
        app.UseHealthChecks(new PathString("/health"));
        app.Logger.LogWarning($"Running in environment: {app.Environment.EnvironmentName}.");

        app.UseCustomCss(options => options.MaxContentLength = 10240);
        app.UseManifest(options => options.ThemeColor = "#333333");
        app.UseRobotsTxt();

        app.UseOpenSearch(options =>
        {
            options.RequestPath = "/opensearch";
            options.IconFileType = "image/png";
            options.IconFilePath = "/favicon-16x16.png";
        });

        app.UseMiddleware<FoafMiddleware>();

        var bc = app.Services.GetRequiredService<IBlogConfig>();
        if (bc.AdvancedSettings.EnableMetaWeblog)
        {
            app.UseMiddleware<RSDMiddleware>().UseMetaWeblog("/metaweblog");
        }

        app.UseMiddleware<SiteMapMiddleware>()
                  .UseMiddleware<PoweredByMiddleware>()
                  .UseMiddleware<DNTMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseStatusCodePages(ConfigureStatusCodePages.Handler).UseExceptionHandler("/error");
        }

        app.UseHttpsRedirection().UseHsts();
        app.UseRequestLocalization(new RequestLocalizationOptions
        {
            DefaultRequestCulture = new("en-US"),
            SupportedCultures = Cultures,
            SupportedUICultures = Cultures
        });

        app.UseStaticFiles();
        app.UseSession().UseCaptchaImage(options =>
        {
            options.RequestPath = "/captcha-image";
            options.ImageHeight = 36;
            options.ImageWidth = 100;
        });

        app.UseIpRateLimiting();
        app.UseRouting();
        app.UseAuthentication().UseAuthorization();

        app.UseEndpoints(ConfigureEndpoints.BlogEndpoints);
    }
}
