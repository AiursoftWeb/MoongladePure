namespace MoongladePure.Data.Entities;
// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

public class PostEntity
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Slug { get; set; }
    public string Author { get; set; }
    public string RawContent { get; set; }
    public string LocalizedChineseContent { get; set; }
    public string LocalizedEnglishContent { get; set; }
    public bool CommentEnabled { get; set; }
    public DateTime CreateTimeUtc { get; set; }
    public string ContentAbstract { get; set; }
    public string ContentLanguageCode { get; set; }
    public bool IsFeedIncluded { get; set; }
    public DateTime? LocalizeJobRunAt { get; set; }
    public DateTime? PubDateUtc { get; set; }
    public DateTime? LastModifiedUtc { get; set; }
    public bool IsPublished { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsOriginal { get; set; }
    public string OriginLink { get; set; }
    public string HeroImageUrl { get; set; }
    public string InlineCss { get; set; }
    public bool IsFeatured { get; set; }
    public int HashCheckSum { get; set; }

    public virtual PostExtensionEntity PostExtension { get; set; }
    public virtual ICollection<CommentEntity> Comments { get; set; } = new HashSet<CommentEntity>();
    public virtual ICollection<PostCategoryEntity> PostCategory { get; set; } = new HashSet<PostCategoryEntity>();
    public virtual ICollection<TagEntity> Tags { get; set; } = new HashSet<TagEntity>();
}
