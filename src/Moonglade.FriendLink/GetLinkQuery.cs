using MediatR;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;
using MoongladePure.Data.Spec;

namespace MoongladePure.FriendLink;

public record GetLinkQuery(Guid Id) : IRequest<Link>;

public class GetLinkQueryHandler : IRequestHandler<GetLinkQuery, Link>
{
    private readonly IRepository<FriendLinkEntity> _repo;

    public GetLinkQueryHandler(IRepository<FriendLinkEntity> repo) => _repo = repo;

    public Task<Link> Handle(GetLinkQuery request, CancellationToken ct)
    {
        return _repo.FirstOrDefaultAsync(
             new FriendLinkSpec(request.Id), f => new Link
             {
                 Id = f.Id,
                 LinkUrl = f.LinkUrl,
                 Title = f.Title
             });
    }
}