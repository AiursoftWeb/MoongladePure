using MediatR;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.FriendLink;

public record GetAllLinksQuery : IRequest<IReadOnlyList<Link>>;

public class GetAllLinksQueryHandler(IRepository<FriendLinkEntity> repo)
    : IRequestHandler<GetAllLinksQuery, IReadOnlyList<Link>>
{
    public Task<IReadOnlyList<Link>> Handle(GetAllLinksQuery request, CancellationToken ct)
    {
        return repo.SelectAsync(f => new Link
        {
            Id = f.Id,
            LinkUrl = f.LinkUrl,
            Title = f.Title
        }, ct);
    }
}