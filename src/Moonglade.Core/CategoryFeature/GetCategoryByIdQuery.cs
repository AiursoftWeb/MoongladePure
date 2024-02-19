using MoongladePure.Data.Spec;

namespace MoongladePure.Core.CategoryFeature;

public record GetCategoryByIdQuery(Guid Id) : IRequest<Category>;

public class GetCategoryByIdQueryHandler(IRepository<CategoryEntity> repo)
    : IRequestHandler<GetCategoryByIdQuery, Category>
{
    public Task<Category> Handle(GetCategoryByIdQuery request, CancellationToken ct) =>
        repo.FirstOrDefaultAsync(new CategorySpec(request.Id), Category.EntitySelector);
}