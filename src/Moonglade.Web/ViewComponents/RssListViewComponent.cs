using MoongladePure.Core.CategoryFeature;

namespace MoongladePure.Web.ViewComponents;

public class RssListViewComponent(ILogger<RssListViewComponent> logger, IMediator mediator) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        try
        {
            var cats = await mediator.Send(new GetCategoriesQuery());
            var items = cats.Select(c => new KeyValuePair<string, string>(c.DisplayName, c.RouteName));

            return View(items);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error");
            return Content(e.Message);
        }
    }
}