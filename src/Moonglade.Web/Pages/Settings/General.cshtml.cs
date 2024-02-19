using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MoongladePure.Web.Pages.Settings;

public class GeneralModel(IBlogConfig blogConfig, IMediator mediator) : PageModel
{
    public GeneralSettings ViewModel { get; set; }

    public CreateThemeRequest ThemeRequest { get; set; }

    public IReadOnlyList<ThemeSegment> Themes { get; set; }

    public async Task OnGetAsync()
    {
        ViewModel = blogConfig.GeneralSettings;

        Themes = await mediator.Send(new GetAllThemeSegmentQuery());
    }
}