using Microsoft.AspNetCore.Mvc.RazorPages;
using MoongladePure.Menus;

namespace MoongladePure.Web.Pages.Admin;

public class MenuModel(IMediator mediator) : PageModel
{
    [BindProperty]
    public IReadOnlyList<Menu> MenuItems { get; set; } = new List<Menu>();

    public async Task OnGet() => MenuItems = await mediator.Send(new GetAllMenusQuery());
}