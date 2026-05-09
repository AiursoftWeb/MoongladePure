using Aiursoft.DbTools;
using Microsoft.EntityFrameworkCore;
using MoongladePure.Data;
using MoongladePure.Data.Sqlite;
using MoongladePure.SaaS.Hosting;
using MoongladePure.SaaS.Identity;
using MoongladePure.SaaS.Registration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<SaaSOptions>(builder.Configuration.GetSection("SaaS"));
builder.Services.AddDbContext<SqliteContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<BlogDbContext>(services => services.GetRequiredService<SqliteContext>());
builder.Services.AddScoped<CustomDomainSiteResolver>();
builder.Services.AddScoped<UserSubdomainSiteResolver>();
builder.Services.AddScoped<SaaSSiteProvisioningService>();
builder.Services.AddScoped<SaaSRegistrationEndpoint>();
builder.Services.AddScoped<SaaSRootEndpoint>();
builder.Services.AddSingleton<UsernamePolicy>();
builder.Services.AddSingleton<SaaSHostClassifier>();

var app = builder.Build();

await app.UpdateDbAsync<BlogDbContext>();

app.MapGet("/", (HttpContext context, SaaSRootEndpoint endpoint) => endpoint.HandleAsync(context));
app.MapGet("/register", (SaaSRegistrationEndpoint endpoint) => endpoint.ShowForm());
app.MapPost("/register", (HttpRequest request, SaaSRegistrationEndpoint endpoint, CancellationToken ct) =>
    endpoint.RegisterFormAsync(request, ct));
app.MapPost("/api/register", (SaaSRegistrationInput input, SaaSRegistrationEndpoint endpoint, CancellationToken ct) =>
    endpoint.RegisterJsonAsync(input, ct));

app.MapFallback(SaaSRootEndpoint.NotRegistered);

app.Run();
