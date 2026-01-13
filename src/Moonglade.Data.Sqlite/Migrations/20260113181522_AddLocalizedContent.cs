using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoongladePure.Data.Sqlite.Migrations
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
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocalizedEnglishContent",
                table: "Post",
                type: "TEXT",
                nullable: true);
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
