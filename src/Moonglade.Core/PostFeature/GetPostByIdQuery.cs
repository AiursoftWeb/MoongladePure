using MoongladePure.Data.Spec;

namespace MoongladePure.Core.PostFeature;

public record GetPostByIdQuery(Guid Id) : IRequest<Post>;

public class GetPostByIdQueryHandler(IRepository<PostEntity> repo, ISiteContext siteContext) : IRequestHandler<GetPostByIdQuery, Post>
{
    public Task<Post> Handle(GetPostByIdQuery request, CancellationToken ct)
    {
        var spec = new PostSpec(request.Id, siteId: siteContext.SiteId);
        var post = repo.FirstOrDefaultAsync(spec, Post.EntitySelector);
        return post;
    }
}
