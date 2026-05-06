using MoongladePure.Data.Spec;

namespace MoongladePure.Core.PostFeature;

public record GetDraftQuery(Guid Id) : IRequest<Post>;

public class GetDraftQueryHandler(IRepository<PostEntity> repo, ISiteContext siteContext) : IRequestHandler<GetDraftQuery, Post>
{
    public Task<Post> Handle(GetDraftQuery request, CancellationToken ct)
    {
        var spec = new PostSpec(request.Id, siteId: siteContext.SiteId);
        var post = repo.FirstOrDefaultAsync(spec, Post.EntitySelector);
        return post;
    }
}
