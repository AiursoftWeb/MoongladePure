using MoongladePure.FriendLink;

namespace MoongladePure.Web.ViewComponents;

public class FriendLinkViewComponent(ILogger<FriendLinkViewComponent> logger, IMediator mediator)
    : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        try
        {
            var links = await mediator.Send(new GetAllLinksQuery());
            return View(links ?? new List<Link>());
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error Reading FriendLink");
            return Content(e.Message);
        }
    }
}