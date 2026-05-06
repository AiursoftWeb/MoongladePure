using MediatR;
using Microsoft.EntityFrameworkCore;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Configuration;

public record GetAllConfigurationsQuery : IRequest<IDictionary<string, string>>;

public class GetAllConfigurationsQueryHandler(IRepository<BlogConfigurationEntity> repo, ISiteContext siteContext)
    : IRequestHandler<GetAllConfigurationsQuery, IDictionary<string, string>>
{
    public async Task<IDictionary<string, string>> Handle(GetAllConfigurationsQuery request, CancellationToken ct)
    {
        var entities = await repo.AsQueryable()
            .Where(p => p.SiteId == siteContext.SiteId)
            .Select(p => new { p.CfgKey, p.CfgValue })
            .ToListAsync(ct);
        return entities.ToDictionary(k => k.CfgKey, v => v.CfgValue);
    }
}
