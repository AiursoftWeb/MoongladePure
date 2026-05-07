using Microsoft.EntityFrameworkCore;

namespace MoongladePure.Core.SiteFeature;

public record ListSitesQuery : IRequest<IReadOnlyList<SiteDigest>>;

public class ListSitesQueryHandler(
    IRepository<SiteEntity> siteRepo,
    IRepository<SiteDomainEntity> domainRepo)
    : IRequestHandler<ListSitesQuery, IReadOnlyList<SiteDigest>>
{
    public async Task<IReadOnlyList<SiteDigest>> Handle(ListSitesQuery request, CancellationToken ct)
    {
        var sites = await siteRepo.AsQueryable()
            .AsNoTracking()
            .OrderBy(site => site.Name)
            .Select(site => new SiteDigest
            {
                Id = site.Id,
                Name = site.Name,
                Slug = site.Slug,
                Status = site.Status,
                DefaultCulture = site.DefaultCulture,
                TimeZoneId = site.TimeZoneId
            })
            .ToListAsync(ct);

        var siteIds = sites.Select(site => site.Id).ToArray();
        var domains = await domainRepo.AsQueryable()
            .AsNoTracking()
            .Where(domain => siteIds.Contains(domain.SiteId))
            .OrderBy(domain => domain.Host)
            .Select(domain => new SiteDomainDigest
            {
                Id = domain.Id,
                SiteId = domain.SiteId,
                Host = domain.Host,
                IsPrimary = domain.IsPrimary
            })
            .ToListAsync(ct);

        foreach (var site in sites)
        {
            site.Domains = domains
                .Where(domain => domain.SiteId == site.Id)
                .ToList();
        }

        return sites;
    }
}
