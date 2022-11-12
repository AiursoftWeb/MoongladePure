using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Data.Spec;

public class PostSitePageSpec : BaseSpecification<PostEntity>
{
    public PostSitePageSpec() : base(p =>
        p.IsPublished && !p.IsDeleted)
    {

    }
}