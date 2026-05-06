using MediatR;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.FriendLink;

public record DeleteLinkCommand(Guid Id) : IRequest;

public class DeleteLinkCommandHandler(IRepository<FriendLinkEntity> repo, ISiteContext siteContext) : IRequestHandler<DeleteLinkCommand>
{
    public async Task Handle(DeleteLinkCommand request, CancellationToken ct)
    {
        var link = await repo.GetAsync(l => l.SiteId == siteContext.SiteId && l.Id == request.Id);
        if (link is not null) await repo.DeleteAsync(link, ct);
    }
}
