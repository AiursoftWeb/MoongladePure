using Microsoft.AspNetCore.Mvc.RazorPages;
using MoongladePure.Core.PageFeature;

namespace MoongladePure.Web.Pages;

[Authorize]
public class PagePreviewModel(IMediator mediator) : PageModel
{
    public BlogPage BlogPage { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid pageId)
    {
        var page = await mediator.Send(new GetPageByIdQuery(pageId));
        if (page is null) return NotFound();

        BlogPage = page;
        return Page();
    }
}