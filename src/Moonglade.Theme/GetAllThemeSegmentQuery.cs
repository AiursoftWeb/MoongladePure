using MediatR;
using Microsoft.EntityFrameworkCore;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Theme;

public record GetAllThemeSegmentQuery : IRequest<IReadOnlyList<ThemeSegment>>;

public class GetAllThemeSegmentQueryHandler(IRepository<BlogThemeEntity> repo)
    : IRequestHandler<GetAllThemeSegmentQuery, IReadOnlyList<ThemeSegment>>
{
    public async Task<IReadOnlyList<ThemeSegment>> Handle(GetAllThemeSegmentQuery request, CancellationToken ct)
    {
        return await repo.AsQueryable()
            .Where(p => p.SiteId == null || p.SiteId == SystemIds.DefaultSiteId)
            .Select(p => new ThemeSegment
            {
                Id = p.Id,
                Name = p.ThemeName
            })
            .ToListAsync(ct);
    }
}
