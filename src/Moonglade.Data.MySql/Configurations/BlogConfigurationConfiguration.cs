using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoongladePure.Data.Entities;

namespace MoongladePure.Data.MySql.Configurations;

internal class BlogConfigurationConfiguration : IEntityTypeConfiguration<BlogConfigurationEntity>
{
    public void Configure(EntityTypeBuilder<BlogConfigurationEntity> builder)
    {
        builder.Property(e => e.LastModifiedTimeUtc).HasColumnType("datetime");
    }
}