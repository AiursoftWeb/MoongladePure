using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Data.Spec;

public sealed class PostTagSpec : BaseSpecification<PostTagEntity>
{
    public PostTagSpec(int tagId, Guid? siteId = null)
        : base(pt => pt.SiteId == (siteId ?? SystemIds.DefaultSiteId) && pt.TagId == tagId)
    {
    }

    public PostTagSpec(int tagId, int pageSize, int pageIndex, Guid? siteId = null)
        : base(pt =>
            pt.TagId == tagId
            && pt.SiteId == (siteId ?? SystemIds.DefaultSiteId)
            && !pt.Post.IsDeleted
            && pt.Post.IsPublished)
    {
        var startRow = (pageIndex - 1) * pageSize;
        ApplyPaging(startRow, pageSize);
        ApplyOrderByDescending(p => p.Post.PubDateUtc);
    }
}
