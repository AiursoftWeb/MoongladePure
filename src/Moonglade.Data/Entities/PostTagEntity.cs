namespace MoongladePure.Data.Entities;

public class PostTagEntity
{
    public Guid SiteId { get; set; } = SystemIds.DefaultSiteId;
    public Guid PostId { get; set; }
    public int TagId { get; set; }

    public virtual SiteEntity Site { get; set; }
    public virtual PostEntity Post { get; set; }
    public virtual TagEntity Tag { get; set; }
}
