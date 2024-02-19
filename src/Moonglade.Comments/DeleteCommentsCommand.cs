using MediatR;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;
using MoongladePure.Data.Spec;

namespace MoongladePure.Comments;

public record DeleteCommentsCommand(Guid[] Ids) : IRequest;

public class DeleteCommentsCommandHandler(
    IRepository<CommentEntity> commentRepo,
    IRepository<CommentReplyEntity> commentReplyRepo)
    : IRequestHandler<DeleteCommentsCommand>
{
    public async Task Handle(DeleteCommentsCommand request, CancellationToken ct)
    {
        var spec = new CommentSpec(request.Ids);
        var comments = await commentRepo.ListAsync(spec);
        foreach (var cmt in comments)
        {
            // 1. Delete all replies
            var cReplies = await commentReplyRepo.ListAsync(new CommentReplySpec(cmt.Id));
            if (cReplies.Any())
            {
                await commentReplyRepo.DeleteAsync(cReplies, ct);
            }

            // 2. Delete comment itself
            await commentRepo.DeleteAsync(cmt, ct);
        }
    }
}