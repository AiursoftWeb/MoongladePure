using MediatR;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.FriendLink;

public record DeleteLinkCommand(Guid Id) : IRequest;

public class DeleteLinkCommandHandler(IRepository<FriendLinkEntity> repo) : IRequestHandler<DeleteLinkCommand>
{
    public Task Handle(DeleteLinkCommand request, CancellationToken ct) =>
        repo.DeleteAsync(request.Id, ct);
}