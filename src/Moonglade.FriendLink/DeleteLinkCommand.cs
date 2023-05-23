using MediatR;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.FriendLink;

public record DeleteLinkCommand(Guid Id) : IRequest;

public class DeleteLinkCommandHandler : IRequestHandler<DeleteLinkCommand>
{
    private readonly IRepository<FriendLinkEntity> _repo;

    public DeleteLinkCommandHandler(IRepository<FriendLinkEntity> repo) => _repo = repo;

    public Task Handle(DeleteLinkCommand request, CancellationToken ct) =>
        _repo.DeleteAsync(request.Id, ct);
}