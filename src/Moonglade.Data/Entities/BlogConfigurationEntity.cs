using Microsoft.EntityFrameworkCore.Metadata.Builders;
// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

namespace MoongladePure.Data.Entities;

public class BlogConfigurationEntity
{
    public int Id { get; set; }

    public Guid SiteId { get; set; } = SystemIds.DefaultSiteId;

    public string CfgKey { get; set; }

    public string CfgValue { get; set; }

    public int SchemaVersion { get; set; } = 1;

    public DateTime? LastModifiedTimeUtc { get; set; }

    public virtual SiteEntity Site { get; set; }
}


internal class BlogConfigurationConfiguration : IEntityTypeConfiguration<BlogConfigurationEntity>
{
    public void Configure(EntityTypeBuilder<BlogConfigurationEntity> builder)
    {
        builder.Property(e => e.CfgKey).HasMaxLength(64);
    }
}
