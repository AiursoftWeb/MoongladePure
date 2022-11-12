using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Auth;

public record CountAccountsQuery : IRequest<int>;

public class CountAccountsQueryHandler : IRequestHandler<CountAccountsQuery, int>
{
    private readonly IRepository<LocalAccountEntity> _repo;

    public CountAccountsQueryHandler(IRepository<LocalAccountEntity> repo) => _repo = repo;

    public Task<int> Handle(CountAccountsQuery request, CancellationToken ct) => _repo.CountAsync(ct: ct);
}