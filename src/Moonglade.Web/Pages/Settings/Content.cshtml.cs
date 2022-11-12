using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MoongladePure.Web.Pages.Settings;

public class ContentModel : PageModel
{
    private readonly IBlogConfig _blogConfig;
    public ContentSettings ViewModel { get; set; }

    public ContentModel(IBlogConfig blogConfig) => _blogConfig = blogConfig;

    public void OnGet() => ViewModel = _blogConfig.ContentSettings;
}