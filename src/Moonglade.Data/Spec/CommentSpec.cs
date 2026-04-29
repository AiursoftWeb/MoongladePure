using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Data.Spec;

public sealed class CommentSpec : BaseSpecification<CommentEntity>
{
    public CommentSpec(int pageSize, int pageIndex) : base(c => c.SiteId == SystemIds.DefaultSiteId)
    {
        var startRow = (pageIndex - 1) * pageSize;

        AddInclude(comment => comment
            .Include(c => c.Post)
            .Include(c => c.Replies));
        ApplyOrderByDescending(p => p.CreateTimeUtc);
        ApplyPaging(startRow, pageSize);
    }

    public CommentSpec(Guid[] ids) : base(c => c.SiteId == SystemIds.DefaultSiteId && EF.Constant(ids).Contains(c.Id))
    {

    }

    public CommentSpec(Guid postId) : base(c => c.SiteId == SystemIds.DefaultSiteId &&
                                                c.PostId == postId &&
                                                c.IsApproved)
    {
        AddInclude(comments => comments.Include(c => c.Replies));
    }
}
