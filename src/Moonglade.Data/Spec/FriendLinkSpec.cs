using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Data.Spec;

public class FriendLinkSpec(Guid id, Guid? siteId = null)
    : BaseSpecification<FriendLinkEntity>(f => f.SiteId == (siteId ?? SystemIds.DefaultSiteId) && f.Id == id);
