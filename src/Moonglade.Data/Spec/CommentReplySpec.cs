using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Data.Spec;

public class CommentReplySpec : BaseSpecification<CommentReplyEntity>
{
    public CommentReplySpec(Guid commentId) : base(cr => cr.CommentId == commentId)
    {

    }
}