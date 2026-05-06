using MoongladePure.Caching;

namespace MoongladePure.Core.PostFeature;

public record DeletePostCommand(Guid Id, bool SoftDelete = false) : IRequest;

public class DeletePostCommandHandler(IRepository<PostEntity> repo, IBlogCache cache, ISiteContext siteContext)
    : IRequestHandler<DeletePostCommand>
{
    public async Task Handle(DeletePostCommand request, CancellationToken ct)
    {
        var (guid, softDelete) = request;
        var post = await repo.GetAsync(p => p.SiteId == siteContext.SiteId && p.Id == guid);
        if (null == post) return;

        if (softDelete)
        {
            post.IsDeleted = true;
            await repo.UpdateAsync(post, ct);
        }
        else
        {
            await repo.DeleteAsync(post, ct);
        }

        cache.Remove(CacheDivision.Post, $"{siteContext.SiteId}:{guid}");
    }
}
