using Microsoft.EntityFrameworkCore;

namespace MoongladePure.Core.PageFeature;

public record ListPageSegmentQuery : IRequest<IReadOnlyList<PageSegment>>;

public class ListPageSegmentQueryHandler(IRepository<PageEntity> repo, ISiteContext siteContext)
    : IRequestHandler<ListPageSegmentQuery, IReadOnlyList<PageSegment>>
{
    public async Task<IReadOnlyList<PageSegment>> Handle(ListPageSegmentQuery request, CancellationToken ct)
    {
        return await repo.AsQueryable()
            .Where(page => page.SiteId == siteContext.SiteId)
            .Select(page => new PageSegment
            {
                Id = page.Id,
                CreateTimeUtc = page.CreateTimeUtc,
                Slug = page.Slug,
                Title = page.Title,
                IsPublished = page.IsPublished
            })
            .ToListAsync(ct);
    }
}
