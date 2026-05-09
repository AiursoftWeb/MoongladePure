using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace MoongladePure.SaaS.Hosting;

public sealed class SaaSRootEndpoint(
    IOptions<SaaSOptions> options,
    SaaSHostClassifier classifier,
    CustomDomainSiteResolver customDomainSiteResolver)
{
    private const string NotRegisteredMessage = "This domain is not registered on this MoongladePure SaaS platform.";

    public async Task<IResult> HandleAsync(HttpContext context)
    {
        var resolution = classifier.Classify(context.Request.Host.Value, options.Value);
        return resolution.Kind switch
        {
            SaaSHostKind.Portal => Results.Content(PortalHtml.Content, "text/html; charset=utf-8"),
            SaaSHostKind.UserSubdomain => Results.Content($"Site subdomain reserved for {resolution.Username}.", "text/plain; charset=utf-8"),
            SaaSHostKind.CustomDomainCandidate => await ResolveCustomDomainAsync(resolution.Host, context.RequestAborted),
            _ => Results.NotFound(NotRegisteredMessage)
        };
    }

    public static IResult NotRegistered() => Results.NotFound(NotRegisteredMessage);

    private async Task<IResult> ResolveCustomDomainAsync(string host, CancellationToken ct)
    {
        var site = await customDomainSiteResolver.ResolveAsync(host, ct);
        return site is null
            ? Results.NotFound(NotRegisteredMessage)
            : Results.Content($"Site domain resolved for {site.SiteId}.", "text/plain; charset=utf-8");
    }
}
