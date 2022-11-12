using AspNetCoreRateLimit;
using Edi.Captcha;
using Microsoft.AspNetCore.HttpOverrides;
using MoongladePure.Data.MySql;
using MoongladePure.Pingback;
using MoongladePure.Syndication;
using SixLabors.Fonts;
using System.Globalization;
using System.Text.Json.Serialization;
using WilderMinds.MetaWeblog;
using Encoder = MoongladePure.Web.Configuration.Encoder;

var info = $"App:\tMoonglade {Helper.AppVersion}\n" +
           $"Path:\t{Environment.CurrentDirectory} \n" +
           $"System:\t{Helper.TryGetFullOSVersion()} \n" +
           $"Host:\t{Environment.MachineName} \n" +
           $"User:\t{Environment.UserName}";
Console.WriteLine(info);

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

string connStr = builder.Configuration.GetConnectionString("MoongladeDatabase");

var cultures = new[] { "en-US", "zh-Hans" }.Select(p => new CultureInfo(p)).ToList();

ConfigureServices(builder.Services);

var app = builder.Build();

await FirstRun();

ConfigureMiddleware(app);

app.Run();

void ConfigureServices(IServiceCollection services)
{
    AppDomain.CurrentDomain.Load("Moonglade.FriendLink");
    AppDomain.CurrentDomain.Load("Moonglade.Menus");
    AppDomain.CurrentDomain.Load("Moonglade.Theme");
    AppDomain.CurrentDomain.Load("Moonglade.Configuration");
    AppDomain.CurrentDomain.Load("Moonglade.Data");

    services.AddMediatR(AppDomain.CurrentDomain.GetAssemblies());

    services.Configure<ForwardedHeadersOptions>(options => options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);

    services.AddOptions()
            .AddHttpContextAccessor()
            .AddRateLimit(builder.Configuration.GetSection("IpRateLimiting"));

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
        options.SupportedCultures = cultures;
        options.SupportedUICultures = cultures;
    }).Configure<RouteOptions>(options =>
    {
        options.LowercaseUrls = true;
        options.LowercaseQueryStrings = true;
        options.AppendTrailingSlash = false;
    });

    services.AddHealthChecks();
    services.AddPingback()
            .AddSyndication()
            .AddReleaseCheckerClient()
            .AddBlogCache()
            .AddMetaWeblog<MoongladePure.Web.MetaWeblogService>()
            .AddScoped<ValidateCaptcha>()
            .AddScoped<ITimeZoneResolver, BlogTimeZoneResolver>()
            .AddBlogConfig(builder.Configuration)
            .AddBlogAuthenticaton(builder.Configuration)
            .AddComments(builder.Configuration)
            .AddImageStorage(builder.Configuration, options => options.ContentRootPath = builder.Environment.ContentRootPath)
            .Configure<List<ManifestIcon>>(builder.Configuration.GetSection("ManifestIcons"));

    services.AddMySqlStorage(connStr);
}

async Task FirstRun()
{
    try
    {
        var startUpResut = await app.InitStartUp();
        switch (startUpResut)
        {
            case StartupInitResult.DatabaseConnectionFail:
                app.MapGet("/", () => Results.Problem(
                    detail: "Database connection test failed, please check your connection string and firewall settings, then RESTART Moonglade manually.",
                    statusCode: 500
                    ));
                app.Run();
                return;
            case StartupInitResult.DatabaseSetupFail:
                app.MapGet("/", () => Results.Problem(
                    detail: "Database setup failed, please check error log, then RESTART Moonglade manually.",
                    statusCode: 500
                ));
                app.Run();
                return;
        }
    }
    catch (Exception e)
    {
        app.MapGet("/", _ => throw new("Start up failed: " + e.Message));
        app.Run();
    }
}

void ConfigureMiddleware(IApplicationBuilder appBuilder)
{
    appBuilder.UseForwardedHeaders();
    appBuilder.UseHealthChecks(new PathString("/health"));
    app.Logger.LogWarning($"Running in environment: {app.Environment.EnvironmentName}.");

    appBuilder.UseCustomCss(options => options.MaxContentLength = 10240);
    appBuilder.UseManifest(options => options.ThemeColor = "#333333");
    appBuilder.UseRobotsTxt();

    appBuilder.UseOpenSearch(options =>
    {
        options.RequestPath = "/opensearch";
        options.IconFileType = "image/png";
        options.IconFilePath = "/favicon-16x16.png";
    });

    appBuilder.UseMiddleware<FoafMiddleware>();

    var bc = app.Services.GetRequiredService<IBlogConfig>();
    if (bc.AdvancedSettings.EnableMetaWeblog)
    {
        appBuilder.UseMiddleware<RSDMiddleware>().UseMetaWeblog("/metaweblog");
    }

    appBuilder.UseMiddleware<SiteMapMiddleware>()
              .UseMiddleware<PoweredByMiddleware>()
              .UseMiddleware<DNTMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        appBuilder.UseDeveloperExceptionPage();
    }
    else
    {
        appBuilder.UseStatusCodePages(ConfigureStatusCodePages.Handler).UseExceptionHandler("/error");
    }

    appBuilder.UseHttpsRedirection().UseHsts();
    appBuilder.UseRequestLocalization(new RequestLocalizationOptions
    {
        DefaultRequestCulture = new("en-US"),
        SupportedCultures = cultures,
        SupportedUICultures = cultures
    });

    appBuilder.UseStaticFiles();
    appBuilder.UseSession().UseCaptchaImage(options =>
    {
        options.RequestPath = "/captcha-image";
        options.ImageHeight = 36;
        options.ImageWidth = 100;
    });

    appBuilder.UseIpRateLimiting();
    appBuilder.UseRouting();
    appBuilder.UseAuthentication().UseAuthorization();

    appBuilder.UseEndpoints(ConfigureEndpoints.BlogEndpoints);
}