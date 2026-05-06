using MediatR;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Menus;

public record DeleteMenuCommand(Guid Id) : IRequest;

public class DeleteMenuCommandHandler(IRepository<MenuEntity> repo, ISiteContext siteContext) : IRequestHandler<DeleteMenuCommand>
{
    public async Task Handle(DeleteMenuCommand request, CancellationToken ct)
    {
        var menu = await repo.GetAsync(m => m.SiteId == siteContext.SiteId && m.Id == request.Id);
        if (menu != null) await repo.DeleteAsync(menu, ct);
    }
}
