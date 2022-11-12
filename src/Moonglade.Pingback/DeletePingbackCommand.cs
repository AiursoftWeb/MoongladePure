using MediatR;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Pingback;

public record DeletePingbackCommand(Guid Id) : IRequest;

public class DeletePingbackCommandHandler : AsyncRequestHandler<DeletePingbackCommand>
{
    private readonly IRepository<PingbackEntity> _repo;

    public DeletePingbackCommandHandler(IRepository<PingbackEntity> repo) => _repo = repo;

    protected override Task Handle(DeletePingbackCommand request, CancellationToken ct) =>
        _repo.DeleteAsync(request.Id, ct);
}