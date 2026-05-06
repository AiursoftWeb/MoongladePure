using Microsoft.EntityFrameworkCore;
using MoongladePure.Caching;

namespace MoongladePure.Core.CategoryFeature;

public record GetCategoriesQuery : IRequest<IReadOnlyList<Category>>;

public class GetCategoriesQueryHandler(IRepository<CategoryEntity> repo, IBlogCache cache, ISiteContext siteContext)
    : IRequestHandler<GetCategoriesQuery, IReadOnlyList<Category>>
{
    public Task<IReadOnlyList<Category>> Handle(GetCategoriesQuery request, CancellationToken ct)
    {
        return cache.GetOrCreateAsync(CacheDivision.General, $"{siteContext.SiteId}:allcats", async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromHours(1);
            var list = await repo.AsQueryable()
                .Where(c => c.SiteId == siteContext.SiteId)
                .Select(Category.EntitySelector)
                .ToListAsync(ct);
            return (IReadOnlyList<Category>)list;
        });
    }
}
