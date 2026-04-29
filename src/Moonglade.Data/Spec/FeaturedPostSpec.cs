using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Data.Spec;

public sealed class FeaturedPostSpec : BaseSpecification<PostEntity>
{
    public FeaturedPostSpec() : base(p => p.SiteId == SystemIds.DefaultSiteId && p.IsFeatured)
    {
    }

    public FeaturedPostSpec(int pageSize, int pageIndex)
        : base(p =>
            p.IsFeatured
            && p.SiteId == SystemIds.DefaultSiteId
            && !p.IsDeleted
            && p.IsPublished)
    {
        var startRow = (pageIndex - 1) * pageSize;
        ApplyPaging(startRow, pageSize);
        ApplyOrderByDescending(p => p.PubDateUtc);
    }
}
