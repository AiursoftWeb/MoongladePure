using Edi.Captcha;
using Microsoft.AspNetCore.HttpOverrides;
using MoongladePure.Data.MySql;
using MoongladePure.Syndication;
using System.Text.Json.Serialization;
using SixLabors.Fonts;
using System.Globalization;
using Aiursoft.Canon;
using Aiursoft.CSTools.Tools;
using Aiursoft.DbTools;
using Aiursoft.DbTools.Switchable;
using Aiursoft.GptClient;
using Aiursoft.WebTools.Abstractions.Models;
using AspNetCoreRateLimit;
using Aiursoft.Dotlang.Shared;
using MoongladePure.Core.AiFeature;
using MoongladePure.Data.Infrastructure;
using MoongladePure.Data.InMemory;
using MoongladePure.Data.Sqlite;
using Encoder = MoongladePure.Web.Configuration.Encoder;
using MoongladePure.Web.BackgroundJobs;

namespace MoongladePure.Web
{
    public class Startup : IWebStartup
    {
        private static readonly List<CultureInfo> Cultures =
            new[] { "en-US", "zh-CN" }.Select(p => new CultureInfo(p)).ToList();

        public void ConfigureServices(IConfiguration configuration, IWebHostEnvironment environment,
            IServiceCollection services)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            AppDomain.CurrentDomain.Load("MoongladePure.Core");
            AppDomain.CurrentDomain.Load("MoongladePure.FriendLink");
            AppDomain.CurrentDomain.Load("MoongladePure.Menus");
            AppDomain.CurrentDomain.Load("MoongladePure.Theme");
            AppDomain.CurrentDomain.Load("MoongladePure.Configuration");
            AppDomain.CurrentDomain.Load("MoongladePure.Data");

            services.AddMediatR(mediatRServiceConfiguration =>
                mediatRServiceConfiguration.RegisterServicesFromAssemblies(AppDomain.CurrentDomain.GetAssemblies()));

            services.Configure<ForwardedHeadersOptions>(options =>
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);

            services.AddOptions()
                .AddHttpContextAccessor()
                .AddRateLimit(configuration.GetSection("IpRateLimiting"));

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(20);
                options.Cookie.HttpOnly = true;
            }).AddSessionBasedCaptcha(options => options.FontStyle = FontStyle.Bold);

            services.AddLocalization(options => options.ResourcesPath = "Resources");
            services.AddControllers()
                .AddApplicationPart(typeof(Startup).Assembly)
                .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
                .ConfigureApiBehaviorOptions(ConfigureApiBehavior.BlogApiBehavior);
            services.AddRazorPages()
                .AddDataAnnotationsLocalization(options =>
                    options.DataAnnotationLocalizerProvider = (_, factory) => factory.Create(typeof(SharedResource)))
                .AddRazorPagesOptions(options =>
                {
                    options.Conventions.AddPageRoute("/Admin/Post", "admin");
                    options.Conventions.AuthorizeFolder("/Admin");
                    options.Conventions.AuthorizeFolder("/Settings");
                });

            // Fix Chinese character being encoded in HTML output
            services.AddSingleton(Encoder.MoongladeHtmlEncoder);
            services
                .Configure<RequestLocalizationOptions>(options =>
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

            var runBackgroundJobs = configuration.GetSection("BackgroundJobs:Enable").Get<bool>();
            if (runBackgroundJobs)
            {
                services.AddSingleton<IHostedService, PostAiProcessingJob>();
                services.AddSingleton<IHostedService, LangDetectJob>();
            }

            services.AddHttpClient();
            services.AddTaskCanon();
            services.AddScoped<OpenAiService>();
            services.AddGptClient();
            services
                .AddSyndication()
                .AddBlogCache()
                .AddScoped<ValidateCaptcha>()
                .AddBlogConfig(configuration)
                .AddBlogAuthenticaton(configuration)
                .AddComments(configuration)
                .AddImageStorage(configuration.GetSection("Storage"),
                    isTest: environment.IsDevelopment() || EntryExtends.IsInUnitTests())
                .Configure<List<ManifestIcon>>(configuration.GetSection("ManifestIcons"));

            services.AddScoped<OllamaBasedTranslatorEngine>();
            services.AddScoped<MarkdownShredder>();
            services.Configure<TranslateOptions>(options =>
            {
                options.OllamaInstance = configuration["OpenAI:CompletionApiUrl"];
                options.OllamaModel = configuration["OpenAI:Model"];
                options.OllamaToken = configuration["OpenAI:Token"];
            });

            var (connectionString, dbType, allowCache) = configuration.GetDbSettings();
            services.AddSwitchableRelationalDatabase(
                dbType: EntryExtends.IsInUnitTests() ? "InMemory" : dbType,
                connectionString: connectionString,
                supportedDbs: new List<SupportedDatabaseType<BlogDbContext>>
                {
                    new MySqlSupportedDb(allowCache: allowCache, splitQuery: false),
                    new SqliteSupportedDb(allowCache: allowCache, splitQuery: true),
                    new InMemorySupportedDb()
                });
            services.AddScoped(typeof(IRepository<>), typeof(BlogDbContextRepository<>));
        }

        public void Configure(WebApplication app)
        {
            app.UseForwardedHeaders();

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

            app.UseMiddleware<SiteMapMiddleware>()
                .UseMiddleware<PoweredByMiddleware>()
                .UseMiddleware<DNTMiddleware>();

            if (app.Environment.IsDevelopment() || EntryExtends.IsInUnitTests())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseStatusCodePages(ConfigureStatusCodePages.Handler).UseExceptionHandler("/error");
            }

            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new("en-US"),
                SupportedCultures = Cultures,
                SupportedUICultures = Cultures
            });

            app.UseStaticFiles();
            app.UseSession().UseSessionCaptcha(options =>
            {
                options.RequestPath = "/captcha-image";
                options.ImageHeight = 36;
                options.ImageWidth = 100;
            });

            app.UseIpRateLimiting();
            app.UseRouting();
            app.UseAuthentication().UseAuthorization();
            app.MapControllers();
            app.MapRazorPages();
        }
    }
}
