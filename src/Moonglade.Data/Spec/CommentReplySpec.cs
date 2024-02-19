using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Data.Spec;

public class CommentReplySpec(Guid commentId) : BaseSpecification<CommentReplyEntity>(cr => cr.CommentId == commentId);