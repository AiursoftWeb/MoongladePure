using MoongladePure.Data.Spec;

namespace MoongladePure.Core.PostFeature;

public record GetPostByIdQuery(Guid Id) : IRequest<Post>;

public class GetPostByIdQueryHandler(
    IRepository<PostEntity> repo,
    IRepository<PostContentEntity> contentRepo,
    IRepository<AiArtifactEntity> artifactRepo,
    ISiteContext siteContext) : IRequestHandler<GetPostByIdQuery, Post>
{
    public async Task<Post> Handle(GetPostByIdQuery request, CancellationToken ct)
    {
        var spec = new PostSpec(request.Id, siteId: siteContext.SiteId);
        var post = await repo.FirstOrDefaultAsync(spec, Post.EntitySelector);
        await PostReadProjection.EnrichAsync(post, contentRepo, artifactRepo, siteContext.SiteId, ct);
        return post;
    }
}
