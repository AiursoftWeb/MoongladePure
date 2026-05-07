using Microsoft.AspNetCore.Mvc.Filters;

namespace MoongladePure.Caching.Filters;

[Flags]
public enum BlogCacheType
{
    None = 1,
    Subscription = 2,
    SiteMap = 4,
    PagingCount = 8
}

public class ClearBlogCache(CacheDivision division, string cacheKey, IBlogCache cache) : ActionFilterAttribute
{
    private readonly BlogCacheType _type = BlogCacheType.None;

    public ClearBlogCache(BlogCacheType type, IBlogCache cache) : this(CacheDivision.General, null, cache)
    {
        _type = type;
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        base.OnActionExecuted(context);

        if (_type.HasFlag(BlogCacheType.None))
        {
            if (division == CacheDivision.General)
            {
                cache.Remove(division);
            }
            else
            {
                cache.Remove(division, cacheKey);
            }
        }

        if (_type.HasFlag(BlogCacheType.Subscription))
        {
            cache.Remove(CacheDivision.General);
            cache.Remove(CacheDivision.RssCategory);
        }

        if (_type.HasFlag(BlogCacheType.SiteMap))
        {
            cache.Remove(CacheDivision.General);
        }

        if (_type.HasFlag(BlogCacheType.PagingCount))
        {
            cache.Remove(CacheDivision.General);
            cache.Remove(CacheDivision.PostCountCategory);
            cache.Remove(CacheDivision.PostCountTag);
            cache.Remove(CacheDivision.PostCountFeatured);
        }
    }
}
