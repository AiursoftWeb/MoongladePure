namespace MoongladePure.Data.Entities;

public class AiJobEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SiteId { get; set; } = SystemIds.DefaultSiteId;
    public AiJobType JobType { get; set; }
    public string TargetEntityType { get; set; }
    public Guid TargetEntityId { get; set; }
    public string Provider { get; set; }
    public string Model { get; set; }
    public AiJobStatus Status { get; set; } = AiJobStatus.Pending;
    public Guid? RequestedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }
    public string ErrorMessage { get; set; }

    public virtual SiteEntity Site { get; set; }
    public virtual LocalAccountEntity RequestedByUser { get; set; }
    public virtual ICollection<AiArtifactEntity> Artifacts { get; set; } = new HashSet<AiArtifactEntity>();
}

public enum AiJobType
{
    Summary = 0,
    Translation = 1,
    Comment = 2,
    Tags = 3,
    QuestionAnswer = 4,
    ImageGeneration = 5,
    LanguageDetection = 6
}

public enum AiJobStatus
{
    Pending = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3,
    Cancelled = 4
}
