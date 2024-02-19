using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MoongladePure.Web.Pages.Settings;

public class SubscriptionModel(IBlogConfig blogConfig) : PageModel
{
    public FeedSettings ViewModel { get; set; }

    public void OnGet() => ViewModel = blogConfig.FeedSettings;
}