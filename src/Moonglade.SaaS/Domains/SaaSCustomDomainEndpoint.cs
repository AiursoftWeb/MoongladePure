using Microsoft.AspNetCore.Http;

namespace MoongladePure.SaaS.Domains;

public sealed class SaaSCustomDomainEndpoint(SaaSCustomDomainService domainService)
{
    public async Task<IResult> ListAsync(Guid siteId, CancellationToken ct = default) =>
        Results.Ok(await domainService.ListAsync(siteId, ct));

    public async Task<IResult> AddAsync(
        Guid siteId,
        SaaSCustomDomainRequest request,
        CancellationToken ct = default)
    {
        var result = await domainService.AddPendingAsync(siteId, request, ct);
        return result.Succeeded
            ? Results.Created($"/api/sites/{siteId}/domains/{result.Domain.Id}", result.Domain)
            : Results.BadRequest(result);
    }

    public async Task<IResult> DeleteAsync(Guid siteId, Guid domainId, CancellationToken ct = default)
    {
        var result = await domainService.DeleteAsync(siteId, domainId, ct);
        return result.Succeeded
            ? Results.Ok(result.Domain)
            : Results.BadRequest(result);
    }
}
