namespace MoongladePure.Core;

public record GetAssetQuery(Guid AssetId) : IRequest<string>;

public class GetAssetQueryHandler(IRepository<BlogAssetEntity> repo, ISiteContext siteContext) : IRequestHandler<GetAssetQuery, string>
{
    public async Task<string> Handle(GetAssetQuery request, CancellationToken ct)
    {
        var asset = await repo.GetAsync(a => a.SiteId == siteContext.SiteId && a.Id == request.AssetId);
        return asset?.Base64Data;
    }
}
