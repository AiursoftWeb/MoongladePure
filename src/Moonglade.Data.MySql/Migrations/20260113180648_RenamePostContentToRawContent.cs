using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoongladePure.Data.MySql.Migrations
{
    /// <inheritdoc />
    public partial class RenamePostContentToRawContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PostContent",
                table: "Post",
                newName: "RawContent");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RawContent",
                table: "Post",
                newName: "PostContent");
        }
    }
}
