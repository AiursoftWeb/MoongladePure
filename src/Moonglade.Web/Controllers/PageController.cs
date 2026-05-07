using MoongladePure.Caching.Filters;
using MoongladePure.Core.PageFeature;
using MoongladePure.Data.Infrastructure;
using MoongladePure.Web.Attributes;
using NUglify;

namespace MoongladePure.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PageController(
    IBlogCache cache,
    IMediator mediator,
    ISiteContext siteContext) : Controller
{
    [HttpPost]
    [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { BlogCacheType.SiteMap })]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> Create(EditPageRequest model) =>
        CreateOrEdit(model, async request => await mediator.Send(new CreatePageCommand(request)));

    [HttpPut("{id:guid}")]
    [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { BlogCacheType.SiteMap })]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> Edit([NotEmpty] Guid id, EditPageRequest model) =>
        CreateOrEdit(model, async request => await mediator.Send(new UpdatePageCommand(id, request)));

    private async Task<IActionResult> CreateOrEdit(EditPageRequest model, Func<EditPageRequest, Task<Guid>> pageServiceAction)
    {
        if (!string.IsNullOrWhiteSpace(model.CssContent))
        {
            var uglifyTest = Uglify.Css(model.CssContent);
            if (uglifyTest.HasErrors)
            {
                foreach (var err in uglifyTest.Errors)
                {
                    ModelState.AddModelError(model.CssContent, err?.ToString() ?? string.Empty);
                }
                return BadRequest(ModelState.CombineErrorMessages());
            }
        }

        var uid = await pageServiceAction(model);

        cache.Remove(CacheDivision.Page, SiteCacheKey.For(siteContext.SiteId, model.Slug.ToLower()));
        return Ok(new { PageId = uid });
    }

    [HttpDelete("{id:guid}")]
    [ApiConventionMethod(typeof(DefaultApiConventions), nameof(DefaultApiConventions.Delete))]
    public async Task<IActionResult> Delete([NotEmpty] Guid id)
    {
        var page = await mediator.Send(new GetPageByIdQuery(id));
        if (page == null) return NotFound();

        await mediator.Send(new DeletePageCommand(id));

        cache.Remove(CacheDivision.Page, SiteCacheKey.For(siteContext.SiteId, page.Slug.ToLower()));
        return NoContent();
    }
}
