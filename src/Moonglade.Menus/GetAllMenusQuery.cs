using MediatR;
using Microsoft.EntityFrameworkCore;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Menus;

public record GetAllMenusQuery : IRequest<IReadOnlyList<Menu>>;

public class GetAllMenusQueryHandler(IRepository<MenuEntity> repo)
    : IRequestHandler<GetAllMenusQuery, IReadOnlyList<Menu>>
{
    public async Task<IReadOnlyList<Menu>> Handle(GetAllMenusQuery request, CancellationToken ct)
    {
        var list = await repo.AsQueryable()
            .Where(p => p.SiteId == SystemIds.DefaultSiteId)
            .Select(p => new Menu
            {
                Id = p.Id,
                DisplayOrder = p.DisplayOrder,
                Icon = p.Icon,
                Title = p.Title,
                Url = p.Url,
                IsOpenInNewTab = p.IsOpenInNewTab,
                SubMenus = p.SubMenus
                    .Where(sm => sm.SiteId == SystemIds.DefaultSiteId)
                    .Select(sm => new SubMenu
                    {
                        Id = sm.Id,
                        Title = sm.Title,
                        Url = sm.Url,
                        IsOpenInNewTab = sm.IsOpenInNewTab
                    }).ToList()
            })
            .ToListAsync(ct);

        return list;
    }
}
