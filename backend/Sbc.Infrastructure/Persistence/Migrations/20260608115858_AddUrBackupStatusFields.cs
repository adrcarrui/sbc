using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sbc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUrBackupStatusFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_online",
                table: "protected_systems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_file_backup_at_utc",
                table: "protected_systems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "last_file_backup_issues",
                table: "protected_systems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "last_file_backup_ok",
                table: "protected_systems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_image_backup_at_utc",
                table: "protected_systems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "last_image_backup_ok",
                table: "protected_systems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_seen_at_utc",
                table: "protected_systems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ur_backup_status_code",
                table: "protected_systems",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_protected_systems_is_online",
                table: "protected_systems",
                column: "is_online");

            migrationBuilder.CreateIndex(
                name: "ix_protected_systems_last_seen_at_utc",
                table: "protected_systems",
                column: "last_seen_at_utc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_protected_systems_is_online",
                table: "protected_systems");

            migrationBuilder.DropIndex(
                name: "ix_protected_systems_last_seen_at_utc",
                table: "protected_systems");

            migrationBuilder.DropColumn(
                name: "is_online",
                table: "protected_systems");

            migrationBuilder.DropColumn(
                name: "last_file_backup_at_utc",
                table: "protected_systems");

            migrationBuilder.DropColumn(
                name: "last_file_backup_issues",
                table: "protected_systems");

            migrationBuilder.DropColumn(
                name: "last_file_backup_ok",
                table: "protected_systems");

            migrationBuilder.DropColumn(
                name: "last_image_backup_at_utc",
                table: "protected_systems");

            migrationBuilder.DropColumn(
                name: "last_image_backup_ok",
                table: "protected_systems");

            migrationBuilder.DropColumn(
                name: "last_seen_at_utc",
                table: "protected_systems");

            migrationBuilder.DropColumn(
                name: "ur_backup_status_code",
                table: "protected_systems");
        }
    }
}
