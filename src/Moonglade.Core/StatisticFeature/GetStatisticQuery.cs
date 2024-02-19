namespace MoongladePure.Core.StatisticFeature;

public record GetStatisticQuery(Guid PostId) : IRequest<(int Hits, int Likes)>;

public class GetStatisticQueryHandler(IRepository<PostExtensionEntity> repo)
    : IRequestHandler<GetStatisticQuery, (int Hits, int Likes)>
{
    public async Task<(int Hits, int Likes)> Handle(GetStatisticQuery request, CancellationToken ct)
    {
        var pp = await repo.GetAsync(request.PostId, ct);
        return (pp.Hits, pp.Likes);
    }
}