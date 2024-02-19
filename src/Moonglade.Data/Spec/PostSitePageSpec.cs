using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Data.Spec;

public class PostSitePageSpec() : BaseSpecification<PostEntity>(p =>
    p.IsPublished && !p.IsDeleted);