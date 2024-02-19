using MoongladePure.Caching;
using MoongladePure.Data.Spec;

namespace MoongladePure.Core.PostFeature;

public record PurgeRecycledCommand : IRequest;

public class PurgeRecycledCommandHandler(IBlogCache cache, IRepository<PostEntity> repo)
    : IRequestHandler<PurgeRecycledCommand>
{
    public async Task Handle(PurgeRecycledCommand request, CancellationToken ct)
    {
        var spec = new PostSpec(true);
        var posts = await repo.ListAsync(spec);
        await repo.DeleteAsync(posts, ct);

        foreach (var guid in posts.Select(p => p.Id))
        {
            cache.Remove(CacheDivision.Post, guid.ToString());
        }
    }
}