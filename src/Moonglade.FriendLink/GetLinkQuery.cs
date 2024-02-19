using MediatR;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;
using MoongladePure.Data.Spec;

namespace MoongladePure.FriendLink;

public record GetLinkQuery(Guid Id) : IRequest<Link>;

public class GetLinkQueryHandler(IRepository<FriendLinkEntity> repo) : IRequestHandler<GetLinkQuery, Link>
{
    public Task<Link> Handle(GetLinkQuery request, CancellationToken ct)
    {
        return repo.FirstOrDefaultAsync(
             new FriendLinkSpec(request.Id), f => new Link
             {
                 Id = f.Id,
                 LinkUrl = f.LinkUrl,
                 Title = f.Title
             });
    }
}