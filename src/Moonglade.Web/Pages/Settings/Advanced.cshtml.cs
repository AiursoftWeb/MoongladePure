using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MoongladePure.Web.Pages.Settings;

public class AdvancedModel(IBlogConfig blogConfig) : PageModel
{
    public AdvancedSettings ViewModel { get; set; }

    public void OnGet() => ViewModel = blogConfig.AdvancedSettings;
}