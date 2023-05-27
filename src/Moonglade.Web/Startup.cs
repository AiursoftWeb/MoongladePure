using Edi.Captcha;
using Microsoft.AspNetCore.HttpOverrides;
using MoongladePure.Data.MySql;
using MoongladePure.Syndication;
using System.Text.Json.Serialization;
using SixLabors.Fonts;
using WilderMinds.MetaWeblog;
using System.Globalization;
using AspNetCoreRateLimit;
using Encoder = MoongladePure.Web.Configuration.Encoder;

namespace MoongladePure.Web
{
    public class Startup
    {
        private IHostEnvironment Environment { get; }
        public IConfiguration Configuration { get; }
        private static readonly List<CultureInfo> Cultures = new[] { "en-US", "zh-CN" }.Select(p => new CultureInfo(p)).ToList();

        public Startup(
            IHostEnvironment environment,
            IConfiguration configuration)
        {
            Environment = environment;
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            AppDomain.CurrentDomain.Load("MoongladePure.Core");
            AppDomain.CurrentDomain.Load("MoongladePure.FriendLink");
            AppDomain.CurrentDomain.Load("MoongladePure.Menus");
            AppDomain.CurrentDomain.Load("MoongladePure.Theme");
            AppDomain.CurrentDomain.Load("MoongladePure.Configuration");
            AppDomain.CurrentDomain.Load("MoongladePure.Data");

            services.AddMediatR(mediatRServiceConfiguration => mediatRServiceConfiguration.RegisterServicesFromAssemblies(AppDomain.CurrentDomain.GetAssemblies()));

            services.Configure<ForwardedHeadersOptions>(options => options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);

            services.AddOptions()
                    .AddHttpContextAccessor()
                    .AddRateLimit(Configuration.GetSection("IpRateLimiting"));

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
            services
                .AddSyndication()
                .AddBlogCache()
                .AddMetaWeblog<MetaWeblogService>()
                .AddScoped<ValidateCaptcha>()
                .AddBlogConfig(Configuration)
                .AddBlogAuthenticaton(Configuration)
                .AddComments(Configuration)
                .AddImageStorage(Configuration, Environment.IsDevelopment())
                .Configure<List<ManifestIcon>>(Configuration.GetSection("ManifestIcons"));

            var connStr = Configuration.GetConnectionString("MoongladeDatabase");
            services.AddDatabase(connStr, useTestDb: Environment.IsDevelopment());

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseForwardedHeaders();
            app.UseHealthChecks(new PathString("/health"));

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

            app.UseMiddleware<RSDMiddleware>().UseMetaWeblog("/metaweblog");
            app.UseMiddleware<SiteMapMiddleware>()
                .UseMiddleware<PoweredByMiddleware>()
                .UseMiddleware<DNTMiddleware>();

            if (env.IsDevelopment())
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
}
