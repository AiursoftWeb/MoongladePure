using Microsoft.EntityFrameworkCore;
using MoongladePure.Data;
using MoongladePure.Data.Entities;
using MoongladePure.SaaS.Hosting;

namespace MoongladePure.SaaS.Domains;

public sealed class SaaSCustomDomainService(BlogDbContext dbContext)
{
    public async Task<IReadOnlyList<SaaSCustomDomainDigest>> ListAsync(Guid siteId, CancellationToken ct = default)
    {
        var domains = await dbContext.SiteDomain
            .Where(domain => domain.SiteId == siteId)
            .OrderByDescending(domain => domain.IsPrimary)
            .ThenBy(domain => domain.Host)
            .ToListAsync(ct);

        return domains.Select(ToDigest).ToList();
    }

    public async Task<SaaSCustomDomainResult> AddPendingAsync(
        Guid siteId,
        SaaSCustomDomainRequest request,
        CancellationToken ct = default)
    {
        var host = SaaSHostClassifier.NormalizeHost(request.Host);
        if (string.IsNullOrWhiteSpace(host))
        {
            return SaaSCustomDomainResult.Fail("Host is required.");
        }

        if (!await dbContext.Site.AnyAsync(site => site.Id == siteId, ct))
        {
            return SaaSCustomDomainResult.Fail("Site does not exist.");
        }

        if (await dbContext.SiteDomain.AnyAsync(domain => domain.Host == host, ct))
        {
            return SaaSCustomDomainResult.Fail("Host is already registered.");
        }

        var now = DateTime.UtcNow;
        var domain = new SiteDomainEntity
        {
            Id = Guid.NewGuid(),
            SiteId = siteId,
            Host = host,
            IsPrimary = false,
            VerificationStatus = SiteDomainVerificationStatus.PendingVerification,
            VerificationToken = CustomDomainVerification.CreateToken(),
            CreatedAtUtc = now
        };

        dbContext.SiteDomain.Add(domain);
        await dbContext.SaveChangesAsync(ct);

        return SaaSCustomDomainResult.Success(ToDigest(domain));
    }

    public async Task<SaaSCustomDomainResult> DeleteAsync(Guid siteId, Guid domainId, CancellationToken ct = default)
    {
        var domain = await dbContext.SiteDomain.SingleOrDefaultAsync(
            item => item.Id == domainId && item.SiteId == siteId,
            ct);

        if (domain is null)
        {
            return SaaSCustomDomainResult.Fail("Domain does not exist.");
        }

        if (domain.IsPrimary)
        {
            return SaaSCustomDomainResult.Fail("Primary site domain cannot be deleted.");
        }

        var digest = ToDigest(domain);
        dbContext.SiteDomain.Remove(domain);
        await dbContext.SaveChangesAsync(ct);

        return SaaSCustomDomainResult.Success(digest);
    }

    private static SaaSCustomDomainDigest ToDigest(SiteDomainEntity domain) =>
        new()
        {
            Id = domain.Id,
            SiteId = domain.SiteId,
            Host = domain.Host,
            IsPrimary = domain.IsPrimary,
            VerificationStatus = domain.VerificationStatus,
            VerificationToken = domain.VerificationToken,
            TxtRecordName = CustomDomainVerification.BuildTxtRecordName(domain.Host),
            TxtRecordValue = CustomDomainVerification.BuildTxtRecordValue(domain.VerificationToken),
            CreatedAtUtc = domain.CreatedAtUtc,
            LastVerifiedAtUtc = domain.LastVerifiedAtUtc,
            VerifiedAtUtc = domain.VerifiedAtUtc,
            LastVerificationError = domain.LastVerificationError
        };
}
