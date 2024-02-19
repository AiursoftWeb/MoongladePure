using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MoongladePure.Web.Pages.Settings;

public class CustomStyleSheetModel(IBlogConfig blogConfig) : PageModel
{
    public CustomStyleSheetSettings ViewModel { get; set; }

    public void OnGet() => ViewModel = blogConfig.CustomStyleSheetSettings;
}