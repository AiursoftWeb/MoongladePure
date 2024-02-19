using MediatR;
using MoongladePure.Comments.Moderators;
using MoongladePure.Configuration;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;
using MoongladePure.Data.Spec;

namespace MoongladePure.Comments;

public class CreateCommentCommand(Guid postId, CommentRequest payload, string ipAddress) : IRequest<CommentDetailedItem>
{
    public Guid PostId { get; set; } = postId;

    public CommentRequest Payload { get; set; } = payload;

    public string IpAddress { get; set; } = ipAddress;
}

public class CreateCommentCommandHandler(
    IBlogConfig blogConfig,
    IRepository<PostEntity> postRepo,
    ICommentModerator moderator,
    IRepository<CommentEntity> commentRepo)
    : IRequestHandler<CreateCommentCommand, CommentDetailedItem>
{
    public async Task<CommentDetailedItem> Handle(CreateCommentCommand request, CancellationToken ct)
    {
        if (blogConfig.ContentSettings.EnableWordFilter)
        {
            switch (blogConfig.ContentSettings.WordFilterMode)
            {
                case WordFilterMode.Mask:
                    request.Payload.Username = await moderator.ModerateContent(request.Payload.Username);
                    request.Payload.Content = await moderator.ModerateContent(request.Payload.Content);
                    break;
                case WordFilterMode.Block:
                    if (await moderator.HasBadWord(request.Payload.Username, request.Payload.Content))
                    {
                        await Task.CompletedTask;
                        return null;
                    }
                    break;
            }
        }

        var model = new CommentEntity
        {
            Id = Guid.NewGuid(),
            Username = request.Payload.Username,
            CommentContent = request.Payload.Content,
            PostId = request.PostId,
            CreateTimeUtc = DateTime.UtcNow,
            Email = request.Payload.Email,
            IPAddress = request.IpAddress,
            IsApproved = !blogConfig.ContentSettings.RequireCommentReview
        };

        await commentRepo.AddAsync(model, ct);

        var spec = new PostSpec(request.PostId, false);
        var postTitle = await postRepo.FirstOrDefaultAsync(spec, p => p.Title);

        var item = new CommentDetailedItem
        {
            Id = model.Id,
            CommentContent = model.CommentContent,
            CreateTimeUtc = model.CreateTimeUtc,
            Email = model.Email,
            IpAddress = model.IPAddress,
            IsApproved = model.IsApproved,
            PostTitle = postTitle,
            Username = model.Username
        };

        return item;
    }
}