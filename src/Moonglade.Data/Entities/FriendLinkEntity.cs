using Microsoft.EntityFrameworkCore.Metadata.Builders;
// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

namespace MoongladePure.Data.Entities;

public class FriendLinkEntity
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; } = SystemIds.DefaultSiteId;

    public string Title { get; set; }

    public string LinkUrl { get; set; }

    public virtual SiteEntity Site { get; set; }
}

internal class FriendLinkConfiguration : IEntityTypeConfiguration<FriendLinkEntity>
{
    public void Configure(EntityTypeBuilder<FriendLinkEntity> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Title).HasMaxLength(64);
        builder.Property(e => e.LinkUrl).HasMaxLength(256);
    }
}
