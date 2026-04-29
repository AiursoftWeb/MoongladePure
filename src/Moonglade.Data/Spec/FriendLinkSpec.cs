using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Data.Spec;

public class FriendLinkSpec(Guid id) : BaseSpecification<FriendLinkEntity>(f => f.SiteId == SystemIds.DefaultSiteId && f.Id == id);
