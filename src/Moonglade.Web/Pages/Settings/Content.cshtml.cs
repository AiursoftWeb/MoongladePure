using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MoongladePure.Web.Pages.Settings;

public class ContentModel(IBlogConfig blogConfig) : PageModel
{
    public ContentSettings ViewModel { get; set; }

    public void OnGet() => ViewModel = blogConfig.ContentSettings;
}