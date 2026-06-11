using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sbc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUrBackupRemovalTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_removed_from_ur_backup",
                table: "protected_systems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_ur_backup_sync_at_utc",
                table: "protected_systems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "removed_from_ur_backup_at_utc",
                table: "protected_systems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_protected_systems_is_removed_from_ur_backup",
                table: "protected_systems",
                column: "is_removed_from_ur_backup");

            migrationBuilder.CreateIndex(
                name: "ix_protected_systems_last_ur_backup_sync_at_utc",
                table: "protected_systems",
                column: "last_ur_backup_sync_at_utc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_protected_systems_is_removed_from_ur_backup",
                table: "protected_systems");

            migrationBuilder.DropIndex(
                name: "ix_protected_systems_last_ur_backup_sync_at_utc",
                table: "protected_systems");

            migrationBuilder.DropColumn(
                name: "is_removed_from_ur_backup",
                table: "protected_systems");

            migrationBuilder.DropColumn(
                name: "last_ur_backup_sync_at_utc",
                table: "protected_systems");

            migrationBuilder.DropColumn(
                name: "removed_from_ur_backup_at_utc",
                table: "protected_systems");
        }
    }
}
