using Microsoft.EntityFrameworkCore;
using MoongladePure.Data;
using MoongladePure.Data.Entities;
using MoongladePure.SaaS.Identity;

namespace MoongladePure.SaaS.Hosting;

public sealed class UserSubdomainSiteResolver(BlogDbContext dbContext)
{
    public async Task<UserSubdomainSiteResolution> ResolveAsync(
        string username,
        string host,
        CancellationToken ct = default)
    {
        var normalizedUsername = UsernamePolicy.Normalize(username);
        var normalizedHost = SaaSHostClassifier.NormalizeHost(host);
        if (string.IsNullOrWhiteSpace(normalizedUsername) || string.IsNullOrWhiteSpace(normalizedHost))
        {
            return null;
        }

        return await dbContext.SiteDomain
            .Where(domain =>
                domain.Host == normalizedHost &&
                domain.VerificationStatus == SiteDomainVerificationStatus.Verified &&
                dbContext.Site.Any(site =>
                    site.Id == domain.SiteId &&
                    site.Status == SiteStatus.Active) &&
                dbContext.SiteMembership.Any(membership =>
                    membership.SiteId == domain.SiteId &&
                    membership.Role == SiteRole.Owner &&
                    dbContext.LocalAccount.Any(user =>
                        user.Id == membership.UserId &&
                        user.NormalizedUsername == normalizedUsername)))
            .Select(domain => new UserSubdomainSiteResolution(domain.SiteId, domain.Host, normalizedUsername))
            .SingleOrDefaultAsync(ct);
    }
}

public sealed record UserSubdomainSiteResolution(Guid SiteId, string Host, string Username);
