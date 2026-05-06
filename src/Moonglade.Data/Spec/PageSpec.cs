using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Data.Spec;

public sealed class PageSpec : BaseSpecification<PageEntity>
{
    public PageSpec(int top, Guid? siteId = null)
        : base(p => p.SiteId == (siteId ?? SystemIds.DefaultSiteId) && p.IsPublished)
    {
        ApplyOrderByDescending(p => p.CreateTimeUtc);
        ApplyPaging(0, top);
    }
}
