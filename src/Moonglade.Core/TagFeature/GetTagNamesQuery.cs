using Microsoft.EntityFrameworkCore;

namespace MoongladePure.Core.TagFeature;

public record GetTagNamesQuery : IRequest<IReadOnlyList<string>>;

public class GetTagNamesQueryHandler(IRepository<TagEntity> repo, ISiteContext siteContext)
    : IRequestHandler<GetTagNamesQuery, IReadOnlyList<string>>
{
    public async Task<IReadOnlyList<string>> Handle(GetTagNamesQuery request, CancellationToken ct) =>
        await repo.AsQueryable()
            .Where(t => t.SiteId == siteContext.SiteId)
            .Select(t => t.DisplayName)
            .ToListAsync(ct);
}
