using MoongladePure.Data.Spec;

namespace MoongladePure.Core.PostFeature;

public record ListArchiveQuery(int Year, int? Month = null) : IRequest<IReadOnlyList<PostDigest>>;

public class ListArchiveQueryHandler(
    IRepository<PostEntity> repo,
    IRepository<PostContentEntity> contentRepo,
    IRepository<AiArtifactEntity> artifactRepo,
    ISiteContext siteContext)
    : IRequestHandler<ListArchiveQuery, IReadOnlyList<PostDigest>>
{
    public async Task<IReadOnlyList<PostDigest>> Handle(ListArchiveQuery request, CancellationToken ct)
    {
        var spec = new PostSpec(request.Year, request.Month.GetValueOrDefault(), siteContext.SiteId);
        var list = await repo.SelectAsync(spec, PostDigest.EntitySelector);
        await PostReadProjection.EnrichAsync(list, contentRepo, artifactRepo, siteContext.SiteId, ct);
        return list;
    }
}
