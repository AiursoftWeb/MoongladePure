using MediatR;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Menus;

public record GetAllMenusQuery : IRequest<IReadOnlyList<Menu>>;

public class GetAllMenusQueryHandler(IRepository<MenuEntity> repo)
    : IRequestHandler<GetAllMenusQuery, IReadOnlyList<Menu>>
{
    public Task<IReadOnlyList<Menu>> Handle(GetAllMenusQuery request, CancellationToken ct)
    {
        var list = repo.SelectAsync(p => new Menu
        {
            Id = p.Id,
            DisplayOrder = p.DisplayOrder,
            Icon = p.Icon,
            Title = p.Title,
            Url = p.Url,
            IsOpenInNewTab = p.IsOpenInNewTab,
            SubMenus = p.SubMenus.Select(sm => new SubMenu
            {
                Id = sm.Id,
                Title = sm.Title,
                Url = sm.Url,
                IsOpenInNewTab = sm.IsOpenInNewTab
            }).ToList()
        }, ct);

        return list;
    }
}