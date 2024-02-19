using MoongladePure.Caching;

namespace MoongladePure.Core.CategoryFeature;

public record GetCategoriesQuery : IRequest<IReadOnlyList<Category>>;

public class GetCategoriesQueryHandler(IRepository<CategoryEntity> repo, IBlogCache cache)
    : IRequestHandler<GetCategoriesQuery, IReadOnlyList<Category>>
{
    public Task<IReadOnlyList<Category>> Handle(GetCategoriesQuery request, CancellationToken ct)
    {
        return cache.GetOrCreateAsync(CacheDivision.General, "allcats", async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromHours(1);
            var list = await repo.SelectAsync(Category.EntitySelector, ct);
            return list;
        });
    }
}