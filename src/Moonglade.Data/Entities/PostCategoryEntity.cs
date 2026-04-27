namespace MoongladePure.Data.Entities;

public class PostCategoryEntity
{
    public Guid SiteId { get; set; } = SystemIds.DefaultSiteId;
    public Guid PostId { get; set; }
    public Guid CategoryId { get; set; }

    public virtual SiteEntity Site { get; set; }
    public virtual CategoryEntity Category { get; set; }
    public virtual PostEntity Post { get; set; }
}
