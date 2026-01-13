using Microsoft.AspNetCore.Http;
using MoongladePure.Configuration;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;
using MoongladePure.Data.Spec;
using MoongladePure.Utils;

namespace MoongladePure.Syndication;

public interface ISyndicationDataSource
{
    Task<IReadOnlyList<FeedEntry>> GetFeedDataAsync(string catRoute = null);
}

public class SyndicationDataSource : ISyndicationDataSource
{
    private readonly string _baseUrl;
    private readonly IBlogConfig _blogConfig;
    private readonly IRepository<CategoryEntity> _catRepo;
    private readonly IRepository<PostEntity> _postRepo;

    public SyndicationDataSource(
        IBlogConfig blogConfig,
        IHttpContextAccessor httpContextAccessor,
        IRepository<CategoryEntity> catRepo,
        IRepository<PostEntity> postRepo)
    {
        _blogConfig = blogConfig;
        _catRepo = catRepo;
        _postRepo = postRepo;

        var acc = httpContextAccessor;
        _baseUrl = $"{acc.HttpContext?.Request.Scheme}://{acc.HttpContext?.Request.Host}";
    }

    public async Task<IReadOnlyList<FeedEntry>> GetFeedDataAsync(string catRoute = null)
    {
        IReadOnlyList<FeedEntry> itemCollection;
        if (!string.IsNullOrWhiteSpace(catRoute))
        {
            var cat = await _catRepo.GetAsync(c => c.RouteName == catRoute);
            if (cat is null) return null;

            itemCollection = await GetFeedEntriesAsync(cat.Id);
        }
        else
        {
            itemCollection = await GetFeedEntriesAsync();
        }

        return itemCollection;
    }

    private async Task<IReadOnlyList<FeedEntry>> GetFeedEntriesAsync(Guid? catId = null)
    {
        int? top = null;
        if (_blogConfig.FeedSettings.RssItemCount != 0)
        {
            top = _blogConfig.FeedSettings.RssItemCount;
        }

        var postSpec = new PostSpec(catId, top);
        var posts = await _postRepo.SelectAsync(postSpec, p => new
        {
            p.Id,
            p.Title,
            p.PubDateUtc,
            p.Slug,
            Description = _blogConfig.FeedSettings.UseFullContent ? p.RawContent : p.ContentAbstract,
            Categories = p.PostCategory.Select(pc => pc.Category.DisplayName).ToArray()
        });

        // Work around for MySQL issue.
        var list = posts.Select(p =>
        {
            if (p.PubDateUtc is null) return null;
            return new FeedEntry
            {
                Id = p.Id.ToString(),
                Title = p.Title,
                PubDateUtc = p.PubDateUtc.Value,
                Description = p.Description,
                Link = $"{_baseUrl}/post/{p.PubDateUtc.Value.Year}/{p.PubDateUtc.Value.Month}/{p.PubDateUtc.Value.Day}/{p.Slug}",
                Author = _blogConfig.GeneralSettings.OwnerName,
                AuthorEmail = _blogConfig.GeneralSettings.OwnerEmail,
                Categories = p.Categories
            };
        }).Where(p => p is not null).ToList();

        // Workaround EF limitation
        // Man, this is super ugly
        if (_blogConfig.FeedSettings.UseFullContent && list.Any())
        {
            foreach (var simpleFeedItem in list)
            {
                simpleFeedItem.Description = FormatPostContent(simpleFeedItem.Description);
            }
        }

        return list;
    }

    private string FormatPostContent(string rawContent)
    {
        return ContentProcessor.MarkdownToContent(rawContent, ContentProcessor.MarkdownConvertType.Html, false);
    }
}