namespace MoongladePure.Data.Entities;

public class AiArtifactEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SiteId { get; set; } = SystemIds.DefaultSiteId;
    public Guid? JobId { get; set; }
    public string TargetEntityType { get; set; }
    public Guid TargetEntityId { get; set; }
    public AiArtifactType ArtifactType { get; set; }
    public string CultureCode { get; set; }
    public string Content { get; set; }
    public string MetadataJson { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public virtual SiteEntity Site { get; set; }
    public virtual AiJobEntity Job { get; set; }
}

public enum AiArtifactType
{
    Summary = 0,
    Translation = 1,
    Comment = 2,
    Tags = 3,
    Question = 4,
    Answer = 5,
    ImagePrompt = 6,
    GeneratedImage = 7
}
