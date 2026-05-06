using MediatR;
using Microsoft.EntityFrameworkCore;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.FriendLink;

public record GetAllLinksQuery : IRequest<IReadOnlyList<Link>>;

public class GetAllLinksQueryHandler(IRepository<FriendLinkEntity> repo, ISiteContext siteContext)
    : IRequestHandler<GetAllLinksQuery, IReadOnlyList<Link>>
{
    public async Task<IReadOnlyList<Link>> Handle(GetAllLinksQuery request, CancellationToken ct)
    {
        return await repo.AsQueryable()
            .Where(f => f.SiteId == siteContext.SiteId)
            .Select(f => new Link
            {
                Id = f.Id,
                LinkUrl = f.LinkUrl,
                Title = f.Title
            })
            .ToListAsync(ct);
    }
}
