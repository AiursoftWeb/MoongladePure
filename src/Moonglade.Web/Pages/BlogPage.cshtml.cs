using Microsoft.AspNetCore.Mvc.RazorPages;
using MoongladePure.Core.PageFeature;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Web.Pages;

public class BlogPageModel(IMediator mediator, IBlogCache cache, IConfiguration configuration, ISiteContext siteContext)
    : PageModel
{
    public BlogPage BlogPage { get; set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return BadRequest();

        var page = await cache.GetOrCreateAsync(
            CacheDivision.Page,
            SiteCacheKey.For(siteContext.SiteId, slug.ToLower()),
            async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(int.Parse(configuration["CacheSlidingExpirationMinutes:Page"] ?? "0"));

            var p = await mediator.Send(new GetPageBySlugQuery(slug));
            return p;
        });

        if (page is null || !page.IsPublished) return NotFound();

        BlogPage = page;
        return Page();
    }
}
