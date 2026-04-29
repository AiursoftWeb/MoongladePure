using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Data.Spec;

public class CategorySpec : BaseSpecification<CategoryEntity>
{
    public CategorySpec(string routeName) : base(c => c.SiteId == SystemIds.DefaultSiteId && c.RouteName == routeName)
    {

    }

    public CategorySpec(Guid id) : base(c => c.SiteId == SystemIds.DefaultSiteId && c.Id == id)
    {

    }
}
