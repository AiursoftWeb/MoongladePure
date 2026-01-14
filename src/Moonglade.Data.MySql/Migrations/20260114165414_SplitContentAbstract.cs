using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoongladePure.Data.MySql.Migrations
{
    /// <inheritdoc />
    public partial class SplitContentAbstract : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ContentAbstract",
                table: "Post",
                newName: "ContentAbstractZh");

            migrationBuilder.AddColumn<string>(
                name: "ContentAbstractEn",
                table: "Post",
                type: "varchar(1024)",
                maxLength: 1024,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentAbstractEn",
                table: "Post");

            migrationBuilder.RenameColumn(
                name: "ContentAbstractZh",
                table: "Post",
                newName: "ContentAbstract");
        }
    }
}
