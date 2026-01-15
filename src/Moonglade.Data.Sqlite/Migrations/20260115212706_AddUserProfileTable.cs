using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoongladePure.Data.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfileTable : Migration
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
                type: "TEXT",
                nullable: true);
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
