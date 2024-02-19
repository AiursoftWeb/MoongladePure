using Microsoft.AspNetCore.Mvc.RazorPages;
using MoongladePure.FriendLink;

namespace MoongladePure.Web.Pages.Admin;

public class FriendLinkModel(IMediator mediator) : PageModel
{
    public UpdateLinkCommand EditLinkRequest { get; set; } = new();

    public IReadOnlyList<Link> Links { get; set; }

    public async Task OnGet() => Links = await mediator.Send(new GetAllLinksQuery());
}