using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Data.Spec;

public sealed class FeaturedPostSpec : BaseSpecification<PostEntity>
{
    public FeaturedPostSpec(Guid? siteId = null) : base(p => p.SiteId == (siteId ?? SystemIds.DefaultSiteId) && p.IsFeatured)
    {
    }

    public FeaturedPostSpec(int pageSize, int pageIndex, Guid? siteId = null)
        : base(p =>
            p.IsFeatured
            && p.SiteId == (siteId ?? SystemIds.DefaultSiteId)
            && !p.IsDeleted
            && p.IsPublished)
    {
        var startRow = (pageIndex - 1) * pageSize;
        ApplyPaging(startRow, pageSize);
        ApplyOrderByDescending(p => p.PubDateUtc);
    }
}
