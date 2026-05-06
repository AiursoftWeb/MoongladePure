using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Data.Spec;

public sealed class PostSpec : BaseSpecification<PostEntity>
{
    public PostSpec(Guid? categoryId, int? top = null, Guid? siteId = null) :
        base(p => p.SiteId == (siteId ?? SystemIds.DefaultSiteId) &&
                  !p.IsDeleted &&
                  p.IsPublished &&
                  p.IsFeedIncluded &&
                  (categoryId == null || p.PostCategory.Any(c => c.CategoryId == categoryId.Value)))
    {
        ApplyOrderByDescending(p => p.PubDateUtc);

        if (top.HasValue)
        {
            ApplyPaging(0, top.Value);
        }
    }

    public PostSpec(int year, int month = 0, Guid? siteId = null) :
        base(p => p.SiteId == (siteId ?? SystemIds.DefaultSiteId) &&
                  p.PubDateUtc.Value.Year == year &&
                  (month == 0 || p.PubDateUtc.Value.Month == month))
    {
        // Fix #313: Filter out unpublished posts
        AddCriteria(p => p.IsPublished && !p.IsDeleted);

        ApplyOrderByDescending(p => p.PubDateUtc);
    }

    public PostSpec(string slug, DateTime pubDateUtc, Guid? siteId = null) :
        base(p =>
        p.SiteId == (siteId ?? SystemIds.DefaultSiteId) &&
        p.Slug == slug &&
        p.PubDateUtc != null
        && p.PubDateUtc.Value.Year == pubDateUtc.Year
        && p.PubDateUtc.Value.Month == pubDateUtc.Month
        && p.PubDateUtc.Value.Day == pubDateUtc.Day)
    {

    }

    public PostSpec(int hashCheckSum, Guid? siteId = null)
        : base(p => p.SiteId == (siteId ?? SystemIds.DefaultSiteId) && p.HashCheckSum == hashCheckSum && p.IsPublished && !p.IsDeleted)
    {
        AddInclude(post => post
            .Include(p => p.PostExtension)
            .Include(p => p.Comments)
            .Include(pt => pt.Tags)
            .Include(p => p.PostCategory).ThenInclude(pc => pc.Category));
    }

    public PostSpec(DateTime date, string slug, Guid? siteId = null)
        : base(p => p.SiteId == (siteId ?? SystemIds.DefaultSiteId) &&
                    p.Slug == slug &&
                    p.IsPublished &&
                    p.PubDateUtc.Value.Date == date &&
                    !p.IsDeleted)
    {
        AddInclude(post => post
            .Include(p => p.PostExtension)
            .Include(p => p.Comments)
            .Include(pt => pt.Tags)
            .Include(p => p.PostCategory).ThenInclude(pc => pc.Category));
    }

    public PostSpec(Guid id, bool includeRelatedData = true, Guid? siteId = null)
        : base(p => p.SiteId == (siteId ?? SystemIds.DefaultSiteId) && p.Id == id)
    {
        if (includeRelatedData)
        {
            AddInclude(post => post
                .Include(p => p.Tags)
                .Include(p => p.PostCategory)
                .ThenInclude(pc => pc.Category));
        }
    }

    public PostSpec(PostStatus status, Guid? siteId = null)
    {
        AddCriteria(p => p.SiteId == (siteId ?? SystemIds.DefaultSiteId));

        switch (status)
        {
            case PostStatus.Draft:
                AddCriteria(p => !p.IsPublished && !p.IsDeleted);
                break;
            case PostStatus.Published:
                AddCriteria(p => p.IsPublished && !p.IsDeleted);
                break;
            case PostStatus.Deleted:
                AddCriteria(p => p.IsDeleted);
                break;
            case PostStatus.Default:
                AddCriteria(p => true);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }
    }

    public PostSpec(bool isDeleted, Guid? siteId = null) :
        base(p => p.SiteId == (siteId ?? SystemIds.DefaultSiteId) && p.IsDeleted == isDeleted)
    {

    }

    public PostSpec(Guid? siteId = null) :
        base(p => p.SiteId == (siteId ?? SystemIds.DefaultSiteId) && p.IsDeleted)
    {

    }
}
