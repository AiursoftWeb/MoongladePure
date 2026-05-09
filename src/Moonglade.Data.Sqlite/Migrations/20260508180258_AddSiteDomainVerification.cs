using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoongladePure.Data.Sqlite.Migrations
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
                type: "TEXT",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastVerifiedAtUtc",
                table: "SiteDomain",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VerificationStatus",
                table: "SiteDomain",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "VerificationToken",
                table: "SiteDomain",
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedAtUtc",
                table: "SiteDomain",
                type: "TEXT",
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
