using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoongladePure.Data.MySql.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // migrationBuilder.AlterDatabase()
            //     .Annotation("MySql:CharSet", "utf8mb4");
            //
            // migrationBuilder.CreateTable(
            //     name: "BlogAsset",
            //     columns: table => new
            //     {
            //         Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //         Base64Data = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4"),
            //         LastModifiedTimeUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_BlogAsset", x => x.Id);
            //     })
            //     .Annotation("MySql:CharSet", "utf8mb4");
            //
            // migrationBuilder.CreateTable(
            //     name: "BlogConfiguration",
            //     columns: table => new
            //     {
            //         Id = table.Column<int>(type: "int", nullable: false)
            //             .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
            //         CfgKey = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4"),
            //         CfgValue = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4"),
            //         LastModifiedTimeUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_BlogConfiguration", x => x.Id);
            //     })
            //     .Annotation("MySql:CharSet", "utf8mb4");
            //
            // migrationBuilder.CreateTable(
            //     name: "BlogTheme",
            //     columns: table => new
            //     {
            //         Id = table.Column<int>(type: "int", nullable: false)
            //             .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
            //         ThemeName = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4"),
            //         CssRules = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4"),
            //         AdditionalProps = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4"),
            //         ThemeType = table.Column<int>(type: "int", nullable: false)
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_BlogTheme", x => x.Id);
            //     })
            //     .Annotation("MySql:CharSet", "utf8mb4");
            //
            // migrationBuilder.CreateTable(
            //     name: "Category",
            //     columns: table => new
            //     {
            //         Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //         RouteName = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4"),
            //         DisplayName = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4"),
            //         Note = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4")
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_Category", x => x.Id);
            //     })
            //     .Annotation("MySql:CharSet", "utf8mb4");
            //
            // migrationBuilder.CreateTable(
            //     name: "CustomPage",
            //     columns: table => new
            //     {
            //         Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //         Title = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4"),
            //         Slug = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4"),
            //         MetaDescription = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4"),
            //         HtmlContent = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4"),
            //         CssContent = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4"),
            //         HideSidebar = table.Column<bool>(type: "tinyint(1)", nullable: false),
            //         IsPublished = table.Column<bool>(type: "tinyint(1)", nullable: false),
            //         CreateTimeUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
            //         UpdateTimeUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_CustomPage", x => x.Id);
            //     })
            //     .Annotation("MySql:CharSet", "utf8mb4");
            //
            // migrationBuilder.CreateTable(
            //     name: "FriendLink",
            //     columns: table => new
            //     {
            //         Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //         Title = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4"),
            //         LinkUrl = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4")
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_FriendLink", x => x.Id);
            //     })
            //     .Annotation("MySql:CharSet", "utf8mb4");
            //
            // migrationBuilder.CreateTable(
            //     name: "LocalAccount",
            //     columns: table => new
            //     {
            //         Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //         Username = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4"),
            //         PasswordSalt = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4"),
            //         PasswordHash = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4"),
            //         LastLoginTimeUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
            //         LastLoginIp = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4"),
            //         CreateTimeUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_LocalAccount", x => x.Id);
            //     })
            //     .Annotation("MySql:CharSet", "utf8mb4");
            //
            // migrationBuilder.CreateTable(
            //     name: "Menu",
            //     columns: table => new
            //     {
            //         Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //         Title = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4"),
            //         Url = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4"),
            //         Icon = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4"),
            //         DisplayOrder = table.Column<int>(type: "int", nullable: false),
            //         IsOpenInNewTab = table.Column<bool>(type: "tinyint(1)", nullable: false)
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_Menu", x => x.Id);
            //     })
            //     .Annotation("MySql:CharSet", "utf8mb4");
            //
            // migrationBuilder.CreateTable(
            //     name: "Post",
            //     columns: table => new
            //     {
            //         Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //         Title = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4"),
            //         Slug = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4"),
            //         Author = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4"),
            //         PostContent = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4"),
            //         CommentEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
            //         CreateTimeUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
            //         ContentAbstract = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4"),
            //         ContentLanguageCode = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4"),
            //         IsFeedIncluded = table.Column<bool>(type: "tinyint(1)", nullable: false),
            //         PubDateUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
            //         LastModifiedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
            //         IsPublished = table.Column<bool>(type: "tinyint(1)", nullable: false),
            //         IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
            //         IsOriginal = table.Column<bool>(type: "tinyint(1)", nullable: false),
            //         OriginLink = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4"),
            //         HeroImageUrl = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4"),
            //         InlineCss = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4"),
            //         IsFeatured = table.Column<bool>(type: "tinyint(1)", nullable: false),
            //         HashCheckSum = table.Column<int>(type: "int", nullable: false)
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_Post", x => x.Id);
            //     })
            //     .Annotation("MySql:CharSet", "utf8mb4");
            //
            // migrationBuilder.CreateTable(
            //     name: "Tag",
            //     columns: table => new
            //     {
            //         Id = table.Column<int>(type: "int", nullable: false)
            //             .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
            //         DisplayName = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4"),
            //         NormalizedName = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4")
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_Tag", x => x.Id);
            //     })
            //     .Annotation("MySql:CharSet", "utf8mb4");
            //
            // migrationBuilder.CreateTable(
            //     name: "SubMenu",
            //     columns: table => new
            //     {
            //         Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //         Title = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4"),
            //         Url = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4"),
            //         IsOpenInNewTab = table.Column<bool>(type: "tinyint(1)", nullable: false),
            //         MenuId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_SubMenu", x => x.Id);
            //         table.ForeignKey(
            //             name: "FK_SubMenu_Menu_MenuId",
            //             column: x => x.MenuId,
            //             principalTable: "Menu",
            //             principalColumn: "Id",
            //             onDelete: ReferentialAction.Cascade);
            //     })
            //     .Annotation("MySql:CharSet", "utf8mb4");
            //
            // migrationBuilder.CreateTable(
            //     name: "Comment",
            //     columns: table => new
            //     {
            //         Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //         Username = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4"),
            //         Email = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4"),
            //         IPAddress = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4"),
            //         CreateTimeUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
            //         CommentContent = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4"),
            //         PostId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //         IsApproved = table.Column<bool>(type: "tinyint(1)", nullable: false)
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_Comment", x => x.Id);
            //         table.ForeignKey(
            //             name: "FK_Comment_Post_PostId",
            //             column: x => x.PostId,
            //             principalTable: "Post",
            //             principalColumn: "Id",
            //             onDelete: ReferentialAction.Cascade);
            //     })
            //     .Annotation("MySql:CharSet", "utf8mb4");
            //
            // migrationBuilder.CreateTable(
            //     name: "PostCategory",
            //     columns: table => new
            //     {
            //         Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //         PostId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //         CategoryId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_PostCategory", x => x.Id);
            //         table.ForeignKey(
            //             name: "FK_PostCategory_Category_CategoryId",
            //             column: x => x.CategoryId,
            //             principalTable: "Category",
            //             principalColumn: "Id",
            //             onDelete: ReferentialAction.Cascade);
            //         table.ForeignKey(
            //             name: "FK_PostCategory_Post_PostId",
            //             column: x => x.PostId,
            //             principalTable: "Post",
            //             principalColumn: "Id",
            //             onDelete: ReferentialAction.Cascade);
            //     })
            //     .Annotation("MySql:CharSet", "utf8mb4");
            //
            // migrationBuilder.CreateTable(
            //     name: "PostExtension",
            //     columns: table => new
            //     {
            //         PostId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //         Hits = table.Column<int>(type: "int", nullable: false),
            //         Likes = table.Column<int>(type: "int", nullable: false)
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_PostExtension", x => x.PostId);
            //         table.ForeignKey(
            //             name: "FK_PostExtension_Post_PostId",
            //             column: x => x.PostId,
            //             principalTable: "Post",
            //             principalColumn: "Id",
            //             onDelete: ReferentialAction.Cascade);
            //     })
            //     .Annotation("MySql:CharSet", "utf8mb4");
            //
            // migrationBuilder.CreateTable(
            //     name: "PostEntityTagEntity",
            //     columns: table => new
            //     {
            //         PostsId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //         TagsId = table.Column<int>(type: "int", nullable: false)
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_PostEntityTagEntity", x => new { x.PostsId, x.TagsId });
            //         table.ForeignKey(
            //             name: "FK_PostEntityTagEntity_Post_PostsId",
            //             column: x => x.PostsId,
            //             principalTable: "Post",
            //             principalColumn: "Id",
            //             onDelete: ReferentialAction.Cascade);
            //         table.ForeignKey(
            //             name: "FK_PostEntityTagEntity_Tag_TagsId",
            //             column: x => x.TagsId,
            //             principalTable: "Tag",
            //             principalColumn: "Id",
            //             onDelete: ReferentialAction.Cascade);
            //     })
            //     .Annotation("MySql:CharSet", "utf8mb4");
            //
            // migrationBuilder.CreateTable(
            //     name: "PostTag",
            //     columns: table => new
            //     {
            //         Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //         PostId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //         TagId = table.Column<int>(type: "int", nullable: false)
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_PostTag", x => x.Id);
            //         table.ForeignKey(
            //             name: "FK_PostTag_Post_PostId",
            //             column: x => x.PostId,
            //             principalTable: "Post",
            //             principalColumn: "Id",
            //             onDelete: ReferentialAction.Cascade);
            //         table.ForeignKey(
            //             name: "FK_PostTag_Tag_TagId",
            //             column: x => x.TagId,
            //             principalTable: "Tag",
            //             principalColumn: "Id",
            //             onDelete: ReferentialAction.Cascade);
            //     })
            //     .Annotation("MySql:CharSet", "utf8mb4");
            //
            // migrationBuilder.CreateTable(
            //     name: "CommentReply",
            //     columns: table => new
            //     {
            //         Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //         ReplyContent = table.Column<string>(type: "longtext", nullable: true)
            //             .Annotation("MySql:CharSet", "utf8mb4"),
            //         CreateTimeUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
            //         CommentId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_CommentReply", x => x.Id);
            //         table.ForeignKey(
            //             name: "FK_CommentReply_Comment_CommentId",
            //             column: x => x.CommentId,
            //             principalTable: "Comment",
            //             principalColumn: "Id");
            //     })
            //     .Annotation("MySql:CharSet", "utf8mb4");
            //
            // migrationBuilder.CreateIndex(
            //     name: "IX_Comment_PostId",
            //     table: "Comment",
            //     column: "PostId");
            //
            // migrationBuilder.CreateIndex(
            //     name: "IX_CommentReply_CommentId",
            //     table: "CommentReply",
            //     column: "CommentId");
            //
            // migrationBuilder.CreateIndex(
            //     name: "IX_PostCategory_CategoryId",
            //     table: "PostCategory",
            //     column: "CategoryId");
            //
            // migrationBuilder.CreateIndex(
            //     name: "IX_PostCategory_PostId",
            //     table: "PostCategory",
            //     column: "PostId");
            //
            // migrationBuilder.CreateIndex(
            //     name: "IX_PostEntityTagEntity_TagsId",
            //     table: "PostEntityTagEntity",
            //     column: "TagsId");
            //
            // migrationBuilder.CreateIndex(
            //     name: "IX_PostTag_PostId",
            //     table: "PostTag",
            //     column: "PostId");
            //
            // migrationBuilder.CreateIndex(
            //     name: "IX_PostTag_TagId",
            //     table: "PostTag",
            //     column: "TagId");
            //
            // migrationBuilder.CreateIndex(
            //     name: "IX_SubMenu_MenuId",
            //     table: "SubMenu",
            //     column: "MenuId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlogAsset");

            migrationBuilder.DropTable(
                name: "BlogConfiguration");

            migrationBuilder.DropTable(
                name: "BlogTheme");

            migrationBuilder.DropTable(
                name: "CommentReply");

            migrationBuilder.DropTable(
                name: "CustomPage");

            migrationBuilder.DropTable(
                name: "FriendLink");

            migrationBuilder.DropTable(
                name: "LocalAccount");

            migrationBuilder.DropTable(
                name: "PostCategory");

            migrationBuilder.DropTable(
                name: "PostEntityTagEntity");

            migrationBuilder.DropTable(
                name: "PostExtension");

            migrationBuilder.DropTable(
                name: "PostTag");

            migrationBuilder.DropTable(
                name: "SubMenu");

            migrationBuilder.DropTable(
                name: "Comment");

            migrationBuilder.DropTable(
                name: "Category");

            migrationBuilder.DropTable(
                name: "Tag");

            migrationBuilder.DropTable(
                name: "Menu");

            migrationBuilder.DropTable(
                name: "Post");
        }
    }
}
