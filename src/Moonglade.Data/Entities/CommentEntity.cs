namespace MoongladePure.Data.Entities;
// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

public class CommentEntity
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; } = SystemIds.DefaultSiteId;
    public string Username { get; set; }
    public string Email { get; set; }
    public string IPAddress { get; set; }
    public DateTime CreateTimeUtc { get; set; }
    public string CommentContent { get; set; }
    public Guid PostId { get; set; }
    public bool IsApproved { get; set; }
    public CommentSource Source { get; set; } = CommentSource.Visitor;

    public virtual SiteEntity Site { get; set; }
    public virtual PostEntity Post { get; set; }
    public virtual ICollection<CommentReplyEntity> Replies { get; set; } = new HashSet<CommentReplyEntity>();
}

public enum CommentSource
{
    Visitor = 0,
    Admin = 1,
    System = 2,
    AiGenerated = 3
}
