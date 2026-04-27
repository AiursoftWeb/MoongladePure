
namespace MoongladePure.Data.Entities;

public class PostExtensionEntity
{
    public Guid SiteId { get; set; } = SystemIds.DefaultSiteId;
    public Guid PostId { get; set; }
    public int Hits { get; set; }
    public int Likes { get; set; }

    public virtual SiteEntity Site { get; set; }
    public virtual PostEntity Post { get; set; }
}
