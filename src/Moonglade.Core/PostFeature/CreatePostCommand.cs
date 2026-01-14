using Microsoft.Extensions.Logging;
using MoongladePure.Core.TagFeature;
using MoongladePure.Utils;

namespace MoongladePure.Core.PostFeature;

public record CreatePostCommand(PostEditModel Payload) : IRequest<PostEntity>;

public class CreatePostCommandHandler(
    IRepository<PostEntity> postRepo,
    ILogger<CreatePostCommandHandler> logger,
    IRepository<TagEntity> tagRepo)
    : IRequestHandler<CreatePostCommand, PostEntity>
{
    public async Task<PostEntity> Handle(CreatePostCommand request, CancellationToken ct)
    {
        var post = new PostEntity
        {
            CommentEnabled = request.Payload.EnableComment,
            Id = Guid.NewGuid(),
            RawContent = request.Payload.EditorContent,
            ContentAbstractZh = "...",
            ContentAbstractEn = "...",
            CreateTimeUtc = DateTime.UtcNow,
            LastModifiedUtc = DateTime.UtcNow, // Fix draft orders
            Slug = request.Payload.Slug.ToLower().Trim(),
            Author = request.Payload.Author?.Trim(),
            Title = request.Payload.Title.Trim(),
            ContentLanguageCode = request.Payload.LanguageCode,
            IsFeedIncluded = request.Payload.FeedIncluded,
            PubDateUtc = request.Payload.IsPublished ? DateTime.UtcNow : null,
            IsDeleted = false,
            IsPublished = request.Payload.IsPublished,
            IsFeatured = request.Payload.Featured,
            IsOriginal = string.IsNullOrWhiteSpace(request.Payload.OriginLink),
            OriginLink = string.IsNullOrWhiteSpace(request.Payload.OriginLink) ? null : Helper.SterilizeLink(request.Payload.OriginLink),
            HeroImageUrl = string.IsNullOrWhiteSpace(request.Payload.HeroImageUrl) ? null : Helper.SterilizeLink(request.Payload.HeroImageUrl),
            InlineCss = request.Payload.InlineCss,
            PostExtension = new()
            {
                Hits = 0,
                Likes = 0
            }
        };

        // --- FIX START ---
        // check if exist same slug under the same day
        // We use a Range comparison (>= and <) instead of PostSpec.
        // PostSpec likely uses .Date == .Date which crashes the Pomelo NullabilityProcessor
        // on nullable DateTime columns.
        var todayUtc = DateTime.UtcNow.Date;
        var tomorrowUtc = todayUtc.AddDays(1);

        // Assuming postRepo.AnyAsync accepts an Expression<Func<PostEntity, bool>>
        // If it strictly requires ISpecification, you will need to create a new PostDateRangeSpec
        var slugToCheck = post.Slug;
        var isDuplicate = await postRepo.AnyAsync(p =>
            p.Slug == slugToCheck &&
            p.PubDateUtc >= todayUtc &&
            p.PubDateUtc < tomorrowUtc, ct);

        if (isDuplicate)
        {
            var uid = Guid.NewGuid();
            post.Slug += $"-{uid.ToString().ToLower()[..8]}";
            logger.LogInformation("Found conflict for post slug, generated new slug: {PostSlug}", post.Slug);
        }
        // --- FIX END ---

        // compute hash
        var input = $"{post.Slug}#{post.PubDateUtc.GetValueOrDefault():yyyyMMdd}";
        var checkSum = Helper.ComputeCheckSum(input);
        post.HashCheckSum = checkSum;

        // add categories
        if (request.Payload.SelectedCatIds is { Length: > 0 })
        {
            foreach (var id in request.Payload.SelectedCatIds)
            {
                post.PostCategory.Add(new()
                {
                    CategoryId = id,
                    PostId = post.Id
                });
            }
        }

        // add tags
        var tags = string.IsNullOrWhiteSpace(request.Payload.Tags) ?
            Array.Empty<string>() :
            request.Payload.Tags.Split(',');

        if (tags is { Length: > 0 })
        {
            foreach (var item in tags)
            {
                if (!Tag.ValidateName(item)) continue;

                var tag = await tagRepo.GetAsync(q => q.DisplayName == item) ?? await CreateTag(item);
                post.Tags.Add(tag);
            }
        }

        await postRepo.AddAsync(post, ct);

        return post;
    }

    private async Task<TagEntity> CreateTag(string item)
    {
        var newTag = new TagEntity
        {
            DisplayName = item,
            NormalizedName = Tag.NormalizeName(item, Helper.TagNormalizationDictionary)
        };

        var tag = await tagRepo.AddAsync(newTag);
        return tag;
    }
}
