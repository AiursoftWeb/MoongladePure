using MediatR;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;
using MoongladePure.Data.Spec;

namespace MoongladePure.Comments;

public record GetCommentsQuery(int PageSize, int PageIndex) : IRequest<IReadOnlyList<CommentDetailedItem>>;

public class GetCommentsQueryHandler(IRepository<CommentEntity> repo)
    : IRequestHandler<GetCommentsQuery, IReadOnlyList<CommentDetailedItem>>
{
    public Task<IReadOnlyList<CommentDetailedItem>> Handle(GetCommentsQuery request, CancellationToken ct)
    {
        var spec = new CommentSpec(request.PageSize, request.PageIndex);
        var comments = repo.SelectAsync(spec, CommentDetailedItem.EntitySelector);

        return comments;
    }
}