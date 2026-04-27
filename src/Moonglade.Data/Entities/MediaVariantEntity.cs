namespace MoongladePure.Data.Entities;

public class MediaVariantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MediaAssetId { get; set; }
    public string VariantName { get; set; }
    public string ObjectKey { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public long FileSize { get; set; }

    public virtual MediaAssetEntity MediaAsset { get; set; }
}
