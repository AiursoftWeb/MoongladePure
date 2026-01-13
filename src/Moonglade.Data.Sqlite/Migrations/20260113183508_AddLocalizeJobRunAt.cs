using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoongladePure.Data.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalizeJobRunAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LocalizeJobRunAt",
                table: "Post",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LocalizeJobRunAt",
                table: "Post");
        }
    }
}
