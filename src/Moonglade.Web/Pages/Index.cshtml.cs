using Microsoft.AspNetCore.Mvc.RazorPages;
using MoongladePure.Core.PostFeature;
using MoongladePure.Data.Spec;
using X.PagedList;

namespace MoongladePure.Web.Pages;

public class IndexModel(IBlogConfig blogConfig, IBlogCache cache, IMediator mediator)
    : PageModel
{
    public string SortBy { get; set; }

    public StaticPagedList<PostDigest> Posts { get; set; }

    public async Task OnGet(int p = 1, string sortBy = "Recent")
    {
        var pagesize = blogConfig.ContentSettings.PostListPageSize;

        if (!Enum.TryParse(sortBy, ignoreCase: true, out PostsSortBy sortByEnum))
        {
            sortByEnum = PostsSortBy.Recent;
            sortBy = "Recent";
        }

        ViewData["sortBy"] = sortBy;

        var posts = await mediator.Send(new ListPostsQuery(pagesize, p, sortBy: sortByEnum));
        var totalPostsCount = await cache.GetOrCreateAsync(
            CacheDivision.General,
            "postcount",
            _ => mediator.Send(new CountPostQuery(CountType.Public)));

        Posts = new StaticPagedList<PostDigest>(posts, p, pagesize, totalPostsCount);
    }
}
