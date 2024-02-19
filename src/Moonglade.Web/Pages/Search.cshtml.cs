using Microsoft.AspNetCore.Mvc.RazorPages;
using MoongladePure.Core.PostFeature;
using MoongladePure.Web.Attributes;

namespace MoongladePure.Web.Pages;

[AddXRobotsTag("noindex, nofollow")]
public class SearchModel(IMediator mediator) : PageModel
{
    public IReadOnlyList<PostDigest> Posts { get; set; }

    public async Task<IActionResult> OnGetAsync(string term)
    {
        if (string.IsNullOrWhiteSpace(term)) return RedirectToPage("Index");

        ViewData["TitlePrefix"] = term;

        var posts = await mediator.Send(new SearchPostQuery(term));
        Posts = posts;

        return Page();
    }
}