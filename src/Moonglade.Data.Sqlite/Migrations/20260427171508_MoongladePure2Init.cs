using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoongladePure.Data.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class MoongladePure2Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenant", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Site",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    DefaultCulture = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    TimeZoneId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Site", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Site_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    NormalizedUsername = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    PasswordSalt = table.Column<string>(type: "TEXT", nullable: true),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    LastLoginTimeUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastLoginIp = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    CreateTimeUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                    table.ForeignKey(
                        name: "FK_User_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Category",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RouteName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Category", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Category_Site_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Site",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FriendLink",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    LinkUrl = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FriendLink", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FriendLink_Site_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Site",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Menu",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsOpenInNewTab = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Menu", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Menu_Site_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Site",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Page",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    MetaDescription = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    HtmlContent = table.Column<string>(type: "TEXT", nullable: true),
                    CssContent = table.Column<string>(type: "TEXT", nullable: true),
                    HideSidebar = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsPublished = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreateTimeUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdateTimeUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Page", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Page_Site_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Site",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Post",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Author = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    RawContent = table.Column<string>(type: "TEXT", nullable: true),
                    LocalizedChineseContent = table.Column<string>(type: "TEXT", nullable: true),
                    LocalizedEnglishContent = table.Column<string>(type: "TEXT", nullable: true),
                    CommentEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreateTimeUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ContentAbstractZh = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    ContentAbstractEn = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    ContentLanguageCode = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    IsFeedIncluded = table.Column<bool>(type: "INTEGER", nullable: false),
                    LocalizeJobRunAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PubDateUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastModifiedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsPublished = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsOriginal = table.Column<bool>(type: "INTEGER", nullable: false),
                    OriginLink = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    HeroImageUrl = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    InlineCss = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    IsFeatured = table.Column<bool>(type: "INTEGER", nullable: false),
                    HashCheckSum = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Post", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Post_Site_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Site",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SiteBinaryAsset",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Base64Data = table.Column<string>(type: "TEXT", nullable: true),
                    LastModifiedTimeUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteBinaryAsset", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SiteBinaryAsset_Site_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Site",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SiteDomain",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Host = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    IsPrimary = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteDomain", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SiteDomain_Site_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Site",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SiteSetting",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CfgKey = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CfgValue = table.Column<string>(type: "TEXT", nullable: true),
                    SchemaVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    LastModifiedTimeUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteSetting", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SiteSetting_Site_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Site",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tag",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    NormalizedName = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tag", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tag_Site_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Site",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Theme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ThemeName = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CssRules = table.Column<string>(type: "TEXT", nullable: true),
                    AdditionalProps = table.Column<string>(type: "TEXT", nullable: true),
                    ThemeType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Theme", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Theme_Site_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Site",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AiJob",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    JobType = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetEntityType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    TargetEntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Model = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FinishedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiJob", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiJob_Site_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Site",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AiJob_User_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MediaAsset",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Bucket = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    ObjectKey = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    OriginalFileName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    PublicUrl = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    MimeType = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    ContentHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Width = table.Column<int>(type: "INTEGER", nullable: true),
                    Height = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaAsset", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaAsset_Site_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Site",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MediaAsset_User_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SiteMembership",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteMembership", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SiteMembership_Site_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Site",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SiteMembership_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubMenu",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    IsOpenInNewTab = table.Column<bool>(type: "INTEGER", nullable: false),
                    MenuId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubMenu", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubMenu_Menu_MenuId",
                        column: x => x.MenuId,
                        principalTable: "Menu",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubMenu_Site_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Site",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Comment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    IPAddress = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    CreateTimeUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CommentContent = table.Column<string>(type: "TEXT", nullable: false),
                    PostId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsApproved = table.Column<bool>(type: "INTEGER", nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comment_Post_PostId",
                        column: x => x.PostId,
                        principalTable: "Post",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Comment_Site_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Site",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PostCategory",
                columns: table => new
                {
                    PostId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostCategory", x => new { x.PostId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_PostCategory_Category_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Category",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostCategory_Post_PostId",
                        column: x => x.PostId,
                        principalTable: "Post",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostCategory_Site_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Site",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PostContent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PostId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CultureCode = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    ContentKind = table.Column<int>(type: "INTEGER", nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: true),
                    Abstract = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    IsOriginal = table.Column<bool>(type: "INTEGER", nullable: false),
                    GeneratedBy = table.Column<int>(type: "INTEGER", nullable: false),
                    GenerationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostContent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostContent_Post_PostId",
                        column: x => x.PostId,
                        principalTable: "Post",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostContent_Site_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Site",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PostMetric",
                columns: table => new
                {
                    PostId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Hits = table.Column<int>(type: "INTEGER", nullable: false),
                    Likes = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostMetric", x => x.PostId);
                    table.ForeignKey(
                        name: "FK_PostMetric_Post_PostId",
                        column: x => x.PostId,
                        principalTable: "Post",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostMetric_Site_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Site",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PostRoute",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PostId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RouteDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    HashCheckSum = table.Column<int>(type: "INTEGER", nullable: false),
                    IsCanonical = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostRoute", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostRoute_Post_PostId",
                        column: x => x.PostId,
                        principalTable: "Post",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostRoute_Site_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Site",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PostTag",
                columns: table => new
                {
                    PostId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TagId = table.Column<int>(type: "INTEGER", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostTag", x => new { x.PostId, x.TagId });
                    table.ForeignKey(
                        name: "FK_PostTag_Post_PostId",
                        column: x => x.PostId,
                        principalTable: "Post",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostTag_Site_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Site",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostTag_Tag_TagId",
                        column: x => x.TagId,
                        principalTable: "Tag",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AiArtifact",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    JobId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TargetEntityType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    TargetEntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ArtifactType = table.Column<int>(type: "INTEGER", nullable: false),
                    CultureCode = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    Content = table.Column<string>(type: "TEXT", nullable: true),
                    MetadataJson = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiArtifact", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiArtifact_AiJob_JobId",
                        column: x => x.JobId,
                        principalTable: "AiJob",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AiArtifact_Site_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Site",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MediaVariant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MediaAssetId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VariantName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ObjectKey = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Width = table.Column<int>(type: "INTEGER", nullable: true),
                    Height = table.Column<int>(type: "INTEGER", nullable: true),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaVariant", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaVariant_MediaAsset_MediaAssetId",
                        column: x => x.MediaAssetId,
                        principalTable: "MediaAsset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommentReply",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReplyContent = table.Column<string>(type: "TEXT", nullable: true),
                    CreateTimeUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CommentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Source = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentReply", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommentReply_Comment_CommentId",
                        column: x => x.CommentId,
                        principalTable: "Comment",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CommentReply_Site_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Site",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiArtifact_JobId",
                table: "AiArtifact",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_AiArtifact_SiteId_TargetEntityType_TargetEntityId_ArtifactType",
                table: "AiArtifact",
                columns: new[] { "SiteId", "TargetEntityType", "TargetEntityId", "ArtifactType" });

            migrationBuilder.CreateIndex(
                name: "IX_AiJob_RequestedByUserId",
                table: "AiJob",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AiJob_SiteId_Status_JobType_CreatedAtUtc",
                table: "AiJob",
                columns: new[] { "SiteId", "Status", "JobType", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Category_SiteId_RouteName",
                table: "Category",
                columns: new[] { "SiteId", "RouteName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comment_PostId",
                table: "Comment",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_Comment_SiteId_PostId_CreateTimeUtc",
                table: "Comment",
                columns: new[] { "SiteId", "PostId", "CreateTimeUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CommentReply_CommentId",
                table: "CommentReply",
                column: "CommentId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentReply_SiteId",
                table: "CommentReply",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_FriendLink_SiteId",
                table: "FriendLink",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAsset_OwnerUserId",
                table: "MediaAsset",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAsset_SiteId_ContentHash",
                table: "MediaAsset",
                columns: new[] { "SiteId", "ContentHash" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaVariant_MediaAssetId",
                table: "MediaVariant",
                column: "MediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Menu_SiteId_DisplayOrder",
                table: "Menu",
                columns: new[] { "SiteId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Page_SiteId_Slug",
                table: "Page",
                columns: new[] { "SiteId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Post_SiteId_IsDeleted_IsPublished_PubDateUtc",
                table: "Post",
                columns: new[] { "SiteId", "IsDeleted", "IsPublished", "PubDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Post_SiteId_IsFeatured_PubDateUtc",
                table: "Post",
                columns: new[] { "SiteId", "IsFeatured", "PubDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PostCategory_CategoryId",
                table: "PostCategory",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PostCategory_SiteId",
                table: "PostCategory",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_PostContent_PostId",
                table: "PostContent",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_PostContent_SiteId_PostId_CultureCode_ContentKind",
                table: "PostContent",
                columns: new[] { "SiteId", "PostId", "CultureCode", "ContentKind" });

            migrationBuilder.CreateIndex(
                name: "IX_PostMetric_SiteId",
                table: "PostMetric",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_PostRoute_PostId",
                table: "PostRoute",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_PostRoute_SiteId_HashCheckSum",
                table: "PostRoute",
                columns: new[] { "SiteId", "HashCheckSum" });

            migrationBuilder.CreateIndex(
                name: "IX_PostRoute_SiteId_RouteDate_Slug",
                table: "PostRoute",
                columns: new[] { "SiteId", "RouteDate", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostTag_SiteId",
                table: "PostTag",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_PostTag_TagId",
                table: "PostTag",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_Site_TenantId_Slug",
                table: "Site",
                columns: new[] { "TenantId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiteBinaryAsset_SiteId",
                table: "SiteBinaryAsset",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteDomain_Host",
                table: "SiteDomain",
                column: "Host",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiteDomain_SiteId",
                table: "SiteDomain",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteMembership_SiteId_UserId",
                table: "SiteMembership",
                columns: new[] { "SiteId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiteMembership_UserId",
                table: "SiteMembership",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteSetting_SiteId_CfgKey",
                table: "SiteSetting",
                columns: new[] { "SiteId", "CfgKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubMenu_MenuId",
                table: "SubMenu",
                column: "MenuId");

            migrationBuilder.CreateIndex(
                name: "IX_SubMenu_SiteId",
                table: "SubMenu",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_Tag_SiteId_NormalizedName",
                table: "Tag",
                columns: new[] { "SiteId", "NormalizedName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Theme_SiteId_ThemeName",
                table: "Theme",
                columns: new[] { "SiteId", "ThemeName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_NormalizedUsername",
                table: "User",
                column: "NormalizedUsername",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_TenantId",
                table: "User",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiArtifact");

            migrationBuilder.DropTable(
                name: "CommentReply");

            migrationBuilder.DropTable(
                name: "FriendLink");

            migrationBuilder.DropTable(
                name: "MediaVariant");

            migrationBuilder.DropTable(
                name: "Page");

            migrationBuilder.DropTable(
                name: "PostCategory");

            migrationBuilder.DropTable(
                name: "PostContent");

            migrationBuilder.DropTable(
                name: "PostMetric");

            migrationBuilder.DropTable(
                name: "PostRoute");

            migrationBuilder.DropTable(
                name: "PostTag");

            migrationBuilder.DropTable(
                name: "SiteBinaryAsset");

            migrationBuilder.DropTable(
                name: "SiteDomain");

            migrationBuilder.DropTable(
                name: "SiteMembership");

            migrationBuilder.DropTable(
                name: "SiteSetting");

            migrationBuilder.DropTable(
                name: "SubMenu");

            migrationBuilder.DropTable(
                name: "Theme");

            migrationBuilder.DropTable(
                name: "AiJob");

            migrationBuilder.DropTable(
                name: "Comment");

            migrationBuilder.DropTable(
                name: "MediaAsset");

            migrationBuilder.DropTable(
                name: "Category");

            migrationBuilder.DropTable(
                name: "Tag");

            migrationBuilder.DropTable(
                name: "Menu");

            migrationBuilder.DropTable(
                name: "Post");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "Site");

            migrationBuilder.DropTable(
                name: "Tenant");
        }
    }
}
