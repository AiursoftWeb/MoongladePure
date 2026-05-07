using MoongladePure.Menus;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Web.ViewComponents;

public class MenuViewComponent(IBlogCache cache, IMediator mediator, ISiteContext siteContext) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        try
        {
            var menus = await cache.GetOrCreateAsync(
                CacheDivision.General,
                SiteCacheKey.For(siteContext.SiteId, "menu"),
                async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(20);

                var items = await mediator.Send(new GetAllMenusQuery());
                return items;
            });

            return View(menus);
        }
        catch (Exception e)
        {
            return Content(e.Message);
        }
    }
}
