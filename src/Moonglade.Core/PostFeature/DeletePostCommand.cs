﻿using Moonglade.Caching;

namespace Moonglade.Core.PostFeature;

public record DeletePostCommand(Guid Id, bool SoftDelete = false) : IRequest;

public class DeletePostCommandHandler : AsyncRequestHandler<DeletePostCommand>
{
    private readonly IRepository<PostEntity> _postRepo;
    private readonly IBlogCache _cache;

    public DeletePostCommandHandler(IRepository<PostEntity> postRepo, IBlogCache cache)
    {
        _postRepo = postRepo;
        _cache = cache;
    }

    protected override async Task Handle(DeletePostCommand request, CancellationToken ct)
    {
        var (guid, softDelete) = request;
        var post = await _postRepo.GetAsync(guid);
        if (null == post) return;

        if (softDelete)
        {
            post.IsDeleted = true;
            await _postRepo.UpdateAsync(post, ct);
        }
        else
        {
            await _postRepo.DeleteAsync(post, ct);
        }

        _cache.Remove(CacheDivision.Post, guid.ToString());
    }
}