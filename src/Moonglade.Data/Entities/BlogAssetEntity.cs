namespace MoongladePure.Data.Entities;
// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

public class BlogAssetEntity
{
    public Guid Id { get; set; }

    public Guid SiteId { get; set; } = SystemIds.DefaultSiteId;

    public string Base64Data { get; set; }

    public DateTime LastModifiedTimeUtc { get; set; }

    public virtual SiteEntity Site { get; set; }
}
