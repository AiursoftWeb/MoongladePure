using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoongladePure.Data.MySql.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteDomainVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastVerificationError",
                table: "SiteDomain",
                type: "varchar(512)",
                maxLength: 512,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastVerifiedAtUtc",
                table: "SiteDomain",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VerificationStatus",
                table: "SiteDomain",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "VerificationToken",
                table: "SiteDomain",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedAtUtc",
                table: "SiteDomain",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastVerificationError",
                table: "SiteDomain");

            migrationBuilder.DropColumn(
                name: "LastVerifiedAtUtc",
                table: "SiteDomain");

            migrationBuilder.DropColumn(
                name: "VerificationStatus",
                table: "SiteDomain");

            migrationBuilder.DropColumn(
                name: "VerificationToken",
                table: "SiteDomain");

            migrationBuilder.DropColumn(
                name: "VerifiedAtUtc",
                table: "SiteDomain");
        }
    }
}
