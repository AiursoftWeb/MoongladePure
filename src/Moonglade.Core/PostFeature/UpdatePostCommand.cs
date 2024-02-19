using Microsoft.EntityFrameworkCore;
using MoongladePure.Caching;
using MoongladePure.Core.TagFeature;
using MoongladePure.Utils;

namespace MoongladePure.Core.PostFeature;

public record UpdatePostCommand(Guid Id, PostEditModel Payload) : IRequest<PostEntity>;
public class UpdatePostCommandHandler(
    IRepository<PostCategoryEntity> pcRepository,
    IRepository<PostTagEntity> ptRepository,
    IRepository<TagEntity> tagRepo,
    IRepository<PostEntity> postRepo,
    IBlogCache cache)
    : IRequestHandler<UpdatePostCommand, PostEntity>
{
    public async Task<PostEntity> Handle(UpdatePostCommand request, CancellationToken ct)
    {
        var (guid, postEditModel) = request;
        var post = await postRepo.GetAsync(guid, ct);
        if (null == post)
        {
            throw new InvalidOperationException($"Post {guid} is not found.");
        }

        post.CommentEnabled = postEditModel.EnableComment;
        post.PostContent = postEditModel.EditorContent;

        if (postEditModel.IsPublished && !post.IsPublished)
        {
            post.IsPublished = true;
            post.PubDateUtc = DateTime.UtcNow;
        }

        // #325: Allow changing publish date for published posts
        if (postEditModel.PublishDate is not null && post.PubDateUtc.HasValue)
        {
            var tod = post.PubDateUtc.Value.TimeOfDay;
            var adjustedDate = postEditModel.PublishDate.Value;
            post.PubDateUtc = adjustedDate.AddTicks(tod.Ticks);
        }

        post.Author = postEditModel.Author?.Trim();
        post.Slug = postEditModel.Slug.ToLower().Trim();
        post.Title = postEditModel.Title.Trim();
        post.LastModifiedUtc = DateTime.UtcNow;
        post.IsFeedIncluded = postEditModel.FeedIncluded;
        post.ContentLanguageCode = postEditModel.LanguageCode;
        post.IsFeatured = postEditModel.Featured;
        post.IsOriginal = string.IsNullOrWhiteSpace(request.Payload.OriginLink);
        post.OriginLink = string.IsNullOrWhiteSpace(postEditModel.OriginLink) ? null : Helper.SterilizeLink(postEditModel.OriginLink);
        post.HeroImageUrl = string.IsNullOrWhiteSpace(postEditModel.HeroImageUrl) ? null : Helper.SterilizeLink(postEditModel.HeroImageUrl);
        post.InlineCss = postEditModel.InlineCss;

        // compute hash
        var input = $"{post.Slug}#{post.PubDateUtc.GetValueOrDefault():yyyyMMdd}";
        var checkSum = Helper.ComputeCheckSum(input);
        post.HashCheckSum = checkSum;

        // 1. Add new tags to tag lib
        var tags = string.IsNullOrWhiteSpace(postEditModel.Tags) ?
            Array.Empty<string>() :
            postEditModel.Tags.Split(',').ToArray();

        foreach (var item in tags)
        {
            if (!await tagRepo.AnyAsync(p => p.DisplayName == item, ct))
            {
                await tagRepo.AddAsync(new()
                {
                    DisplayName = item,
                    NormalizedName = Tag.NormalizeName(item, Helper.TagNormalizationDictionary)
                }, ct);
            }
        }

        // 2. update tags
        var oldTags = await ptRepository.AsQueryable().Where(pc => pc.PostId == post.Id).ToListAsync();
        await ptRepository.DeleteAsync(oldTags);
        post.Tags.Clear();
        if (tags.Any())
        {
            foreach (var tagName in tags)
            {
                if (!Tag.ValidateName(tagName))
                {
                    continue;
                }

                var tag = await tagRepo.GetAsync(t => t.DisplayName == tagName);
                if (tag is not null) post.Tags.Add(tag);
            }
        }

        // 3. update categories
        var oldpcs = await pcRepository.AsQueryable().Where(pc => pc.PostId == post.Id).ToListAsync();
        await pcRepository.DeleteAsync(oldpcs);

        post.PostCategory.Clear();
        if (postEditModel.SelectedCatIds.Any())
        {
            foreach (var cid in postEditModel.SelectedCatIds)
            {
                post.PostCategory.Add(new()
                {
                    PostId = post.Id,
                    CategoryId = cid
                });
            }
        }

        await postRepo.UpdateAsync(post, ct);

        cache.Remove(CacheDivision.Post, guid.ToString());
        return post;
    }
}