using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MoongladePure.Data.Entities;

public class CategoryEntity
{
    public Guid Id { get; set; }
    public string RouteName { get; set; }
    public string DisplayName { get; set; }
    public string Note { get; set; }

    public virtual ICollection<PostCategoryEntity> PostCategory { get; set; } = new HashSet<PostCategoryEntity>();
}

internal class CategoryConfiguration : IEntityTypeConfiguration<CategoryEntity>
{
    public void Configure(EntityTypeBuilder<CategoryEntity> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.DisplayName).HasMaxLength(64);
        builder.Property(e => e.Note).HasMaxLength(128);
        builder.Property(e => e.RouteName).HasMaxLength(64);
    }
}