﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoongladePure.Data.Entities;

namespace MoongladePure.Data.InMemory.Configurations;


internal class LocalAccountConfiguration : IEntityTypeConfiguration<LocalAccountEntity>
{
    public void Configure(EntityTypeBuilder<LocalAccountEntity> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Username).HasMaxLength(32);
        builder.Property(e => e.PasswordHash).HasMaxLength(64);
        builder.Property(e => e.LastLoginIp).HasMaxLength(64);
        builder.Property(e => e.CreateTimeUtc).HasColumnType("datetime");
        builder.Property(e => e.LastLoginTimeUtc).HasColumnType("datetime");
    }
}