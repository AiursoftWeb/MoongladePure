using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Data.Spec;

public class CategorySpec : BaseSpecification<CategoryEntity>
{
    public CategorySpec(string routeName, Guid? siteId = null)
        : base(c => c.SiteId == (siteId ?? SystemIds.DefaultSiteId) && c.RouteName == routeName)
    {

    }

    public CategorySpec(Guid id, Guid? siteId = null)
        : base(c => c.SiteId == (siteId ?? SystemIds.DefaultSiteId) && c.Id == id)
    {

    }
}
