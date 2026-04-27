namespace MoongladePure.Data.Entities;

public class PostRouteEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SiteId { get; set; } = SystemIds.DefaultSiteId;
    public Guid PostId { get; set; }
    public DateTime RouteDate { get; set; }
    public string Slug { get; set; }
    public int HashCheckSum { get; set; }
    public bool IsCanonical { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public virtual SiteEntity Site { get; set; }
    public virtual PostEntity Post { get; set; }
}
