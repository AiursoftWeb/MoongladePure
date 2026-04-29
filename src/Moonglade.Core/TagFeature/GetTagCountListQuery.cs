using Microsoft.EntityFrameworkCore;

namespace MoongladePure.Core.TagFeature;

public record GetTagCountListQuery : IRequest<IReadOnlyList<KeyValuePair<Tag, int>>>;

public class GetTagCountListQueryHandler(IRepository<TagEntity> repo)
    : IRequestHandler<GetTagCountListQuery, IReadOnlyList<KeyValuePair<Tag, int>>>
{
    public async Task<IReadOnlyList<KeyValuePair<Tag, int>>> Handle(GetTagCountListQuery request, CancellationToken ct) =>
        await repo.AsQueryable()
            .Where(t => t.SiteId == SystemIds.DefaultSiteId)
            .Select(t => new KeyValuePair<Tag, int>(new()
            {
                Id = t.Id,
                DisplayName = t.DisplayName,
                NormalizedName = t.NormalizedName
            }, t.Posts.Count))
            .ToListAsync(ct);
}
