using MoongladePure.Data.Spec;

namespace MoongladePure.Core.PostFeature;

public record ListInsightsQuery(PostInsightsType PostInsightsType) : IRequest<IReadOnlyList<PostSegment>>;

public class ListInsightsQueryHandler(IRepository<PostEntity> repo)
    : IRequestHandler<ListInsightsQuery, IReadOnlyList<PostSegment>>
{
    public Task<IReadOnlyList<PostSegment>> Handle(ListInsightsQuery request, CancellationToken ct)
    {
        var spec = new PostInsightsSpec(request.PostInsightsType, 10);
        return repo.SelectAsync(spec, PostSegment.EntitySelector);
    }
}