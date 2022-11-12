using MediatR;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Pingback;

public record GetPingbacksQuery : IRequest<IReadOnlyList<PingbackEntity>>;

public class GetPingbacksQueryHandler : IRequestHandler<GetPingbacksQuery, IReadOnlyList<PingbackEntity>>
{
    private readonly IRepository<PingbackEntity> _repo;

    public GetPingbacksQueryHandler(IRepository<PingbackEntity> repo) => _repo = repo;

    public Task<IReadOnlyList<PingbackEntity>> Handle(GetPingbacksQuery request, CancellationToken ct) => _repo.ListAsync(ct);
}