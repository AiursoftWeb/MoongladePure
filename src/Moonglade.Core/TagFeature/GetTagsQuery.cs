using Microsoft.EntityFrameworkCore;

namespace MoongladePure.Core.TagFeature;

public record GetTagsQuery : IRequest<IReadOnlyList<Tag>>;

public class GetTagsQueryHandler(IRepository<TagEntity> repo) : IRequestHandler<GetTagsQuery, IReadOnlyList<Tag>>
{
    public async Task<IReadOnlyList<Tag>> Handle(GetTagsQuery request, CancellationToken ct) =>
        await repo.AsQueryable()
            .Where(t => t.SiteId == SystemIds.DefaultSiteId)
            .Select(Tag.EntitySelector)
            .ToListAsync(ct);
}
