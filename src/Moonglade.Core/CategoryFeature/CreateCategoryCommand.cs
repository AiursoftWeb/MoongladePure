using MoongladePure.Caching;
using System.ComponentModel.DataAnnotations;

namespace MoongladePure.Core.CategoryFeature;

public class CreateCategoryCommand : IRequest
{
    [Required]
    [Display(Name = "Display Name")]
    [MaxLength(64)]
    public string DisplayName { get; set; }

    [Required]
    [Display(Name = "Route Name")]
    [RegularExpression("(?!-)([a-z0-9-]+)")]
    [MaxLength(64)]
    public string RouteName { get; set; }

    [Required]
    [Display(Name = "Description")]
    [MaxLength(128)]
    public string Note { get; set; }
}

public class CreateCategoryCommandHandler(IRepository<CategoryEntity> catRepo, IBlogCache cache)
    : IRequestHandler<CreateCategoryCommand>
{
    public async Task Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        var exists = await catRepo.AnyAsync(c => c.RouteName == request.RouteName, ct);
        if (exists) return;

        var category = new CategoryEntity
        {
            Id = Guid.NewGuid(),
            RouteName = request.RouteName.Trim(),
            Note = request.Note?.Trim(),
            DisplayName = request.DisplayName.Trim()
        };

        await catRepo.AddAsync(category, ct);
        cache.Remove(CacheDivision.General, "allcats");
    }
}