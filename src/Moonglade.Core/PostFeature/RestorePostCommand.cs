using MoongladePure.Caching;

namespace MoongladePure.Core.PostFeature;

public record RestorePostCommand(Guid Id) : IRequest;

public class RestorePostCommandHandler(IRepository<PostEntity> repo, IBlogCache cache, ISiteContext siteContext)
    : IRequestHandler<RestorePostCommand>
{
    public async Task Handle(RestorePostCommand request, CancellationToken ct)
    {
        var pp = await repo.GetAsync(p => p.SiteId == siteContext.SiteId && p.Id == request.Id);
        if (null == pp) return;

        pp.IsDeleted = false;
        await repo.UpdateAsync(pp, ct);

        cache.Remove(CacheDivision.Post, $"{siteContext.SiteId}:{request.Id}");
    }
}
