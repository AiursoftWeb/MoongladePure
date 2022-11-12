using Microsoft.AspNetCore.Mvc.RazorPages;
using MoongladePure.FriendLink;

namespace MoongladePure.Web.Pages.Admin;

public class FriendLinkModel : PageModel
{
    private readonly IMediator _mediator;

    public UpdateLinkCommand EditLinkRequest { get; set; }

    public IReadOnlyList<Link> Links { get; set; }

    public FriendLinkModel(IMediator mediator)
    {
        _mediator = mediator;
        EditLinkRequest = new();
    }

    public async Task OnGet() => Links = await _mediator.Send(new GetAllLinksQuery());
}