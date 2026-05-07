using Microsoft.EntityFrameworkCore;

namespace MoongladePure.Core.PostFeature;

internal static class PostReadProjection
{
    public static void Apply(Post post, IReadOnlyList<PostContentEntity> contents, IReadOnlyList<AiArtifactEntity> artifacts)
    {
        if (post is null)
        {
            return;
        }

        post.RawPostContent = FindContentBody(contents, PostContentKind.RawMarkdown, post.ContentLanguageCode, post.RawPostContent);
        post.ContentAbstractZh = FindSummary(contents, artifacts, "zh-CN", post.ContentAbstractZh);
        post.ContentAbstractEn = FindSummary(contents, artifacts, "en-US", post.ContentAbstractEn);
        post.LocalizedChineseContent = FindTranslation(contents, artifacts, "zh-CN", post.LocalizedChineseContent);
        post.LocalizedEnglishContent = FindTranslation(contents, artifacts, "en-US", post.LocalizedEnglishContent);
    }

    public static void Apply(PostDigest post, IReadOnlyList<PostContentEntity> contents, IReadOnlyList<AiArtifactEntity> artifacts)
    {
        post.ContentAbstractZh = FindSummary(contents, artifacts, "zh-CN", post.ContentAbstractZh);
        post.ContentAbstractEn = FindSummary(contents, artifacts, "en-US", post.ContentAbstractEn);
    }

    public static async Task EnrichAsync(
        IReadOnlyList<PostDigest> posts,
        IRepository<PostContentEntity> contentRepo,
        IRepository<AiArtifactEntity> artifactRepo,
        Guid siteId,
        CancellationToken ct)
    {
        var postIds = posts.Select(p => p.Id).ToArray();
        if (postIds.Length == 0)
        {
            return;
        }

        var contents = await contentRepo.AsQueryable()
            .AsNoTracking()
            .Where(content => content.SiteId == siteId && postIds.Contains(content.PostId))
            .ToListAsync(ct);
        var artifacts = await artifactRepo.AsQueryable()
            .AsNoTracking()
            .Where(artifact =>
                artifact.SiteId == siteId &&
                artifact.TargetEntityType == nameof(PostEntity) &&
                postIds.Contains(artifact.TargetEntityId))
            .ToListAsync(ct);

        foreach (var post in posts)
        {
            Apply(
                post,
                contents.Where(content => content.PostId == post.Id).ToList(),
                artifacts.Where(artifact => artifact.TargetEntityId == post.Id).ToList());
        }
    }

    public static async Task EnrichAsync(
        Post post,
        IRepository<PostContentEntity> contentRepo,
        IRepository<AiArtifactEntity> artifactRepo,
        Guid siteId,
        CancellationToken ct)
    {
        if (post is null)
        {
            return;
        }

        var contents = await contentRepo.AsQueryable()
            .AsNoTracking()
            .Where(content => content.SiteId == siteId && content.PostId == post.Id)
            .ToListAsync(ct);
        var artifacts = await artifactRepo.AsQueryable()
            .AsNoTracking()
            .Where(artifact =>
                artifact.SiteId == siteId &&
                artifact.TargetEntityType == nameof(PostEntity) &&
                artifact.TargetEntityId == post.Id)
            .ToListAsync(ct);

        Apply(post, contents, artifacts);
    }

    private static string FindContentBody(IReadOnlyList<PostContentEntity> contents, PostContentKind kind, string cultureCode, string fallback) =>
        contents
            .Where(content => content.ContentKind == kind && !string.IsNullOrWhiteSpace(content.Body))
            .OrderByDescending(content => content.IsOriginal)
            .ThenByDescending(content => string.Equals(content.CultureCode, cultureCode, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(content => content.UpdatedAtUtc ?? content.CreatedAtUtc)
            .Select(content => content.Body)
            .FirstOrDefault() ?? fallback;

    private static string FindSummary(
        IReadOnlyList<PostContentEntity> contents,
        IReadOnlyList<AiArtifactEntity> artifacts,
        string cultureCode,
        string fallback) =>
        FindArtifact(artifacts, AiArtifactType.Summary, cultureCode) ??
        FindContentAbstract(contents, PostContentKind.Summary, cultureCode) ??
        fallback;

    private static string FindTranslation(
        IReadOnlyList<PostContentEntity> contents,
        IReadOnlyList<AiArtifactEntity> artifacts,
        string cultureCode,
        string fallback) =>
        FindContentBody(contents.Where(content => IsCulture(content, cultureCode)).ToList(), PostContentKind.Translation, cultureCode, null) ??
        FindArtifact(artifacts, AiArtifactType.Translation, cultureCode) ??
        fallback;

    private static string FindContentAbstract(IReadOnlyList<PostContentEntity> contents, PostContentKind kind, string cultureCode) =>
        contents
            .Where(content =>
                content.ContentKind == kind &&
                IsCulture(content, cultureCode) &&
                !string.IsNullOrWhiteSpace(content.Abstract))
            .OrderByDescending(content => content.UpdatedAtUtc ?? content.CreatedAtUtc)
            .Select(content => content.Abstract)
            .FirstOrDefault();

    private static string FindArtifact(IReadOnlyList<AiArtifactEntity> artifacts, AiArtifactType type, string cultureCode) =>
        artifacts
            .Where(artifact =>
                artifact.ArtifactType == type &&
                IsCulture(artifact, cultureCode) &&
                !string.IsNullOrWhiteSpace(artifact.Content))
            .OrderByDescending(artifact => artifact.CreatedAtUtc)
            .Select(artifact => artifact.Content)
            .FirstOrDefault();

    private static bool IsCulture(PostContentEntity content, string cultureCode) =>
        string.Equals(content.CultureCode, cultureCode, StringComparison.OrdinalIgnoreCase);

    private static bool IsCulture(AiArtifactEntity artifact, string cultureCode) =>
        string.Equals(artifact.CultureCode, cultureCode, StringComparison.OrdinalIgnoreCase);
}
