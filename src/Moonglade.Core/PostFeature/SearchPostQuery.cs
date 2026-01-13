using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace MoongladePure.Core.PostFeature;

public record SearchPostQuery(string Keyword) : IRequest<IReadOnlyList<PostDigest>>;

public class SearchPostQueryHandler(IRepository<PostEntity> repo)
    : IRequestHandler<SearchPostQuery, IReadOnlyList<PostDigest>>
{
    public async Task<IReadOnlyList<PostDigest>> Handle(SearchPostQuery request, CancellationToken ct)
    {
        if (null == request || string.IsNullOrWhiteSpace(request.Keyword))
        {
            throw new ArgumentNullException(request?.Keyword);
        }

        return await SearchByKeywordAsync(request.Keyword);
    }

    private Task<List<PostDigest>> SearchByKeywordAsync(string keyword)
    {
        // Normalize and split the keyword into terms using one or more whitespace as delimiter.
        var terms = Regex.Split(keyword.Trim(), @"\s+")
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToArray();

        if (terms.Length == 0)
        {
            return Task.FromResult(new List<PostDigest>());
        }

        // Pre-filter posts on the database side:
        // Only include posts that are not deleted and published,
        // and that contain at least one of the search terms in any of the four fields.
        var posts = repo.AsQueryable()
            .Where(p => !p.IsDeleted && p.IsPublished)
            .AsNoTracking()
            .AsEnumerable() // TODO: Translate to original LINQ query
            .Where(p => terms.Any(term =>
                    p.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    p.Tags.Any(tag => tag.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    p.ContentAbstract.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    p.RawContent.Contains(term, StringComparison.OrdinalIgnoreCase)
                // EF.Functions.Like(p.Title, $"%{term}%") ||
                // EF.Functions.Like(p.PostContent, $"%{term}%") ||
                // EF.Functions.Like(p.ContentAbstract, $"%{term}%") ||
                // p.Tags.Any(tag => EF.Functions.Like(tag.DisplayName, $"%{term}%"))
            ))
            .ToList();

        // Define the weights for each field.
        const int titleWeight = 4;
        const int tagWeight = 3;
        const int abstractWeight = 2;
        const int contentWeight = 1;

        // Compute the matching score for each post.
        // Each search term is matched once per field.
        // A term can contribute from multiple fields if it is present in more than one.
        var scoredPosts = posts
            .Select(p =>
            {
                // Compute score for Title (case-insensitive).
                int titleMatches = terms.Count(term =>
                    !string.IsNullOrEmpty(p.Title) &&
                    p.Title.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);

                // Compute score for Tags: count the term once if any tag matches.
                int tagMatches = terms.Count(term =>
                    p.Tags != null && p.Tags.Any(tag =>
                        !string.IsNullOrEmpty(tag.DisplayName) &&
                        tag.DisplayName.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0));

                // Compute score for ContentAbstract.
                int abstractMatches = terms.Count(term =>
                    !string.IsNullOrEmpty(p.ContentAbstract) &&
                    p.ContentAbstract.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);

                // Compute score for RawContent.
                int contentMatches = terms.Count(term =>
                    !string.IsNullOrEmpty(p.RawContent) &&
                    p.RawContent.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);

                // Calculate total score with respective weights.
                int totalScore = (titleMatches * titleWeight)
                                 + (tagMatches * tagWeight)
                                 + (abstractMatches * abstractWeight)
                                 + (contentMatches * contentWeight);

                return new
                {
                    Post = p,
                    TotalScore = totalScore
                };
            })
            .Where(x => x.TotalScore > 0)
            .OrderByDescending(x => x.TotalScore)
            .Select(x => x.Post)
            .ToList();

        return Task.FromResult(scoredPosts.Select(PostDigest.EntitySelector.Compile()).ToList());
    }
}