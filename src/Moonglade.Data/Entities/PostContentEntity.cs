namespace MoongladePure.Data.Entities;

public class PostContentEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SiteId { get; set; } = SystemIds.DefaultSiteId;
    public Guid PostId { get; set; }
    public string CultureCode { get; set; }
    public PostContentKind ContentKind { get; set; }
    public string Body { get; set; }
    public string Abstract { get; set; }
    public bool IsOriginal { get; set; }
    public AiGeneratedBy GeneratedBy { get; set; } = AiGeneratedBy.None;
    public Guid? GenerationId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public virtual SiteEntity Site { get; set; }
    public virtual PostEntity Post { get; set; }
}

public enum PostContentKind
{
    RawMarkdown = 0,
    Translation = 1,
    Summary = 2,
    RenderedHtml = 3
}

public enum AiGeneratedBy
{
    None = 0,
    Ai = 1,
    HumanEditedAi = 2
}
