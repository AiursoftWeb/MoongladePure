using MediatR;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Menus;

public record DeleteMenuCommand(Guid Id) : IRequest;

public class DeleteMenuCommandHandler(IRepository<MenuEntity> repo) : IRequestHandler<DeleteMenuCommand>
{
    public async Task Handle(DeleteMenuCommand request, CancellationToken ct)
    {
        var menu = await repo.GetAsync(request.Id, ct);
        if (menu != null) await repo.DeleteAsync(request.Id, ct);
    }
}