using MoongladePure.Data.Spec;

namespace MoongladePure.Core.TagFeature;

public record GetTagQuery(string NormalizedName) : IRequest<Tag>;

public class GetTagQueryHandler(IRepository<TagEntity> repo, ISiteContext siteContext) : IRequestHandler<GetTagQuery, Tag>
{
    public Task<Tag> Handle(GetTagQuery request, CancellationToken ct) => 
        repo.FirstOrDefaultAsync(new TagSpec(request.NormalizedName, siteContext.SiteId), Tag.EntitySelector);
}
