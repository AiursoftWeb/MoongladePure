using Microsoft.AspNetCore.Http;
using MoongladePure.Data.Entities;

namespace MoongladePure.Data.Infrastructure;

public interface ISiteContext
{
    Guid SiteId { get; }
}

public sealed class DefaultSiteContext : ISiteContext
{
    public Guid SiteId => SystemIds.DefaultSiteId;
}

public sealed class RequestSiteContext(IHttpContextAccessor httpContextAccessor, BlogDbContext dbContext) : ISiteContext
{
    private bool _resolved;
    private Guid _siteId;

    public Guid SiteId
    {
        get
        {
            if (_resolved)
            {
                return _siteId;
            }

            _siteId = ResolveSiteId();
            _resolved = true;

            return _siteId;
        }
    }

    private Guid ResolveSiteId()
    {
        var host = NormalizeHost(httpContextAccessor.HttpContext?.Request.Host.Host);
        if (string.IsNullOrWhiteSpace(host))
        {
            return SystemIds.DefaultSiteId;
        }

        var siteId = dbContext.SiteDomain
            .AsNoTracking()
            .Where(domain => domain.Host.ToLower() == host)
            .Select(domain => domain.SiteId)
            .FirstOrDefault();

        return siteId == Guid.Empty ? SystemIds.DefaultSiteId : siteId;
    }

    internal static string NormalizeHost(string host) =>
        string.IsNullOrWhiteSpace(host) ? null : host.Trim().ToLowerInvariant();
}
