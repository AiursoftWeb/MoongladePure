using MoongladePure.Data.Spec;
using System.Linq.Expressions;

namespace MoongladePure.Core.PostFeature;

public record struct Archive(int Year, int Month, int Count);
public record GetArchiveQuery : IRequest<IReadOnlyList<Archive>>;

public class GetArchiveQueryHandler(IRepository<PostEntity> repo)
    : IRequestHandler<GetArchiveQuery, IReadOnlyList<Archive>>
{
    public async Task<IReadOnlyList<Archive>> Handle(GetArchiveQuery request, CancellationToken ct)
    {
        if (!await repo.AnyAsync(p => p.IsPublished && !p.IsDeleted, ct))
        {
            return new List<Archive>();
        }

        var spec = new PostSpec(PostStatus.Published);
        var dates = await repo.SelectAsync(spec, p => p.PubDateUtc);

        var list = dates
            .Where(d => d.HasValue)
            .Select(d => d.Value)
            .GroupBy(d => new { d.Year, d.Month })
            .Select(g => new Archive(g.Key.Year, g.Key.Month, g.Count()))
            .OrderByDescending(a => a.Year)
            .ThenByDescending(a => a.Month)
            .ToList();

        return list;
    }
}