namespace MoongladePure.Core.StatisticFeature;

public record UpdateStatisticCommand(Guid PostId, bool IsLike) : IRequest;

public class UpdateStatisticCommandHandler(IRepository<PostExtensionEntity> repo, ISiteContext siteContext)
    : IRequestHandler<UpdateStatisticCommand>
{
    public async Task Handle(UpdateStatisticCommand request, CancellationToken ct)
    {
        var pp = await repo.GetAsync(p => p.SiteId == siteContext.SiteId && p.PostId == request.PostId);
        if (pp is null) return;

        if (request.IsLike)
        {
            if (pp.Likes >= int.MaxValue) return;
            pp.Likes += 1;
        }
        else
        {
            if (pp.Hits >= int.MaxValue) return;
            pp.Hits += 1;
        }

        await repo.UpdateAsync(pp, ct);
    }
}
