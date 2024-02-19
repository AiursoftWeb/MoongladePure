using Microsoft.AspNetCore.Mvc.Filters;

namespace MoongladePure.Web.Attributes;

public class AddXRobotsTagAttribute(string content) : ResultFilterAttribute
{
    public override void OnResultExecuting(ResultExecutingContext context)
    {
        if (!context.HttpContext.Response.Headers.ContainsKey("X-Robots-Tag"))
        {
            context.HttpContext.Response.Headers.Append("X-Robots-Tag", content);
        }

        base.OnResultExecuting(context);
    }
}