using MediatR;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;
using MoongladePure.Utils;

namespace MoongladePure.Menus;

public record UpdateMenuCommand(EditMenuRequest Payload) : IRequest;

public class UpdateMenuCommandHandler(IRepository<MenuEntity> repo) : IRequestHandler<UpdateMenuCommand>
{
    public async Task Handle(UpdateMenuCommand request, CancellationToken ct)
    {
        var menu = await repo.GetAsync(request.Payload.Id, ct);
        if (menu is null)
        {
            throw new InvalidOperationException($"MenuEntity with Id '{request.Payload.Id}' not found.");
        }

        var url = Helper.SterilizeLink(request.Payload.Url.Trim());

        menu.Title = request.Payload.Title.Trim();
        menu.Url = url;
        menu.DisplayOrder = request.Payload.DisplayOrder.GetValueOrDefault();
        menu.Icon = request.Payload.Icon;
        menu.IsOpenInNewTab = request.Payload.IsOpenInNewTab;

        if (request.Payload.SubMenus != null)
        {
            menu.SubMenus.Clear();
            var sms = request.Payload.SubMenus.Select(p => new SubMenuEntity
            {
                Id = Guid.NewGuid(),
                IsOpenInNewTab = p.IsOpenInNewTab,
                Title = p.Title,
                Url = p.Url,
                MenuId = menu.Id
            });

            menu.SubMenus = sms.ToList();
        }

        await repo.UpdateAsync(menu, ct);
    }
}