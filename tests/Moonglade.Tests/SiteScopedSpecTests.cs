using MoongladePure.Data.Entities;
using MoongladePure.Data.Spec;

namespace MoongladePure.Tests;

[TestClass]
public class SiteScopedSpecTests
{
    private static readonly Guid OtherSiteId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    [TestMethod]
    public void PostSpecsUseDefaultSiteBoundary()
    {
        var post = new PostEntity
        {
            SiteId = SystemIds.DefaultSiteId,
            IsPublished = true,
            IsDeleted = false,
            IsFeedIncluded = true,
            PubDateUtc = DateTime.UtcNow
        };
        var otherSitePost = new PostEntity
        {
            SiteId = OtherSiteId,
            IsPublished = true,
            IsDeleted = false,
            IsFeedIncluded = true,
            PubDateUtc = DateTime.UtcNow
        };
        var criteria = new PostSpec(PostStatus.Published).Criteria.Compile();

        Assert.IsTrue(criteria(post));
        Assert.IsFalse(criteria(otherSitePost));
    }

    [TestMethod]
    public void CategoryAndTagSpecsUseDefaultSiteBoundary()
    {
        var categoryCriteria = new CategorySpec("default").Criteria.Compile();
        var tagCriteria = new TagSpec("test").Criteria.Compile();

        Assert.IsTrue(categoryCriteria(new CategoryEntity { SiteId = SystemIds.DefaultSiteId, RouteName = "default" }));
        Assert.IsFalse(categoryCriteria(new CategoryEntity { SiteId = OtherSiteId, RouteName = "default" }));
        Assert.IsTrue(tagCriteria(new TagEntity { SiteId = SystemIds.DefaultSiteId, NormalizedName = "test" }));
        Assert.IsFalse(tagCriteria(new TagEntity { SiteId = OtherSiteId, NormalizedName = "test" }));
    }

    [TestMethod]
    public void PageAndCommentSpecsUseDefaultSiteBoundary()
    {
        var postId = Guid.NewGuid();
        var pageCriteria = new PageSpec(10).Criteria.Compile();
        var commentCriteria = new CommentSpec(postId).Criteria.Compile();

        Assert.IsTrue(pageCriteria(new PageEntity { SiteId = SystemIds.DefaultSiteId, IsPublished = true }));
        Assert.IsFalse(pageCriteria(new PageEntity { SiteId = OtherSiteId, IsPublished = true }));
        Assert.IsTrue(commentCriteria(new CommentEntity { SiteId = SystemIds.DefaultSiteId, PostId = postId, IsApproved = true }));
        Assert.IsFalse(commentCriteria(new CommentEntity { SiteId = OtherSiteId, PostId = postId, IsApproved = true }));
    }
}
