namespace MoongladePure.Core.PageFeature;

public record GetPageByIdQuery(Guid Id) : IRequest<BlogPage>;

public class GetPageByIdQueryHandler(IRepository<PageEntity> repo, ISiteContext siteContext) : IRequestHandler<GetPageByIdQuery, BlogPage>
{
    public async Task<BlogPage> Handle(GetPageByIdQuery request, CancellationToken ct)
    {
        var entity = await repo.GetAsync(p => p.SiteId == siteContext.SiteId && p.Id == request.Id);
        if (entity == null) return null;

        var item = new BlogPage(entity);
        return item;
    }
}
