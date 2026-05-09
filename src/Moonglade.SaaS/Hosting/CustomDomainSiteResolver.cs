using Microsoft.EntityFrameworkCore;
using MoongladePure.Data;
using MoongladePure.Data.Entities;

namespace MoongladePure.SaaS.Hosting;

public sealed class CustomDomainSiteResolver(BlogDbContext dbContext)
{
    public async Task<CustomDomainSiteResolution> ResolveAsync(string host, CancellationToken ct = default)
    {
        var normalizedHost = SaaSHostClassifier.NormalizeHost(host);
        if (string.IsNullOrWhiteSpace(normalizedHost))
        {
            return null;
        }

        return await dbContext.SiteDomain
            .Where(domain =>
                domain.Host == normalizedHost &&
                domain.VerificationStatus == SiteDomainVerificationStatus.Verified)
            .Select(domain => new CustomDomainSiteResolution(domain.SiteId, domain.Host))
            .SingleOrDefaultAsync(ct);
    }
}

public sealed record CustomDomainSiteResolution(Guid SiteId, string Host);
