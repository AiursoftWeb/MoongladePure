using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Data.Spec;

public class CommentReplySpec(Guid commentId, Guid? siteId = null)
    : BaseSpecification<CommentReplyEntity>(cr => cr.SiteId == (siteId ?? SystemIds.DefaultSiteId) && cr.CommentId == commentId);
