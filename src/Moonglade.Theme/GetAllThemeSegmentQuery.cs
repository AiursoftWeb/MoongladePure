using MediatR;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Theme;

public record GetAllThemeSegmentQuery : IRequest<IReadOnlyList<ThemeSegment>>;

public class GetAllThemeSegmentQueryHandler : IRequestHandler<GetAllThemeSegmentQuery, IReadOnlyList<ThemeSegment>>
{
    private readonly IRepository<BlogThemeEntity> _repo;

    public GetAllThemeSegmentQueryHandler(IRepository<BlogThemeEntity> repo) => _repo = repo;

    public Task<IReadOnlyList<ThemeSegment>> Handle(GetAllThemeSegmentQuery request, CancellationToken ct)
    {
        return _repo.SelectAsync(p => new ThemeSegment
        {
            Id = p.Id,
            Name = p.ThemeName
        }, ct);
    }
}