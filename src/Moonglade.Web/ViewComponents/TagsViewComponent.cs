using MoongladePure.Core.TagFeature;

namespace MoongladePure.Web.ViewComponents;

public class TagsViewComponent(IBlogConfig blogConfig, IMediator mediator) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        try
        {
            var tags = await mediator.Send(new GetHotTagsQuery(blogConfig.ContentSettings.HotTagAmount));
            return View(tags);
        }
        catch (Exception e)
        {
            return Content(e.Message);
        }
    }
}