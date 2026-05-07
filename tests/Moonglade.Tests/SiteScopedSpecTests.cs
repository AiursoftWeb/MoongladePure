using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using MediatR;
using MoongladePure.Caching;
using MoongladePure.Caching.Filters;
using MoongladePure.Configuration;
using MoongladePure.Core;
using MoongladePure.Core.CategoryFeature;
using MoongladePure.Core.PageFeature;
using MoongladePure.Core.PostFeature;
using MoongladePure.Core.TagFeature;
using MoongladePure.Data;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;
using MoongladePure.Data.InMemory;
using MoongladePure.Data.Spec;
using MoongladePure.Menus;
using MoongladePure.Syndication;
using MoongladePure.Theme;
using MoongladePure.Web.Pages;
using MoongladePure.Web.ViewComponents;
using MoongladePure.Web;
using MoongladePure.Web.Controllers;
using MoongladePure.Web.Middleware;

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
        var handler = new ListPostsQueryHandler(
            repo,
            new BlogDbContextRepository<PostContentEntity>(context),
            new BlogDbContextRepository<AiArtifactEntity>(context),
            new FixedSiteContext(OtherSiteId));

        var posts = await handler.Handle(new ListPostsQuery(10, 1), CancellationToken.None);

        Assert.AreEqual(1, posts.Count);
        Assert.AreEqual("Other Site Post", posts[0].Title);
    }

    [TestMethod]
    public async Task ListPostsQueryUsesAiArtifactSummaryBeforeLegacyFields()
    {
        await using var context = CreateContext();
        var post = CreatePost(SystemIds.DefaultSiteId, "Projection Post");
        post.ContentAbstractZh = "legacy zh";
        post.ContentAbstractEn = "legacy en";
        await context.Post.AddAsync(post);
        await context.AiArtifact.AddRangeAsync(
            CreateArtifact(post.Id, AiArtifactType.Summary, "zh-CN", "artifact zh"),
            CreateArtifact(post.Id, AiArtifactType.Summary, "en-US", "artifact en"));
        await context.SaveChangesAsync();
        var handler = new ListPostsQueryHandler(
            new BlogDbContextRepository<PostEntity>(context),
            new BlogDbContextRepository<PostContentEntity>(context),
            new BlogDbContextRepository<AiArtifactEntity>(context),
            new FixedSiteContext(SystemIds.DefaultSiteId));

        var posts = await handler.Handle(new ListPostsQuery(10, 1), CancellationToken.None);

        Assert.AreEqual("artifact zh", posts[0].ContentAbstractZh);
        Assert.AreEqual("artifact en", posts[0].ContentAbstractEn);
    }

    [TestMethod]
    public async Task GetPostByIdQueryUsesPostContentAndAiArtifactsBeforeLegacyFields()
    {
        await using var context = CreateContext();
        var post = CreatePost(SystemIds.DefaultSiteId, "Detail Projection Post");
        post.RawContent = "legacy raw";
        post.ContentAbstractZh = "legacy zh";
        post.ContentAbstractEn = "legacy en";
        post.LocalizedChineseContent = "legacy translated zh";
        post.LocalizedEnglishContent = "legacy translated en";
        await context.Post.AddAsync(post);
        await context.PostContent.AddRangeAsync(
            CreatePostContent(post.Id, PostContentKind.RawMarkdown, "en-US", "postcontent raw"),
            CreatePostContent(post.Id, PostContentKind.Translation, "zh-CN", "postcontent translated zh"));
        await context.AiArtifact.AddRangeAsync(
            CreateArtifact(post.Id, AiArtifactType.Summary, "zh-CN", "artifact zh"),
            CreateArtifact(post.Id, AiArtifactType.Summary, "en-US", "artifact en"),
            CreateArtifact(post.Id, AiArtifactType.Translation, "en-US", "artifact translated en"));
        await context.SaveChangesAsync();
        var handler = new GetPostByIdQueryHandler(
            new BlogDbContextRepository<PostEntity>(context),
            new BlogDbContextRepository<PostContentEntity>(context),
            new BlogDbContextRepository<AiArtifactEntity>(context),
            new FixedSiteContext(SystemIds.DefaultSiteId));

        var projectedPost = await handler.Handle(new GetPostByIdQuery(post.Id), CancellationToken.None);

        Assert.AreEqual("postcontent raw", projectedPost.RawPostContent);
        Assert.AreEqual("artifact zh", projectedPost.ContentAbstractZh);
        Assert.AreEqual("artifact en", projectedPost.ContentAbstractEn);
        Assert.AreEqual("postcontent translated zh", projectedPost.LocalizedChineseContent);
        Assert.AreEqual("artifact translated en", projectedPost.LocalizedEnglishContent);
    }

    [TestMethod]
    public async Task GetPostByIdQueryFallsBackToLegacyFields()
    {
        await using var context = CreateContext();
        var post = CreatePost(SystemIds.DefaultSiteId, "Legacy Projection Post");
        post.RawContent = "legacy raw";
        post.ContentAbstractZh = "legacy zh";
        post.ContentAbstractEn = "legacy en";
        await context.Post.AddAsync(post);
        await context.SaveChangesAsync();
        var handler = new GetPostByIdQueryHandler(
            new BlogDbContextRepository<PostEntity>(context),
            new BlogDbContextRepository<PostContentEntity>(context),
            new BlogDbContextRepository<AiArtifactEntity>(context),
            new FixedSiteContext(SystemIds.DefaultSiteId));

        var projectedPost = await handler.Handle(new GetPostByIdQuery(post.Id), CancellationToken.None);

        Assert.AreEqual("legacy raw", projectedPost.RawPostContent);
        Assert.AreEqual("legacy zh", projectedPost.ContentAbstractZh);
        Assert.AreEqual("legacy en", projectedPost.ContentAbstractEn);
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

    [TestMethod]
    public void SiteCacheKeySeparatesSiteScopedEntries()
    {
        var defaultMenuKey = SiteCacheKey.For(SystemIds.DefaultSiteId, "menu");
        var otherMenuKey = SiteCacheKey.For(OtherSiteId, "menu");

        Assert.AreNotEqual(defaultMenuKey, otherMenuKey);
        Assert.IsTrue(defaultMenuKey.EndsWith(":menu", StringComparison.Ordinal));
        Assert.IsTrue(otherMenuKey.EndsWith(":menu", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task IndexPageUsesSiteScopedPostCountCacheKey()
    {
        var cache = new CapturingBlogCache();
        var mediator = new StubMediator(request => request switch
        {
            ListPostsQuery => new List<PostDigest>(),
            CountPostQuery => 7,
            _ => throw new InvalidOperationException($"Unexpected request {request.GetType().Name}.")
        });
        var model = new IndexModel(
            new BlogConfig { ContentSettings = new ContentSettings { PostListPageSize = 10 } },
            cache,
            mediator,
            new FixedSiteContext(OtherSiteId))
        {
            PageContext = CreatePageContext()
        };

        await model.OnGet();

        Assert.AreEqual(CacheDivision.General, cache.LastDivision);
        Assert.AreEqual(SiteCacheKey.For(OtherSiteId, "postcount"), cache.LastKey);
        Assert.AreEqual(7, model.Posts.TotalItemCount);
    }

    [TestMethod]
    public async Task BlogPageUsesSiteScopedSlugCacheKey()
    {
        var cache = new CapturingBlogCache();
        var mediator = new StubMediator(request => request switch
        {
            GetPageBySlugQuery => new BlogPage { Slug = "about", Title = "About", IsPublished = true },
            _ => throw new InvalidOperationException($"Unexpected request {request.GetType().Name}.")
        });
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["CacheSlidingExpirationMinutes:Page"] = "20"
            })
            .Build();
        var model = new BlogPageModel(mediator, cache, configuration, new FixedSiteContext(OtherSiteId));

        await model.OnGetAsync("About");

        Assert.AreEqual(CacheDivision.Page, cache.LastDivision);
        Assert.AreEqual(SiteCacheKey.For(OtherSiteId, "about"), cache.LastKey);
        Assert.AreEqual("About", model.BlogPage.Title);
    }

    [TestMethod]
    public async Task MenuViewComponentUsesSiteScopedCacheKey()
    {
        var cache = new CapturingBlogCache();
        var mediator = new StubMediator(request => request switch
        {
            GetAllMenusQuery => new List<Menu>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Other Site Menu",
                    Url = "/"
                }
            },
            _ => throw new InvalidOperationException($"Unexpected request {request.GetType().Name}.")
        });
        var component = new MenuViewComponent(cache, mediator, new FixedSiteContext(OtherSiteId));

        await component.InvokeAsync();

        Assert.AreEqual(CacheDivision.General, cache.LastDivision);
        Assert.AreEqual(SiteCacheKey.For(OtherSiteId, "menu"), cache.LastKey);
    }

    [TestMethod]
    public async Task CategoryListPageUsesSiteScopedCountCacheKey()
    {
        var categoryId = Guid.NewGuid();
        var cache = new CapturingBlogCache();
        var mediator = new StubMediator(request => request switch
        {
            GetCategoryByRouteQuery => new Category
            {
                Id = categoryId,
                RouteName = "notes",
                DisplayName = "Notes"
            },
            CountPostQuery => 3,
            ListPostsQuery => new List<PostDigest>(),
            _ => throw new InvalidOperationException($"Unexpected request {request.GetType().Name}.")
        });
        var model = new CategoryListModel(
            new BlogConfig { ContentSettings = new ContentSettings { PostListPageSize = 10 } },
            mediator,
            cache,
            new FixedSiteContext(OtherSiteId))
        {
            PageContext = CreatePageContext()
        };

        await model.OnGetAsync("notes");

        Assert.AreEqual(CacheDivision.PostCountCategory, cache.LastDivision);
        Assert.AreEqual(SiteCacheKey.For(OtherSiteId, categoryId.ToString()), cache.LastKey);
        Assert.AreEqual(3, model.Posts.TotalItemCount);
    }

    [TestMethod]
    public async Task TagListPageUsesSiteScopedCountCacheKey()
    {
        const int tagId = 7;
        var cache = new CapturingBlogCache();
        var mediator = new StubMediator(request => request switch
        {
            GetTagQuery => new Tag
            {
                Id = tagId,
                NormalizedName = "dotnet",
                DisplayName = ".NET"
            },
            ListByTagQuery => new List<PostDigest>(),
            CountPostQuery => 4,
            _ => throw new InvalidOperationException($"Unexpected request {request.GetType().Name}.")
        });
        var model = new TagListModel(
            mediator,
            new BlogConfig { ContentSettings = new ContentSettings { PostListPageSize = 10 } },
            cache,
            new FixedSiteContext(OtherSiteId))
        {
            PageContext = CreatePageContext()
        };

        await model.OnGet("dotnet");

        Assert.AreEqual(CacheDivision.PostCountTag, cache.LastDivision);
        Assert.AreEqual(SiteCacheKey.For(OtherSiteId, tagId.ToString()), cache.LastKey);
        Assert.AreEqual(4, model.Posts.TotalItemCount);
    }

    [TestMethod]
    public async Task FeaturedPageUsesSiteScopedCountCacheKey()
    {
        var cache = new CapturingBlogCache();
        var mediator = new StubMediator(request => request switch
        {
            ListFeaturedQuery => new List<PostDigest>(),
            CountPostQuery => 5,
            _ => throw new InvalidOperationException($"Unexpected request {request.GetType().Name}.")
        });
        var model = new FeaturedModel(
            new BlogConfig { ContentSettings = new ContentSettings { PostListPageSize = 10 } },
            cache,
            mediator,
            new FixedSiteContext(OtherSiteId))
        {
            PageContext = CreatePageContext()
        };

        await model.OnGet();

        Assert.AreEqual(CacheDivision.PostCountFeatured, cache.LastDivision);
        Assert.AreEqual(SiteCacheKey.For(OtherSiteId, "featured"), cache.LastKey);
        Assert.AreEqual(5, model.Posts.TotalItemCount);
    }

    [TestMethod]
    public async Task SubscriptionControllerUsesSiteScopedFeedCacheKeys()
    {
        var cache = new CapturingBlogCache();
        var mediator = new StubMediator(request => request switch
        {
            GetRssStringQuery => "<rss />",
            GetAtomStringQuery => "<feed />",
            _ => throw new InvalidOperationException($"Unexpected request {request.GetType().Name}.")
        });
        var controller = new SubscriptionController(
            new BlogConfig { GeneralSettings = new GeneralSettings { SiteTitle = "Site" } },
            cache,
            mediator,
            new FixedSiteContext(OtherSiteId));

        await controller.Rss("News");

        Assert.AreEqual(CacheDivision.RssCategory, cache.LastDivision);
        Assert.AreEqual(SiteCacheKey.For(OtherSiteId, "news"), cache.LastKey);

        await controller.Atom();

        Assert.AreEqual(CacheDivision.General, cache.LastDivision);
        Assert.AreEqual(SiteCacheKey.For(OtherSiteId, "atom"), cache.LastKey);
    }

    [TestMethod]
    public async Task ThemeControllerUsesSiteScopedCssCacheKey()
    {
        var cache = new CapturingBlogCache();
        var mediator = new StubMediator(request => request switch
        {
            GetStyleSheetQuery => ":root { --accent-color1: #fff; }",
            _ => throw new InvalidOperationException($"Unexpected request {request.GetType().Name}.")
        });
        var controller = new ThemeController(
            mediator,
            cache,
            new BlogConfig { GeneralSettings = new GeneralSettings { ThemeId = 1 } },
            new FixedSiteContext(OtherSiteId));

        await controller.Css();

        Assert.AreEqual(CacheDivision.General, cache.LastDivision);
        Assert.AreEqual(SiteCacheKey.For(OtherSiteId, "theme"), cache.LastKey);
    }

    [TestMethod]
    public async Task AssetsControllerUsesSiteScopedAvatarCacheKey()
    {
        var cache = new CapturingBlogCache();
        var mediator = new StubMediator(request => request switch
        {
            GetAssetQuery => Convert.ToBase64String(new byte[] { 1, 2, 3 }),
            _ => throw new InvalidOperationException($"Unexpected request {request.GetType().Name}.")
        });
        var controller = new AssetsController(
            NullLogger<AssetsController>.Instance,
            mediator,
            new StubWebHostEnvironment(),
            new FixedSiteContext(OtherSiteId));

        await controller.Avatar(cache);

        Assert.AreEqual(CacheDivision.General, cache.LastDivision);
        Assert.AreEqual(SiteCacheKey.For(OtherSiteId, "avatar"), cache.LastKey);
    }

    [TestMethod]
    public async Task SiteMapMiddlewareUsesCurrentSiteForCacheAndData()
    {
        await using var context = CreateContext();
        await context.Post.AddRangeAsync(
            CreatePost(SystemIds.DefaultSiteId, "Default Site Post"),
            CreatePost(OtherSiteId, "Other Site Post"));
        await context.CustomPage.AddRangeAsync(
            CreatePage(SystemIds.DefaultSiteId, "default-page", "Default Page"),
            CreatePage(OtherSiteId, "other-page", "Other Page"));
        await context.SaveChangesAsync();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/sitemap.xml";
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("other.example.com");
        httpContext.Response.Body = new MemoryStream();
        var cache = new CapturingBlogCache();
        var middleware = new SiteMapMiddleware(_ => throw new InvalidOperationException("Sitemap request should not call next middleware."));

        await middleware.Invoke(
            httpContext,
            new BlogConfig { AdvancedSettings = new AdvancedSettings { EnableSiteMap = true } },
            cache,
            CreateSiteMapConfiguration(),
            new BlogDbContextRepository<PostEntity>(context),
            new BlogDbContextRepository<PageEntity>(context),
            new FixedSiteContext(OtherSiteId));

        httpContext.Response.Body.Position = 0;
        using var reader = new StreamReader(httpContext.Response.Body);
        var xml = await reader.ReadToEndAsync();

        Assert.AreEqual(CacheDivision.General, cache.LastDivision);
        Assert.AreEqual(SiteCacheKey.For(OtherSiteId, "sitemap"), cache.LastKey);
        Assert.Contains("other-site-post", xml);
        Assert.Contains("other-page", xml);
        Assert.DoesNotContain("default-site-post", xml);
        Assert.DoesNotContain("default-page", xml);
    }

    [TestMethod]
    public void ClearBlogCacheRemovesSiteScopedDivisions()
    {
        var cache = new CapturingBlogCache();
        var filter = new ClearBlogCache(
            BlogCacheType.Subscription | BlogCacheType.SiteMap | BlogCacheType.PagingCount,
            cache);

        filter.OnActionExecuted(CreateActionExecutedContext());

        CollectionAssert.Contains(cache.RemovedDivisions, CacheDivision.General);
        CollectionAssert.Contains(cache.RemovedDivisions, CacheDivision.RssCategory);
        CollectionAssert.Contains(cache.RemovedDivisions, CacheDivision.PostCountCategory);
        CollectionAssert.Contains(cache.RemovedDivisions, CacheDivision.PostCountTag);
        CollectionAssert.Contains(cache.RemovedDivisions, CacheDivision.PostCountFeatured);
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

    private static PostContentEntity CreatePostContent(Guid postId, PostContentKind kind, string cultureCode, string body) => new()
    {
        SiteId = SystemIds.DefaultSiteId,
        PostId = postId,
        CultureCode = cultureCode,
        ContentKind = kind,
        Body = body,
        IsOriginal = kind == PostContentKind.RawMarkdown
    };

    private static AiArtifactEntity CreateArtifact(Guid postId, AiArtifactType type, string cultureCode, string content) => new()
    {
        SiteId = SystemIds.DefaultSiteId,
        TargetEntityType = nameof(PostEntity),
        TargetEntityId = postId,
        ArtifactType = type,
        CultureCode = cultureCode,
        Content = content
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

    private static PageContext CreatePageContext() => new()
    {
        ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
    };

    private static IConfiguration CreateSiteMapConfiguration() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string>
        {
            ["SiteMap:ChangeFreq:Posts"] = "monthly",
            ["SiteMap:ChangeFreq:Pages"] = "monthly",
            ["SiteMap:ChangeFreq:Default"] = "weekly"
        })
        .Build();

    private static ActionExecutedContext CreateActionExecutedContext() => new(
        new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
        new List<IFilterMetadata>(),
        null);

    private sealed class CapturingBlogCache : IBlogCache
    {
        public CacheDivision LastDivision { get; private set; }
        public string LastKey { get; private set; }
        public List<CacheDivision> RemovedDivisions { get; } = new();

        public TItem GetOrCreate<TItem>(CacheDivision division, string key, Func<ICacheEntry, TItem> factory)
        {
            LastDivision = division;
            LastKey = key;

            return factory(new StubCacheEntry(key));
        }

        public Task<TItem> GetOrCreateAsync<TItem>(CacheDivision division, string key, Func<ICacheEntry, Task<TItem>> factory)
        {
            LastDivision = division;
            LastKey = key;

            return factory(new StubCacheEntry(key));
        }

        public void RemoveAllCache()
        {
        }

        public void Remove(CacheDivision division)
        {
            LastDivision = division;
            LastKey = null;
            RemovedDivisions.Add(division);
        }

        public void Remove(CacheDivision division, string key)
        {
            LastDivision = division;
            LastKey = key;
        }
    }

    private sealed class StubCacheEntry(object key) : ICacheEntry
    {
        public object Key { get; } = key;
        public object Value { get; set; }
        public DateTimeOffset? AbsoluteExpiration { get; set; }
        public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }
        public TimeSpan? SlidingExpiration { get; set; }
        public IList<IChangeToken> ExpirationTokens { get; } = new List<IChangeToken>();
        public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks { get; } = new List<PostEvictionCallbackRegistration>();
        public CacheItemPriority Priority { get; set; }
        public long? Size { get; set; }

        public void Dispose()
        {
        }
    }

    private sealed class StubMediator(Func<object, object> handler) : IMediator
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            Task.FromResult((TResponse)handler(request));

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            handler(request);
            return Task.CompletedTask;
        }

        public Task<object> Send(object request, CancellationToken cancellationToken = default) =>
            Task.FromResult(handler(request));

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<object> CreateStream(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task Publish(object notification, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification =>
            Task.CompletedTask;
    }

    private sealed class StubWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = nameof(SiteScopedSpecTests);
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = "/tmp";
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = "/tmp";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
