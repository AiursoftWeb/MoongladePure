using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Auth;

public record DeleteAccountCommand(Guid Id) : IRequest;

public class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand>
{
    private readonly IRepository<LocalAccountEntity> _repo;
    public DeleteAccountCommandHandler(IRepository<LocalAccountEntity> repo) => _repo = repo;

    public async Task Handle(DeleteAccountCommand request, CancellationToken ct)
    {
        var account = await _repo.GetAsync(request.Id, ct);
        if (account != null) await _repo.DeleteAsync(request.Id, ct);
    }
}