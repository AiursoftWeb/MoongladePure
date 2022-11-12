using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Auth;

public record AccountExistsQuery(string Username) : IRequest<bool>;

public class AccountExistsQueryHandler : IRequestHandler<AccountExistsQuery, bool>
{
    private readonly IRepository<LocalAccountEntity> _repo;

    public AccountExistsQueryHandler(IRepository<LocalAccountEntity> repo) => _repo = repo;

    public Task<bool> Handle(AccountExistsQuery request, CancellationToken ct) =>
        _repo.AnyAsync(p => p.Username == request.Username.ToLower(), ct);
}