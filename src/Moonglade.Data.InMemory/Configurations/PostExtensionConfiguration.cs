﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoongladePure.Data.Entities;

namespace MoongladePure.Data.InMemory.Configurations;


internal class PostExtensionConfiguration : IEntityTypeConfiguration<PostExtensionEntity>
{
    public void Configure(EntityTypeBuilder<PostExtensionEntity> builder)
    {
        builder.HasKey(e => e.PostId);
        builder.Property(e => e.PostId).ValueGeneratedNever();

        builder.HasOne(d => d.Post)
            .WithOne(p => p.PostExtension)
            .HasForeignKey<PostExtensionEntity>(d => d.PostId)
            .HasConstraintName("FK_PostExtension_Post");
    }
}