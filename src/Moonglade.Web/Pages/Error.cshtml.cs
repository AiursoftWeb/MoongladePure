using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;

namespace MoongladePure.Web.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class ErrorModel(ILogger<ErrorModel> logger) : PageModel
{
    public string RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public void OnGet()
    {
        var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        if (exceptionFeature is not null)
        {
            // Get the exception that occurred
            var exceptionThatOccurred = exceptionFeature.Error;
            logger.LogError("Error: {RouteWhereExceptionOccurred}, client IP: {ClientIp}, request id: {RequestId}", 
                exceptionThatOccurred.Message, 
                Helper.GetClientIP(HttpContext), 
                requestId);
        }

        RequestId = requestId;
    }
}