using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using MoongladePure.Core.PageFeature;
using MoongladePure.Core.PostFeature;
using MoongladePure.Data;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;
using MoongladePure.Data.InMemory;
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

    [TestMethod]
    public async Task ListPostsQueryUsesCurrentSiteBoundary()
    {
        await using var context = CreateContext();
        await context.Post.AddRangeAsync(
            CreatePost(SystemIds.DefaultSiteId, "Default Site Post"),
            CreatePost(OtherSiteId, "Other Site Post"));
        await context.SaveChangesAsync();
        var repo = new BlogDbContextRepository<PostEntity>(context);
        var handler = new ListPostsQueryHandler(repo, new FixedSiteContext(OtherSiteId));

        var posts = await handler.Handle(new ListPostsQuery(10, 1), CancellationToken.None);

        Assert.AreEqual(1, posts.Count);
        Assert.AreEqual("Other Site Post", posts[0].Title);
    }

    [TestMethod]
    public async Task GetPageBySlugQueryUsesCurrentSiteBoundary()
    {
        await using var context = CreateContext();
        await context.CustomPage.AddRangeAsync(
            CreatePage(SystemIds.DefaultSiteId, "about", "Default About"),
            CreatePage(OtherSiteId, "about", "Other About"));
        await context.SaveChangesAsync();
        var repo = new BlogDbContextRepository<PageEntity>(context);
        var handler = new GetPageBySlugQueryHandler(repo, new FixedSiteContext(OtherSiteId));

        var page = await handler.Handle(new GetPageBySlugQuery("about"), CancellationToken.None);

        Assert.IsNotNull(page);
        Assert.AreEqual("Other About", page.Title);
    }

    [TestMethod]
    public async Task RequestSiteContextUsesBoundDomain()
    {
        await using var context = CreateContext();
        await context.SiteDomain.AddAsync(new SiteDomainEntity
        {
            SiteId = OtherSiteId,
            Host = "blog.example.com"
        });
        await context.SaveChangesAsync();
        var siteContext = new RequestSiteContext(CreateHttpContextAccessor("blog.example.com"), context);

        Assert.AreEqual(OtherSiteId, siteContext.SiteId);
    }

    [TestMethod]
    public async Task RequestSiteContextFallsBackForUnknownDomain()
    {
        await using var context = CreateContext();
        await context.SiteDomain.AddAsync(new SiteDomainEntity
        {
            SiteId = OtherSiteId,
            Host = "blog.example.com"
        });
        await context.SaveChangesAsync();
        var siteContext = new RequestSiteContext(CreateHttpContextAccessor("unknown.example.com"), context);

        Assert.AreEqual(SystemIds.DefaultSiteId, siteContext.SiteId);
    }

    [TestMethod]
    public async Task RequestSiteContextNormalizesHostCaseAndPort()
    {
        await using var context = CreateContext();
        await context.SiteDomain.AddAsync(new SiteDomainEntity
        {
            SiteId = OtherSiteId,
            Host = "blog.example.com"
        });
        await context.SaveChangesAsync();
        var siteContext = new RequestSiteContext(CreateHttpContextAccessor("BLOG.EXAMPLE.COM:8443"), context);

        Assert.AreEqual(OtherSiteId, siteContext.SiteId);
    }

    private static InMemoryContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InMemoryContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new InMemoryContext(options);
    }

    private static PostEntity CreatePost(Guid siteId, string title) => new()
    {
        Id = Guid.NewGuid(),
        SiteId = siteId,
        Title = title,
        Slug = title.ToLowerInvariant().Replace(' ', '-'),
        RawContent = "content",
        IsPublished = true,
        IsDeleted = false,
        IsFeedIncluded = true,
        PubDateUtc = DateTime.UtcNow,
        ContentLanguageCode = "en-US"
    };

    private static PageEntity CreatePage(Guid siteId, string slug, string title) => new()
    {
        Id = Guid.NewGuid(),
        SiteId = siteId,
        Title = title,
        Slug = slug,
        HtmlContent = "content",
        CreateTimeUtc = DateTime.UtcNow,
        IsPublished = true
    };

    private static IHttpContextAccessor CreateHttpContextAccessor(string host)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = HostString.FromUriComponent(host);

        return new HttpContextAccessor { HttpContext = httpContext };
    }

    private sealed class FixedSiteContext(Guid siteId) : ISiteContext
    {
        public Guid SiteId { get; } = siteId;
    }
}
