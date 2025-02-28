using Aiursoft.DbTools;
using MoongladePure.Data.Entities;

namespace MoongladePure.Data;

public abstract class BlogDbContext(DbContextOptions options) : DbContext(options), ICanMigrate
{
    public DbSet<CategoryEntity> Category => Set<CategoryEntity>();
    public DbSet<CommentEntity> Comment => Set<CommentEntity>();
    public DbSet<CommentReplyEntity> CommentReply => Set<CommentReplyEntity>();
    public DbSet<PostEntity> Post => Set<PostEntity>();
    public DbSet<PostCategoryEntity> PostCategory => Set<PostCategoryEntity>();
    public DbSet<PostExtensionEntity> PostExtension => Set<PostExtensionEntity>();
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

    public virtual  Task MigrateAsync(CancellationToken cancellationToken) =>
        Database.MigrateAsync(cancellationToken);

    public virtual  Task<bool> CanConnectAsync() =>
        Database.CanConnectAsync();
}

public static class BlogDbContextExtension
{
    public static async Task ClearAllDataAsync(this BlogDbContext context)
    {
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

        await context.SaveChangesAsync();
    }
}
