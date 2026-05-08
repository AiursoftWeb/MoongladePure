using Microsoft.Extensions.Options;
using MoongladePure.SaaS.Hosting;
using MoongladePure.SaaS.Identity;

const string PortalHtml = """
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>MoongladePure</title>
  <style>
    body { margin: 0; font-family: system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif; background: #f7f5ef; color: #152033; }
    main { min-height: 100vh; display: grid; place-items: center; padding: 32px; }
    section { max-width: 760px; }
    h1 { margin: 0 0 16px; font-size: 64px; line-height: 1; }
    p { margin: 0 0 28px; max-width: 620px; font-size: 20px; line-height: 1.6; color: #40516b; }
    a { display: inline-block; padding: 12px 18px; border-radius: 6px; background: #152033; color: white; text-decoration: none; font-weight: 650; }
    @media (max-width: 640px) { h1 { font-size: 44px; } p { font-size: 18px; } }
  </style>
</head>
<body>
  <main>
    <section>
      <h1>MoongladePure</h1>
      <p>Launch a clean multi-user blog platform with managed subdomains, custom domains, and the Moonglade publishing experience.</p>
      <a href="/register">Start publishing</a>
    </section>
  </main>
</body>
</html>
""";

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<SaaSOptions>(builder.Configuration.GetSection("SaaS"));
builder.Services.AddSingleton<UsernamePolicy>();
builder.Services.AddSingleton<SaaSHostClassifier>();

var app = builder.Build();

app.MapGet("/", (HttpContext context, IOptions<SaaSOptions> options, SaaSHostClassifier classifier) =>
{
    var resolution = classifier.Classify(context.Request.Host.Value, options.Value);
    return resolution.Kind switch
    {
        SaaSHostKind.Portal => Results.Content(PortalHtml, "text/html; charset=utf-8"),
        SaaSHostKind.UserSubdomain => Results.Content($"Site subdomain reserved for {resolution.Username}.", "text/plain; charset=utf-8"),
        _ => Results.NotFound("This domain is not registered on this MoongladePure SaaS platform.")
    };
});

app.MapFallback(() => Results.NotFound("This domain is not registered on this MoongladePure SaaS platform."));

app.Run();
