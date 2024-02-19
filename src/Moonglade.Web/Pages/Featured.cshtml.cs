using Microsoft.AspNetCore.Mvc.RazorPages;
using MoongladePure.Core.PostFeature;
using X.PagedList;

namespace MoongladePure.Web.Pages;

public class FeaturedModel(IBlogConfig blogConfig, IBlogCache cache, IMediator mediator)
    : PageModel
{
    public StaticPagedList<PostDigest> Posts { get; set; }

    public async Task OnGet(int p = 1)
    {
        var pagesize = blogConfig.ContentSettings.PostListPageSize;
        var posts = await mediator.Send(new ListFeaturedQuery(pagesize, p));
        var count = await cache.GetOrCreateAsync(CacheDivision.PostCountFeatured, "featured", _ => mediator.Send(new CountPostQuery(CountType.Featured)));

        var list = new StaticPagedList<PostDigest>(posts, p, pagesize, count);
        Posts = list;
    }
}