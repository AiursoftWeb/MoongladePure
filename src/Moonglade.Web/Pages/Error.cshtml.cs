using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;

namespace MoongladePure.Web.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    private readonly ILogger<ErrorModel> _logger;

    public string RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public ErrorModel(ILogger<ErrorModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {
        var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        if (exceptionFeature is not null)
        {
            // Get the exception that occurred
            var exceptionThatOccurred = exceptionFeature.Error;
            _logger.LogError("Error: {RouteWhereExceptionOccurred}, client IP: {ClientIp}, request id: {RequestId}", 
                exceptionThatOccurred.Message, 
                Helper.GetClientIP(HttpContext), 
                requestId);
        }

        RequestId = requestId;
    }
}