using MoongladePure.Data.Spec;

namespace MoongladePure.Core.PostFeature;

public record GetDraftQuery(Guid Id) : IRequest<Post>;

public class GetDraftQueryHandler(
    IRepository<PostEntity> repo,
    IRepository<PostContentEntity> contentRepo,
    IRepository<AiArtifactEntity> artifactRepo,
    ISiteContext siteContext) : IRequestHandler<GetDraftQuery, Post>
{
    public async Task<Post> Handle(GetDraftQuery request, CancellationToken ct)
    {
        var spec = new PostSpec(request.Id, siteId: siteContext.SiteId);
        var post = await repo.FirstOrDefaultAsync(spec, Post.EntitySelector);
        await PostReadProjection.EnrichAsync(post, contentRepo, artifactRepo, siteContext.SiteId, ct);
        return post;
    }
}
