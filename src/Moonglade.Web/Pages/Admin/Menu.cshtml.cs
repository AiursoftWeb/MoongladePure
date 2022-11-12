using Microsoft.AspNetCore.Mvc.RazorPages;
using MoongladePure.Menus;

namespace MoongladePure.Web.Pages.Admin;

public class MenuModel : PageModel
{
    private readonly IMediator _mediator;

    [BindProperty]
    public IReadOnlyList<Menu> MenuItems { get; set; }

    public MenuModel(IMediator mediator)
    {
        _mediator = mediator;
        MenuItems = new List<Menu>();
    }

    public async Task OnGet() => MenuItems = await _mediator.Send(new GetAllMenusQuery());
}