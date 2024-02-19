using MoongladePure.Core.CategoryFeature;
using MoongladePure.Syndication;
using System.ComponentModel.DataAnnotations;

namespace MoongladePure.Web.Controllers;

[ApiController]
public class SubscriptionController(
    IBlogConfig blogConfig,
    IBlogCache cache,
    IMediator mediator)
    : ControllerBase
{
    [HttpGet("opml")]
    public async Task<IActionResult> Opml()
    {
        var cats = await mediator.Send(new GetCategoriesQuery());
        var catInfos = cats.Select(c => new KeyValuePair<string, string>(c.DisplayName, c.RouteName));
        var rootUrl = Helper.ResolveRootUrl(HttpContext);

        var oi = new OpmlDoc
        {
            SiteTitle = $"{blogConfig.GeneralSettings.SiteTitle} - OPML",
            ContentInfo = catInfos,
            HtmlUrl = $"{rootUrl}/post",
            XmlUrl = $"{rootUrl}/rss",
            XmlUrlTemplate = $"{rootUrl}/rss/[catTitle]",
            HtmlUrlTemplate = $"{rootUrl}/category/[catTitle]"
        };

        var xml = await mediator.Send(new GetOpmlQuery(oi));
        return Content(xml, "text/xml");
    }

    [HttpGet("rss/{routeName?}")]
    public async Task<IActionResult> Rss([MaxLength(64)] string routeName = null)
    {
        bool hasRoute = !string.IsNullOrWhiteSpace(routeName);
        var route = hasRoute ? routeName.ToLower().Trim() : null;

        return await cache.GetOrCreateAsync(
            hasRoute ? CacheDivision.RssCategory : CacheDivision.General, route ?? "rss", async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(1);

                var xml = await mediator.Send(new GetRssStringQuery(routeName));
                if (string.IsNullOrWhiteSpace(xml))
                {
                    return (IActionResult)NotFound();
                }

                return Content(xml, "text/xml");
            });
    }

    [HttpGet("atom")]
    public async Task<IActionResult> Atom()
    {
        return await cache.GetOrCreateAsync(CacheDivision.General, "atom", async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromHours(1);

            var xml = await mediator.Send(new GetAtomStringQuery());
            return Content(xml, "text/xml");
        });
    }
}