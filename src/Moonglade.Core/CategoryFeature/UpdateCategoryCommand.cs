using MoongladePure.Caching;
using MoongladePure.Data;

namespace MoongladePure.Core.CategoryFeature;

public class UpdateCategoryCommand : CreateCategoryCommand, IRequest<OperationCode>
{
    public Guid Id { get; set; }
}

public class UpdateCategoryCommandHandler(IRepository<CategoryEntity> repo, IBlogCache cache, ISiteContext siteContext)
    : IRequestHandler<UpdateCategoryCommand, OperationCode>
{
    public async Task<OperationCode> Handle(UpdateCategoryCommand request, CancellationToken ct)
    {
        var cat = await repo.GetAsync(c => c.SiteId == siteContext.SiteId && c.Id == request.Id);
        if (cat is null) return OperationCode.ObjectNotFound;

        cat.RouteName = request.RouteName.Trim();
        cat.DisplayName = request.DisplayName.Trim();
        cat.Note = request.Note?.Trim();

        await repo.UpdateAsync(cat, ct);
        cache.Remove(CacheDivision.General, $"{siteContext.SiteId}:allcats");

        return OperationCode.Done;
    }
}
