namespace MoongladePure.Data.Entities;

public class MediaAssetEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SiteId { get; set; } = SystemIds.DefaultSiteId;
    public Guid? OwnerUserId { get; set; }
    public string Provider { get; set; }
    public string Bucket { get; set; }
    public string ObjectKey { get; set; }
    public string OriginalFileName { get; set; }
    public string PublicUrl { get; set; }
    public string MimeType { get; set; }
    public long FileSize { get; set; }
    public string ContentHash { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public virtual SiteEntity Site { get; set; }
    public virtual LocalAccountEntity OwnerUser { get; set; }
    public virtual ICollection<MediaVariantEntity> Variants { get; set; } = new HashSet<MediaVariantEntity>();
}
