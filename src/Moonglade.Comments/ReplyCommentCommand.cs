using MediatR;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;
using MoongladePure.Utils;

namespace MoongladePure.Comments;

public record ReplyCommentCommand(Guid CommentId, string ReplyContent) : IRequest<CommentReply>;

public class ReplyCommentCommandHandler(
    IRepository<PostEntity> postRepo,
    IRepository<CommentEntity> commentRepo,
    IRepository<CommentReplyEntity> commentReplyRepo)
    : IRequestHandler<ReplyCommentCommand, CommentReply>
{
    public async Task<CommentReply> Handle(ReplyCommentCommand request, CancellationToken ct)
    {
        var cmt = await commentRepo.GetAsync(c => c.SiteId == SystemIds.DefaultSiteId && c.Id == request.CommentId);
        if (cmt is null) throw new InvalidOperationException($"Comment {request.CommentId} is not found.");

        var id = Guid.NewGuid();
        var model = new CommentReplyEntity
        {
            Id = id,
            SiteId = cmt.SiteId,
            ReplyContent = request.ReplyContent,
            CreateTimeUtc = DateTime.UtcNow,
            CommentId = request.CommentId
        };

        await commentReplyRepo.AddAsync(model, ct);

        cmt.Post = await postRepo.GetAsync(p => p.SiteId == cmt.SiteId && p.Id == cmt.PostId);
        var reply = new CommentReply
        {
            CommentContent = cmt.CommentContent,
            CommentId = request.CommentId,
            Email = cmt.Email,
            Id = model.Id,
            PostId = cmt.PostId,
            PubDateUtc = cmt.Post.PubDateUtc.GetValueOrDefault(),
            ReplyContent = model.ReplyContent,
            ReplyContentHtml = ContentProcessor.MarkdownToContent(model.ReplyContent, ContentProcessor.MarkdownConvertType.Html),
            ReplyTimeUtc = model.CreateTimeUtc,
            Slug = cmt.Post.Slug,
            Title = cmt.Post.Title
        };

        return reply;
    }
}
