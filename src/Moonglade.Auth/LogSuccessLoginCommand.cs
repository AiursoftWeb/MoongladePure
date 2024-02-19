using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Auth;

public record LogSuccessLoginCommand(Guid Id, string IpAddress) : IRequest;

public class LogSuccessLoginCommandHandler(IRepository<LocalAccountEntity> repo)
    : IRequestHandler<LogSuccessLoginCommand>
{
    public async Task Handle(LogSuccessLoginCommand request, CancellationToken ct)
    {
        var (id, ipAddress) = request;

        var entity = await repo.GetAsync(id, ct);
        if (entity is not null)
        {
            entity.LastLoginIp = ipAddress.Trim();
            entity.LastLoginTimeUtc = DateTime.UtcNow;
            await repo.UpdateAsync(entity, ct);
        }
    }
}