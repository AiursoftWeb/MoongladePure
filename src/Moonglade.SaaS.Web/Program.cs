using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MoongladePure.Data;
using MoongladePure.Data.Sqlite;
using MoongladePure.SaaS.Hosting;
using MoongladePure.SaaS.Identity;
using MoongladePure.SaaS.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<SaaSOptions>(builder.Configuration.GetSection("SaaS"));
builder.Services.AddDbContext<SqliteContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<BlogDbContext>(services => services.GetRequiredService<SqliteContext>());
builder.Services.AddScoped<CustomDomainSiteResolver>();
builder.Services.AddScoped<SaaSRootEndpoint>();
builder.Services.AddSingleton<UsernamePolicy>();
builder.Services.AddSingleton<SaaSHostClassifier>();

var app = builder.Build();

app.MapGet("/", (HttpContext context, SaaSRootEndpoint endpoint) => endpoint.HandleAsync(context));

app.MapFallback(SaaSRootEndpoint.NotRegistered);

app.Run();
