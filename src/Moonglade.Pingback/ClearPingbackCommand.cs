using MediatR;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Pingback;

public record ClearPingbackCommand : IRequest;

public class ClearPingbackCommandHandler : AsyncRequestHandler<ClearPingbackCommand>
{
    private readonly IRepository<PingbackEntity> _repo;

    public ClearPingbackCommandHandler(IRepository<PingbackEntity> repo) => _repo = repo;

    protected override Task Handle(ClearPingbackCommand request, CancellationToken ct) =>
        _repo.Clear(ct);
}