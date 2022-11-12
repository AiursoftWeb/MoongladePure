using MediatR;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Configuration;

public record GetAllConfigurationsQuery : IRequest<IDictionary<string, string>>;

public class GetAllConfigurationsQueryHandler : IRequestHandler<GetAllConfigurationsQuery, IDictionary<string, string>>
{
    private readonly IRepository<BlogConfigurationEntity> _repo;

    public GetAllConfigurationsQueryHandler(IRepository<BlogConfigurationEntity> repo) => _repo = repo;

    public async Task<IDictionary<string, string>> Handle(GetAllConfigurationsQuery request, CancellationToken ct)
    {
        var entities = await _repo.SelectAsync(p => new { p.CfgKey, p.CfgValue }, ct);
        return entities.ToDictionary(k => k.CfgKey, v => v.CfgValue);
    }
}