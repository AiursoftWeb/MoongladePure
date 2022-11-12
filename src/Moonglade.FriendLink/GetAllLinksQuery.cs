using MediatR;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.FriendLink;

public record GetAllLinksQuery : IRequest<IReadOnlyList<Link>>;

public class GetAllLinksQueryHandler : IRequestHandler<GetAllLinksQuery, IReadOnlyList<Link>>
{
    private readonly IRepository<FriendLinkEntity> _repo;

    public GetAllLinksQueryHandler(IRepository<FriendLinkEntity> repo) => _repo = repo;

    public Task<IReadOnlyList<Link>> Handle(GetAllLinksQuery request, CancellationToken ct)
    {
        return _repo.SelectAsync(f => new Link
        {
            Id = f.Id,
            LinkUrl = f.LinkUrl,
            Title = f.Title
        }, ct);
    }
}