using MoongladePure.Caching;
using MoongladePure.Data;

namespace MoongladePure.Core.CategoryFeature;

public record DeleteCategoryCommand(Guid Id) : IRequest<OperationCode>;

public class DeleteCategoryCommandHandler(
    IRepository<CategoryEntity> catRepo,
    IRepository<PostCategoryEntity> postCatRepo,
    IBlogCache cache,
    ISiteContext siteContext)
    : IRequestHandler<DeleteCategoryCommand, OperationCode>
{
    public async Task<OperationCode> Handle(DeleteCategoryCommand request, CancellationToken ct)
    {
        var exists = await catRepo.AnyAsync(c => c.SiteId == siteContext.SiteId && c.Id == request.Id, ct);
        if (!exists) return OperationCode.ObjectNotFound;

        var pcs = await postCatRepo.GetAsync(pc => pc.SiteId == siteContext.SiteId && pc.CategoryId == request.Id);
        if (pcs is not null) await postCatRepo.DeleteAsync(pcs, ct);

        await catRepo.DeleteAsync(request.Id, ct);
        cache.Remove(CacheDivision.General, $"{siteContext.SiteId}:allcats");

        return OperationCode.Done;
    }
}
