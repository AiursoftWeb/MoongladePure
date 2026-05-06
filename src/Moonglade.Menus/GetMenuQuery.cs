using MediatR;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Menus;

public record GetMenuQuery(Guid Id) : IRequest<Menu>;

public class GetMenuQueryHandler(IRepository<MenuEntity> repo, ISiteContext siteContext) : IRequestHandler<GetMenuQuery, Menu>
{
    public async Task<Menu> Handle(GetMenuQuery request, CancellationToken ct)
    {
        var entity = await repo.GetAsync(m => m.SiteId == siteContext.SiteId && m.Id == request.Id);
        if (null == entity) return null;

        var item = new Menu(entity);
        return item;
    }
}
