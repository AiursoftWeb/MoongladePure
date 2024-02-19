using Microsoft.AspNetCore.Mvc.RazorPages;
using MoongladePure.Core.CategoryFeature;

namespace MoongladePure.Web.Pages.Admin;

public class CategoryModel(IMediator mediator) : PageModel
{
    public CreateCategoryCommand EditCategoryRequest { get; set; } = new();

    public IReadOnlyList<Category> Categories { get; set; }

    public async Task OnGet() => Categories = await mediator.Send(new GetCategoriesQuery());
}