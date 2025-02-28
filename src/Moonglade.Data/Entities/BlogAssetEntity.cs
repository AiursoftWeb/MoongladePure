namespace MoongladePure.Data.Entities;
// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

public class BlogAssetEntity
{
    public Guid Id { get; set; }

    public string Base64Data { get; set; }

    public DateTime LastModifiedTimeUtc { get; set; }
}
