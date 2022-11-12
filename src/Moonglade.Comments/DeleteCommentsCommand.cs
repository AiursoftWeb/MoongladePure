using MediatR;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;
using MoongladePure.Data.Spec;

namespace MoongladePure.Comments;

public record DeleteCommentsCommand(Guid[] Ids) : IRequest;

public class DeleteCommentsCommandHandler : AsyncRequestHandler<DeleteCommentsCommand>
{
    private readonly IRepository<CommentEntity> _commentRepo;
    private readonly IRepository<CommentReplyEntity> _commentReplyRepo;

    public DeleteCommentsCommandHandler(IRepository<CommentEntity> commentRepo, IRepository<CommentReplyEntity> commentReplyRepo)
    {
        _commentRepo = commentRepo;
        _commentReplyRepo = commentReplyRepo;
    }

    protected override async Task Handle(DeleteCommentsCommand request, CancellationToken ct)
    {
        var spec = new CommentSpec(request.Ids);
        var comments = await _commentRepo.ListAsync(spec);
        foreach (var cmt in comments)
        {
            // 1. Delete all replies
            var cReplies = await _commentReplyRepo.ListAsync(new CommentReplySpec(cmt.Id));
            if (cReplies.Any())
            {
                await _commentReplyRepo.DeleteAsync(cReplies, ct);
            }

            // 2. Delete comment itself
            await _commentRepo.DeleteAsync(cmt, ct);
        }
    }
}