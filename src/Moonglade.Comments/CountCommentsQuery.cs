using MediatR;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Comments;

public record CountCommentsQuery : IRequest<int>;

public class CountCommentsQueryHandler(IRepository<CommentEntity> repo) : IRequestHandler<CountCommentsQuery, int>
{
    public Task<int> Handle(CountCommentsQuery request, CancellationToken ct) => repo.CountAsync(ct: ct);
}