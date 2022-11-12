using Microsoft.AspNetCore.Mvc.RazorPages;
using MoongladePure.Core.PostFeature;

namespace MoongladePure.Web.Pages;

[Authorize]
public class PostPreviewModel : PageModel
{
    private readonly IMediator _mediator;

    public Post Post { get; set; }

    public PostPreviewModel(IMediator mediator) => _mediator = mediator;

    public async Task<IActionResult> OnGetAsync(Guid postId)
    {
        var post = await _mediator.Send(new GetDraftQuery(postId));
        if (post is null) return NotFound();

        ViewData["TitlePrefix"] = $"{post.Title}";

        Post = post;
        return Page();
    }
}