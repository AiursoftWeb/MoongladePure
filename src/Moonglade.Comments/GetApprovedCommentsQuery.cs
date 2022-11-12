﻿using MediatR;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;
using MoongladePure.Data.Spec;

namespace MoongladePure.Comments;

public record GetApprovedCommentsQuery(Guid PostId) : IRequest<IReadOnlyList<Comment>>;

public class GetApprovedCommentsQueryHandler : IRequestHandler<GetApprovedCommentsQuery, IReadOnlyList<Comment>>
{
    private readonly IRepository<CommentEntity> _repo;

    public GetApprovedCommentsQueryHandler(IRepository<CommentEntity> repo) => _repo = repo;

    public Task<IReadOnlyList<Comment>> Handle(GetApprovedCommentsQuery request, CancellationToken ct)
    {
        return _repo.SelectAsync(new CommentSpec(request.PostId), c => new Comment
        {
            CommentContent = c.CommentContent,
            CreateTimeUtc = c.CreateTimeUtc,
            Username = c.Username,
            Email = c.Email,
            CommentReplies = c.Replies.Select(cr => new CommentReplyDigest
            {
                ReplyContent = cr.ReplyContent,
                ReplyTimeUtc = cr.CreateTimeUtc
            }).ToList()
        });
    }
}