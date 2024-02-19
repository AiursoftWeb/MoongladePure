using Microsoft.AspNetCore.Mvc.RazorPages;
using MoongladePure.Core.PostFeature;
using MoongladePure.Core.TagFeature;
using X.PagedList;

namespace MoongladePure.Web.Pages;

public class TagListModel(IMediator mediator, IBlogConfig blogConfig, IBlogCache cache)
    : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int P { get; set; } = 1;

    public StaticPagedList<PostDigest> Posts { get; set; }

    public async Task<IActionResult> OnGet(string normalizedName)
    {
        var tagResponse = await mediator.Send(new GetTagQuery(normalizedName));
        if (tagResponse is null) return NotFound();

        var pagesize = blogConfig.ContentSettings.PostListPageSize;
        var posts = await mediator.Send(new ListByTagQuery(tagResponse.Id, pagesize, P));
        var count = await cache.GetOrCreateAsync(CacheDivision.PostCountTag, tagResponse.Id.ToString(), _ => mediator.Send(new CountPostQuery(CountType.Tag, TagId: tagResponse.Id)));

        ViewData["TitlePrefix"] = tagResponse.DisplayName;

        var list = new StaticPagedList<PostDigest>(posts, P, pagesize, count);
        Posts = list;

        return Page();
    }
}