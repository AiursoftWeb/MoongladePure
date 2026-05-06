using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Data.Spec;

public class PostSitePageSpec(Guid? siteId = null) : BaseSpecification<PostEntity>(p =>
    p.SiteId == (siteId ?? SystemIds.DefaultSiteId) && p.IsPublished && !p.IsDeleted);
