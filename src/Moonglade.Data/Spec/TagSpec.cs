using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Data.Spec;

public sealed class TagSpec : BaseSpecification<TagEntity>
{
    public TagSpec(int top, Guid? siteId = null) : base(t => t.SiteId == (siteId ?? SystemIds.DefaultSiteId))
    {
        ApplyPaging(0, top);
        ApplyOrderByDescending(p => p.Posts.Count);
    }

    public TagSpec(string normalizedName, Guid? siteId = null)
        : base(t => t.SiteId == (siteId ?? SystemIds.DefaultSiteId) && t.NormalizedName == normalizedName)
    {

    }
}
