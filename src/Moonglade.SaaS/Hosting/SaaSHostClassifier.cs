using MoongladePure.SaaS.Identity;

namespace MoongladePure.SaaS.Hosting;

public sealed class SaaSHostClassifier(UsernamePolicy usernamePolicy)
{
    public SaaSHostResolution Classify(string host, SaaSOptions options)
    {
        var normalizedHost = NormalizeHost(host);
        if (string.IsNullOrWhiteSpace(normalizedHost))
        {
            return SaaSHostResolution.Unknown(normalizedHost);
        }

        if (IsPortalHost(normalizedHost, options.PortalHosts))
        {
            return new SaaSHostResolution(SaaSHostKind.Portal, normalizedHost);
        }

        var userSubdomain = TryGetUserSubdomain(normalizedHost, options.SiteSubdomainRoot);
        if (userSubdomain.IsPlatformSubdomain)
        {
            return userSubdomain.Username is not null && usernamePolicy.Validate(userSubdomain.Username).Succeeded
                ? new SaaSHostResolution(SaaSHostKind.UserSubdomain, normalizedHost, userSubdomain.Username)
                : SaaSHostResolution.Unknown(normalizedHost);
        }

        return new SaaSHostResolution(SaaSHostKind.CustomDomainCandidate, normalizedHost);
    }

    public static string NormalizeHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return null;
        }

        var normalized = host.Trim().ToLowerInvariant();
        var separatorIndex = normalized.LastIndexOf(':');
        var hostWithoutPort = separatorIndex <= 0 ? normalized : normalized[..separatorIndex];
        return hostWithoutPort.TrimEnd('.');
    }

    private static bool IsPortalHost(string host, IEnumerable<string> portalHosts) =>
        portalHosts
            .Select(NormalizeHost)
            .Any(portalHost => portalHost == host);

    private static UserSubdomainMatch TryGetUserSubdomain(string host, string siteSubdomainRoot)
    {
        var root = NormalizeHost(siteSubdomainRoot);
        if (string.IsNullOrWhiteSpace(root) || host == root)
        {
            return new UserSubdomainMatch(false);
        }

        var suffix = "." + root;
        if (!host.EndsWith(suffix, StringComparison.Ordinal))
        {
            return new UserSubdomainMatch(false);
        }

        var prefix = host[..^suffix.Length];
        return prefix.Contains('.', StringComparison.Ordinal)
            ? new UserSubdomainMatch(true)
            : new UserSubdomainMatch(true, prefix);
    }

    private sealed record UserSubdomainMatch(bool IsPlatformSubdomain, string Username = null);
}
