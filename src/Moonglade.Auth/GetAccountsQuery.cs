using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Auth;

public record GetAccountsQuery : IRequest<IReadOnlyList<Account>>;

public class GetAccountsQueryHandler : IRequestHandler<GetAccountsQuery, IReadOnlyList<Account>>
{
    private readonly IRepository<LocalAccountEntity> _repo;

    public GetAccountsQueryHandler(IRepository<LocalAccountEntity> repo) => _repo = repo;

    public Task<IReadOnlyList<Account>> Handle(GetAccountsQuery request, CancellationToken ct)
    {
        return _repo.SelectAsync(p => new Account
        {
            Id = p.Id,
            CreateTimeUtc = p.CreateTimeUtc,
            LastLoginIp = p.LastLoginIp,
            LastLoginTimeUtc = p.LastLoginTimeUtc,
            Username = p.Username
        }, ct);
    }
}