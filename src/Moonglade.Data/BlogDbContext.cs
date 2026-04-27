using Aiursoft.DbTools;
using MoongladePure.Data.Entities;

namespace MoongladePure.Data;

public abstract class BlogDbContext(DbContextOptions options) : DbContext(options), ICanMigrate
{
    public DbSet<TenantEntity> Tenant => Set<TenantEntity>();
    public DbSet<SiteEntity> Site => Set<SiteEntity>();
    public DbSet<SiteDomainEntity> SiteDomain => Set<SiteDomainEntity>();
    public DbSet<SiteMembershipEntity> SiteMembership => Set<SiteMembershipEntity>();
    public DbSet<CategoryEntity> Category => Set<CategoryEntity>();
    public DbSet<CommentEntity> Comment => Set<CommentEntity>();
    public DbSet<CommentReplyEntity> CommentReply => Set<CommentReplyEntity>();
    public DbSet<PostEntity> Post => Set<PostEntity>();
    public DbSet<PostContentEntity> PostContent => Set<PostContentEntity>();
    public DbSet<PostCategoryEntity> PostCategory => Set<PostCategoryEntity>();
    public DbSet<PostExtensionEntity> PostExtension => Set<PostExtensionEntity>();
    public DbSet<PostRouteEntity> PostRoute => Set<PostRouteEntity>();
    public DbSet<PostTagEntity> PostTag => Set<PostTagEntity>();
    public DbSet<TagEntity> Tag => Set<TagEntity>();
    public DbSet<FriendLinkEntity> FriendLink => Set<FriendLinkEntity>();
    public DbSet<PageEntity> CustomPage => Set<PageEntity>();
    public DbSet<MenuEntity> Menu => Set<MenuEntity>();
    public DbSet<SubMenuEntity> SubMenu => Set<SubMenuEntity>();
    public DbSet<LocalAccountEntity> LocalAccount => Set<LocalAccountEntity>();
    public DbSet<BlogThemeEntity> BlogTheme => Set<BlogThemeEntity>();
    public DbSet<BlogAssetEntity> BlogAsset => Set<BlogAssetEntity>();
    public DbSet<BlogConfigurationEntity> BlogConfiguration => Set<BlogConfigurationEntity>();
    public DbSet<MediaAssetEntity> MediaAsset => Set<MediaAssetEntity>();
    public DbSet<MediaVariantEntity> MediaVariant => Set<MediaVariantEntity>();
    public DbSet<AiJobEntity> AiJob => Set<AiJobEntity>();
    public DbSet<AiArtifactEntity> AiArtifact => Set<AiArtifactEntity>();

    public virtual  Task MigrateAsync(CancellationToken cancellationToken) =>
        Database.MigrateAsync(cancellationToken);

