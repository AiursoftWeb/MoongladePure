using MoongladePure.Data.Spec;
using MoongladePure.Utils;

namespace MoongladePure.Core.PostFeature;

public record ListByTagQuery(int TagId, int PageSize, int PageIndex) : IRequest<IReadOnlyList<PostDigest>>;

public class ListByTagQueryHandler(
    IRepository<PostTagEntity> repo,
    IRepository<PostContentEntity> contentRepo,
    IRepository<AiArtifactEntity> artifactRepo,
    ISiteContext siteContext)
    : IRequestHandler<ListByTagQuery, IReadOnlyList<PostDigest>>
{
    public async Task<IReadOnlyList<PostDigest>> Handle(ListByTagQuery request, CancellationToken ct)
    {
        if (request.TagId <= 0) throw new ArgumentOutOfRangeException(nameof(request.TagId));
        Helper.ValidatePagingParameters(request.PageSize, request.PageIndex);

        var posts = await repo.SelectAsync(new PostTagSpec(request.TagId, request.PageSize, request.PageIndex, siteContext.SiteId), PostDigest.EntitySelectorByTag);
        await PostReadProjection.EnrichAsync(posts, contentRepo, artifactRepo, siteContext.SiteId, ct);
        return posts;
    }
}
