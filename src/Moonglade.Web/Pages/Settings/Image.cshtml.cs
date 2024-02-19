using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MoongladePure.Web.Pages.Settings;

public class ImageModel(IBlogConfig blogConfig) : PageModel
{
    public ImageSettings ViewModel { get; set; }

    public void OnGet() => ViewModel = blogConfig.ImageSettings;
}