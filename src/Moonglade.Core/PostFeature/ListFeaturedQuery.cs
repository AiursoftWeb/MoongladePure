using MoongladePure.Data.Spec;
using MoongladePure.Utils;

namespace MoongladePure.Core.PostFeature;

public record ListFeaturedQuery(int PageSize, int PageIndex) : IRequest<IReadOnlyList<PostDigest>>;

public class ListFeaturedQueryHandler(IRepository<PostEntity> repo, ISiteContext siteContext)
    : IRequestHandler<ListFeaturedQuery, IReadOnlyList<PostDigest>>
{
    public Task<IReadOnlyList<PostDigest>> Handle(ListFeaturedQuery request, CancellationToken ct)
    {
        var (pageSize, pageIndex) = request;
        Helper.ValidatePagingParameters(pageSize, pageIndex);

        var posts = repo.SelectAsync(new FeaturedPostSpec(pageSize, pageIndex, siteContext.SiteId), PostDigest.EntitySelector);
        return posts;
    }
}
