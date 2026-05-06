using MediatR;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;
using System.Text.Json;

namespace MoongladePure.Theme;

public record CreateThemeCommand(string Name, IDictionary<string, string> Rules) : IRequest<int>;

public class CreateThemeCommandHandler(IRepository<BlogThemeEntity> repo, ISiteContext siteContext) : IRequestHandler<CreateThemeCommand, int>
{
    public async Task<int> Handle(CreateThemeCommand request, CancellationToken ct)
    {
        var (name, dictionary) = request;
        if (await repo.AnyAsync(p => (p.SiteId == null || p.SiteId == siteContext.SiteId) && p.ThemeName == name.Trim(), ct)) return 0;

        var rules = JsonSerializer.Serialize(dictionary);
        var blogTheme = new BlogThemeEntity
        {
            SiteId = siteContext.SiteId,
            ThemeName = name.Trim(),
            CssRules = rules,
            ThemeType = ThemeType.User
        };

        await repo.AddAsync(blogTheme, ct);
        return blogTheme.Id;
    }
}