    public virtual  Task<bool> CanConnectAsync() =>
        Database.CanConnectAsync();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigurePlatform(modelBuilder);
        ConfigureContent(modelBuilder);
        ConfigureSiteData(modelBuilder);
        ConfigureAi(modelBuilder);
    }

    private static void ConfigurePlatform(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantEntity>(builder =>
        {
            builder.ToTable("Tenant");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Name).IsRequired().HasMaxLength(128);
        });

        modelBuilder.Entity<SiteEntity>(builder =>
        {
            builder.ToTable("Site");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Name).IsRequired().HasMaxLength(128);
            builder.Property(e => e.Slug).IsRequired().HasMaxLength(128);
            builder.Property(e => e.DefaultCulture).IsRequired().HasMaxLength(16);
            builder.Property(e => e.TimeZoneId).IsRequired().HasMaxLength(128);
            builder.HasIndex(e => new { e.TenantId, e.Slug }).IsUnique();
            builder.HasOne(e => e.Tenant).WithMany(e => e.Sites).HasForeignKey(e => e.TenantId);
        });

        modelBuilder.Entity<SiteDomainEntity>(builder =>
        {
            builder.ToTable("SiteDomain");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Host).IsRequired().HasMaxLength(256);
            builder.HasIndex(e => e.Host).IsUnique();
            builder.HasOne(e => e.Site).WithMany(e => e.Domains).HasForeignKey(e => e.SiteId);
        });

        modelBuilder.Entity<LocalAccountEntity>(builder =>
        {
            builder.ToTable("User");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Username).IsRequired().HasMaxLength(32);
            builder.Property(e => e.NormalizedUsername).HasMaxLength(32);
            builder.Property(e => e.Email).HasMaxLength(128);
            builder.Property(e => e.NormalizedEmail).HasMaxLength(128);
            builder.Property(e => e.PasswordHash).HasMaxLength(256);
            builder.Property(e => e.LastLoginIp).HasMaxLength(64);
            builder.HasIndex(e => e.NormalizedUsername).IsUnique();
            builder.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId);
        });

        modelBuilder.Entity<SiteMembershipEntity>(builder =>
        {
            builder.ToTable("SiteMembership");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.DisplayName).HasMaxLength(64);
            builder.HasIndex(e => new { e.SiteId, e.UserId }).IsUnique();
            builder.HasOne(e => e.Site).WithMany().HasForeignKey(e => e.SiteId);
            builder.HasOne(e => e.User).WithMany(e => e.SiteMemberships).HasForeignKey(e => e.UserId);
        });
    }

    private static void ConfigureContent(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PostEntity>(builder =>
        {
            builder.ToTable("Post");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Title).IsRequired().HasMaxLength(128);
            builder.Property(e => e.Slug).IsRequired().HasMaxLength(128);
            builder.Property(e => e.Author).HasMaxLength(64);
            builder.Property(e => e.ContentAbstractZh).HasMaxLength(1024);
            builder.Property(e => e.ContentAbstractEn).HasMaxLength(1024);
            builder.Property(e => e.ContentLanguageCode).HasMaxLength(16);
            builder.Property(e => e.OriginLink).HasMaxLength(512);
            builder.Property(e => e.HeroImageUrl).HasMaxLength(512);
            builder.Property(e => e.InlineCss).HasMaxLength(2048);
            builder.HasIndex(e => new { e.SiteId, e.IsDeleted, e.IsPublished, e.PubDateUtc });
            builder.HasIndex(e => new { e.SiteId, e.IsFeatured, e.PubDateUtc });
            builder.HasOne(e => e.Site).WithMany().HasForeignKey(e => e.SiteId);
        });

        modelBuilder.Entity<PostContentEntity>(builder =>
        {
            builder.ToTable("PostContent");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.CultureCode).HasMaxLength(16);
            builder.Property(e => e.Abstract).HasMaxLength(2048);
            builder.HasIndex(e => new { e.SiteId, e.PostId, e.CultureCode, e.ContentKind });
            builder.HasOne(e => e.Site).WithMany().HasForeignKey(e => e.SiteId);
            builder.HasOne(e => e.Post).WithMany(e => e.Contents).HasForeignKey(e => e.PostId);
        });

        modelBuilder.Entity<PostRouteEntity>(builder =>
        {
            builder.ToTable("PostRoute");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Slug).IsRequired().HasMaxLength(128);
            builder.HasIndex(e => new { e.SiteId, e.RouteDate, e.Slug }).IsUnique();
            builder.HasIndex(e => new { e.SiteId, e.HashCheckSum });
            builder.HasOne(e => e.Site).WithMany().HasForeignKey(e => e.SiteId);
            builder.HasOne(e => e.Post).WithMany(e => e.Routes).HasForeignKey(e => e.PostId);
        });

        modelBuilder.Entity<PostExtensionEntity>(builder =>
        {
            builder.ToTable("PostMetric");
            builder.HasKey(e => e.PostId);
            builder.Property(e => e.PostId).ValueGeneratedNever();
            builder.HasOne(e => e.Site).WithMany().HasForeignKey(e => e.SiteId);
            builder.HasOne(e => e.Post).WithOne(e => e.PostExtension).HasForeignKey<PostExtensionEntity>(e => e.PostId);
        });

        modelBuilder.Entity<CategoryEntity>(builder =>
        {
            builder.ToTable("Category");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.DisplayName).IsRequired().HasMaxLength(64);
            builder.Property(e => e.Note).HasMaxLength(128);
            builder.Property(e => e.RouteName).IsRequired().HasMaxLength(64);
            builder.HasIndex(e => new { e.SiteId, e.RouteName }).IsUnique();
            builder.HasOne(e => e.Site).WithMany().HasForeignKey(e => e.SiteId);
        });

        modelBuilder.Entity<TagEntity>(builder =>
        {
            builder.ToTable("Tag");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.DisplayName).IsRequired().HasMaxLength(32);
            builder.Property(e => e.NormalizedName).IsRequired().HasMaxLength(32);
            builder.HasIndex(e => new { e.SiteId, e.NormalizedName }).IsUnique();
            builder.HasOne(e => e.Site).WithMany().HasForeignKey(e => e.SiteId);
        });

        modelBuilder.Entity<PostCategoryEntity>(builder =>
        {
            builder.ToTable("PostCategory");
            builder.HasKey(e => new { e.PostId, e.CategoryId });
            builder.HasOne(e => e.Site).WithMany().HasForeignKey(e => e.SiteId);
            builder.HasOne(e => e.Post).WithMany(e => e.PostCategory).HasForeignKey(e => e.PostId);
            builder.HasOne(e => e.Category).WithMany(e => e.PostCategory).HasForeignKey(e => e.CategoryId);
        });

        modelBuilder.Entity<PostTagEntity>(builder =>
        {
            builder.ToTable("PostTag");
            builder.HasKey(e => new { e.PostId, e.TagId });
            builder.HasOne(e => e.Site).WithMany().HasForeignKey(e => e.SiteId);
            builder.HasOne(e => e.Post).WithMany().HasForeignKey(e => e.PostId);
            builder.HasOne(e => e.Tag).WithMany().HasForeignKey(e => e.TagId);
        });

        modelBuilder.Entity<PostEntity>()
            .HasMany(p => p.Tags)
            .WithMany(p => p.Posts)
            .UsingEntity<PostTagEntity>();

        modelBuilder.Entity<PageEntity>(builder =>
        {
            builder.ToTable("Page");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Title).IsRequired().HasMaxLength(128);
            builder.Property(e => e.Slug).IsRequired().HasMaxLength(128);
            builder.Property(e => e.MetaDescription).HasMaxLength(256);
            builder.HasIndex(e => new { e.SiteId, e.Slug }).IsUnique();
            builder.HasOne(e => e.Site).WithMany().HasForeignKey(e => e.SiteId);
        });
    }

    private static void ConfigureSiteData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CommentEntity>(builder =>
        {
            builder.ToTable("Comment");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.CommentContent).IsRequired();
            builder.Property(e => e.Email).HasMaxLength(128);
            builder.Property(e => e.IPAddress).HasMaxLength(64);
            builder.Property(e => e.Username).HasMaxLength(64);
            builder.HasIndex(e => new { e.SiteId, e.PostId, e.CreateTimeUtc });
            builder.HasOne(e => e.Site).WithMany().HasForeignKey(e => e.SiteId);
            builder.HasOne(e => e.Post).WithMany(e => e.Comments).HasForeignKey(e => e.PostId);
        });

        modelBuilder.Entity<CommentReplyEntity>(builder =>
        {
            builder.ToTable("CommentReply");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.HasOne(e => e.Site).WithMany().HasForeignKey(e => e.SiteId);
            builder.HasOne(e => e.Comment).WithMany(e => e.Replies).HasForeignKey(e => e.CommentId);
        });

        modelBuilder.Entity<MenuEntity>(builder =>
        {
            builder.ToTable("Menu");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Title).IsRequired().HasMaxLength(64);
            builder.Property(e => e.Url).HasMaxLength(256);
            builder.Property(e => e.Icon).HasMaxLength(64);
            builder.HasIndex(e => new { e.SiteId, e.DisplayOrder });
            builder.HasOne(e => e.Site).WithMany().HasForeignKey(e => e.SiteId);
        });

        modelBuilder.Entity<SubMenuEntity>(builder =>
        {
            builder.ToTable("SubMenu");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Title).IsRequired().HasMaxLength(64);
            builder.Property(e => e.Url).HasMaxLength(256);
            builder.HasOne(e => e.Site).WithMany().HasForeignKey(e => e.SiteId);
            builder.HasOne(e => e.Menu).WithMany(e => e.SubMenus).HasForeignKey(e => e.MenuId);
        });

        modelBuilder.Entity<FriendLinkEntity>(builder =>
        {
            builder.ToTable("FriendLink");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Title).IsRequired().HasMaxLength(64);
            builder.Property(e => e.LinkUrl).IsRequired().HasMaxLength(512);
            builder.HasOne(e => e.Site).WithMany().HasForeignKey(e => e.SiteId);
        });

        modelBuilder.Entity<BlogConfigurationEntity>(builder =>
        {
            builder.ToTable("SiteSetting");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.CfgKey).IsRequired().HasMaxLength(64);
            builder.HasIndex(e => new { e.SiteId, e.CfgKey }).IsUnique();
            builder.HasOne(e => e.Site).WithMany().HasForeignKey(e => e.SiteId);
        });

        modelBuilder.Entity<BlogThemeEntity>(builder =>
        {
            builder.ToTable("Theme");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.ThemeName).IsRequired().HasMaxLength(32);
            builder.HasIndex(e => new { e.SiteId, e.ThemeName }).IsUnique();
            builder.HasOne(e => e.Site).WithMany().HasForeignKey(e => e.SiteId);
        });

        modelBuilder.Entity<BlogAssetEntity>(builder =>
        {
            builder.ToTable("SiteBinaryAsset");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.HasOne(e => e.Site).WithMany().HasForeignKey(e => e.SiteId);
        });

        modelBuilder.Entity<MediaAssetEntity>(builder =>
        {
            builder.ToTable("MediaAsset");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Provider).IsRequired().HasMaxLength(64);
            builder.Property(e => e.Bucket).HasMaxLength(128);
            builder.Property(e => e.ObjectKey).IsRequired().HasMaxLength(512);
            builder.Property(e => e.OriginalFileName).HasMaxLength(256);
            builder.Property(e => e.PublicUrl).HasMaxLength(1024);
            builder.Property(e => e.MimeType).HasMaxLength(128);
            builder.Property(e => e.ContentHash).HasMaxLength(128);
            builder.HasIndex(e => new { e.SiteId, e.ContentHash });
            builder.HasOne(e => e.Site).WithMany().HasForeignKey(e => e.SiteId);
            builder.HasOne(e => e.OwnerUser).WithMany().HasForeignKey(e => e.OwnerUserId);
        });

        modelBuilder.Entity<MediaVariantEntity>(builder =>
        {
            builder.ToTable("MediaVariant");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.VariantName).IsRequired().HasMaxLength(64);
            builder.Property(e => e.ObjectKey).IsRequired().HasMaxLength(512);
            builder.HasOne(e => e.MediaAsset).WithMany(e => e.Variants).HasForeignKey(e => e.MediaAssetId);
        });
    }

    private static void ConfigureAi(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AiJobEntity>(builder =>
        {
            builder.ToTable("AiJob");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.TargetEntityType).IsRequired().HasMaxLength(64);
            builder.Property(e => e.Provider).HasMaxLength(64);
            builder.Property(e => e.Model).HasMaxLength(128);
            builder.Property(e => e.ErrorMessage).HasMaxLength(2048);
            builder.HasIndex(e => new { e.SiteId, e.Status, e.JobType, e.CreatedAtUtc });
            builder.HasOne(e => e.Site).WithMany().HasForeignKey(e => e.SiteId);
            builder.HasOne(e => e.RequestedByUser).WithMany().HasForeignKey(e => e.RequestedByUserId);
        });

        modelBuilder.Entity<AiArtifactEntity>(builder =>
        {
            builder.ToTable("AiArtifact");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.TargetEntityType).IsRequired().HasMaxLength(64);
            builder.Property(e => e.CultureCode).HasMaxLength(16);
            builder.HasIndex(e => new { e.SiteId, e.TargetEntityType, e.TargetEntityId, e.ArtifactType });
            builder.HasOne(e => e.Site).WithMany().HasForeignKey(e => e.SiteId);
            builder.HasOne(e => e.Job).WithMany(e => e.Artifacts).HasForeignKey(e => e.JobId);
        });
    }

}

public static class BlogDbContextExtension
{
    public static async Task ClearAllDataAsync(this BlogDbContext context)
    {
        context.AiArtifact.RemoveRange();
        context.AiJob.RemoveRange();
        context.MediaVariant.RemoveRange();
        context.MediaAsset.RemoveRange();
        context.PostRoute.RemoveRange();
        context.PostContent.RemoveRange();
        context.PostTag.RemoveRange();
        context.PostCategory.RemoveRange();
        context.CommentReply.RemoveRange();
        context.Category.RemoveRange();
        context.Tag.RemoveRange();
        context.Comment.RemoveRange();
        context.FriendLink.RemoveRange();
        context.PostExtension.RemoveRange();
        context.Post.RemoveRange();
        context.Menu.RemoveRange();
        context.BlogConfiguration.RemoveRange();
        context.BlogAsset.RemoveRange();
        context.BlogTheme.RemoveRange();
        context.LocalAccount.RemoveRange();
        context.SiteDomain.RemoveRange();
        context.SiteMembership.RemoveRange();
        context.Site.RemoveRange();
        context.Tenant.RemoveRange();

        await context.SaveChangesAsync();
    }
}
