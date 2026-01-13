using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoongladePure.Data.MySql.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalizedContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LocalizedChineseContent",
                table: "Post",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LocalizedEnglishContent",
                table: "Post",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LocalizedChineseContent",
                table: "Post");

            migrationBuilder.DropColumn(
                name: "LocalizedEnglishContent",
                table: "Post");
        }
    }
}
