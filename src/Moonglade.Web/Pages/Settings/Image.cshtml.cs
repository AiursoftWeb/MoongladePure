using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MoongladePure.Web.Pages.Settings;

public class ImageModel : PageModel
{
    private readonly IBlogConfig _blogConfig;

    public ImageSettings ViewModel { get; set; }

    public ImageModel(IBlogConfig blogConfig) => _blogConfig = blogConfig;

    public void OnGet() => ViewModel = _blogConfig.ImageSettings;
}