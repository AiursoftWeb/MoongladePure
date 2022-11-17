using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MoongladePure.Web.Pages.Settings;

public class GeneralModel : PageModel
{
    private readonly IBlogConfig _blogConfig;
    private readonly IMediator _mediator;

    public GeneralSettings ViewModel { get; set; }

    public CreateThemeRequest ThemeRequest { get; set; }

    public IReadOnlyList<ThemeSegment> Themes { get; set; }

    public GeneralModel(IBlogConfig blogConfig, IMediator mediator)
    {
        _blogConfig = blogConfig;
        _mediator = mediator;
    }

    public async Task OnGetAsync()
    {
        ViewModel = _blogConfig.GeneralSettings;

        Themes = await _mediator.Send(new GetAllThemeSegmentQuery());
    }
}