using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MoongladePure.Data.Entities;

public class TagEntity
{
    public int Id { get; set; }
    public string DisplayName { get; set; }
    public string NormalizedName { get; set; }

    public virtual ICollection<PostEntity> Posts { get; set; } = new HashSet<PostEntity>();
}

internal class TagConfiguration : IEntityTypeConfiguration<TagEntity>
{
    public void Configure(EntityTypeBuilder<TagEntity> builder)
    {
        builder.Property(e => e.DisplayName).HasMaxLength(32);
        builder.Property(e => e.NormalizedName).HasMaxLength(32);
    }
}