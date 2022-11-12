using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MoongladePure.Web.Pages.Settings;

public class CustomStyleSheetModel : PageModel
{
    private readonly IBlogConfig _blogConfig;

    public CustomStyleSheetSettings ViewModel { get; set; }

    public CustomStyleSheetModel(IBlogConfig blogConfig) => _blogConfig = blogConfig;

    public void OnGet() => ViewModel = _blogConfig.CustomStyleSheetSettings;
}