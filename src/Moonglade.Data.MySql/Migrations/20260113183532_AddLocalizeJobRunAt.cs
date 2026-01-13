using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoongladePure.Data.MySql.Migrations
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
                type: "datetime",
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
