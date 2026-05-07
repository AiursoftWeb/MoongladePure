using MoongladePure.Data.Spec;
using MoongladePure.Utils;

namespace MoongladePure.Core.PostFeature;

public class ListPostsQuery(int pageSize, int pageIndex, Guid? catId = null, PostsSortBy sortBy = PostsSortBy.Recent)
    : IRequest<IReadOnlyList<PostDigest>>
{
    public int PageSize { get; set; } = pageSize;

    public int PageIndex { get; set; } = pageIndex;

    public Guid? CatId { get; set; } = catId;

    public PostsSortBy SortBy { get; set; } = sortBy;
}

public class ListPostsQueryHandler(
    IRepository<PostEntity> repo,
    IRepository<PostContentEntity> contentRepo,
    IRepository<AiArtifactEntity> artifactRepo,
    ISiteContext siteContext)
    : IRequestHandler<ListPostsQuery, IReadOnlyList<PostDigest>>
{
    public async Task<IReadOnlyList<PostDigest>> Handle(ListPostsQuery request, CancellationToken ct)
    {
        Helper.ValidatePagingParameters(request.PageSize, request.PageIndex);

        var spec = new PostPagingSpec(request.PageSize, request.PageIndex, request.CatId, request.SortBy, siteContext.SiteId);
        var posts = await repo.SelectAsync(spec, PostDigest.EntitySelector);
        await PostReadProjection.EnrichAsync(posts, contentRepo, artifactRepo, siteContext.SiteId, ct);
        return posts;
    }
}
