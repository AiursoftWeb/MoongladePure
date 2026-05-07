using MoongladePure.Data.Spec;
using MoongladePure.Utils;

namespace MoongladePure.Core.PostFeature;

public record ListFeaturedQuery(int PageSize, int PageIndex) : IRequest<IReadOnlyList<PostDigest>>;

public class ListFeaturedQueryHandler(
    IRepository<PostEntity> repo,
    IRepository<PostContentEntity> contentRepo,
    IRepository<AiArtifactEntity> artifactRepo,
    ISiteContext siteContext)
    : IRequestHandler<ListFeaturedQuery, IReadOnlyList<PostDigest>>
{
    public async Task<IReadOnlyList<PostDigest>> Handle(ListFeaturedQuery request, CancellationToken ct)
    {
        var (pageSize, pageIndex) = request;
        Helper.ValidatePagingParameters(pageSize, pageIndex);

        var posts = await repo.SelectAsync(new FeaturedPostSpec(pageSize, pageIndex, siteContext.SiteId), PostDigest.EntitySelector);
        await PostReadProjection.EnrichAsync(posts, contentRepo, artifactRepo, siteContext.SiteId, ct);
        return posts;
    }
}
