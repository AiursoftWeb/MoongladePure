using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Auth;

public record CountAccountsQuery : IRequest<int>;

public class CountAccountsQueryHandler(IRepository<LocalAccountEntity> repo) : IRequestHandler<CountAccountsQuery, int>
{
    public Task<int> Handle(CountAccountsQuery request, CancellationToken ct) => repo.CountAsync(ct: ct);
}