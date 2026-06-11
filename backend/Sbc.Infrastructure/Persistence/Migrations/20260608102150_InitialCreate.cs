using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sbc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "simulators",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    location = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_simulators", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "protected_systems",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    simulator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    hostname = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ip_address = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    operating_system = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    file_system = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    partition_scheme = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ur_backup_client_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ur_backup_client_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ur_backup_client_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    criticality = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    backup_capability = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    file_backup_validated = table.Column<bool>(type: "boolean", nullable: false),
                    image_backup_validated = table.Column<bool>(type: "boolean", nullable: false),
                    live_backup_validated = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_protected_systems", x => x.id);
                    table.ForeignKey(
                        name: "fk_protected_systems_simulators_simulator_id",
                        column: x => x.simulator_id,
                        principalTable: "simulators",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "alerts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    protected_system_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    severity = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    resolved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_alerts", x => x.id);
                    table.ForeignKey(
                        name: "fk_alerts_protected_systems_protected_system_id",
                        column: x => x.protected_system_id,
                        principalTable: "protected_systems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "backup_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    protected_system_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    backup_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    finished_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    duration_seconds = table.Column<int>(type: "integer", nullable: true),
                    size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    ur_backup_job_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    backup_path = table.Column<string>(type: "text", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_backup_jobs", x => x.id);
                    table.ForeignKey(
                        name: "fk_backup_jobs_protected_systems_protected_system_id",
                        column: x => x.protected_system_id,
                        principalTable: "protected_systems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "manual_backup_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    protected_system_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_by = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    assigned_to = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    reason = table.Column<string>(type: "text", nullable: false),
                    related_change_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    requested_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    validated_by = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    validated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    validation_notes = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_manual_backup_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_manual_backup_requests_protected_systems_protected_system_id",
                        column: x => x.protected_system_id,
                        principalTable: "protected_systems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "backup_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    protected_system_id = table.Column<Guid>(type: "uuid", nullable: true),
                    backup_job_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    severity = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    raw_payload_json = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_backup_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_backup_events_backup_jobs_backup_job_id",
                        column: x => x.backup_job_id,
                        principalTable: "backup_jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_backup_events_protected_systems_protected_system_id",
                        column: x => x.protected_system_id,
                        principalTable: "protected_systems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_alerts_code",
                table: "alerts",
                column: "code");

            migrationBuilder.CreateIndex(
                name: "ix_alerts_protected_system_id",
                table: "alerts",
                column: "protected_system_id");

            migrationBuilder.CreateIndex(
                name: "ix_alerts_status",
                table: "alerts",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_backup_events_backup_job_id",
                table: "backup_events",
                column: "backup_job_id");

            migrationBuilder.CreateIndex(
                name: "ix_backup_events_protected_system_id",
                table: "backup_events",
                column: "protected_system_id");

            migrationBuilder.CreateIndex(
                name: "ix_backup_jobs_protected_system_id",
                table: "backup_jobs",
                column: "protected_system_id");

            migrationBuilder.CreateIndex(
                name: "ix_backup_jobs_status",
                table: "backup_jobs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_manual_backup_requests_protected_system_id",
                table: "manual_backup_requests",
                column: "protected_system_id");

            migrationBuilder.CreateIndex(
                name: "ix_manual_backup_requests_status",
                table: "manual_backup_requests",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_protected_systems_hostname",
                table: "protected_systems",
                column: "hostname");

            migrationBuilder.CreateIndex(
                name: "ix_protected_systems_simulator_id",
                table: "protected_systems",
                column: "simulator_id");

            migrationBuilder.CreateIndex(
                name: "ix_protected_systems_ur_backup_client_id",
                table: "protected_systems",
                column: "ur_backup_client_id");

            migrationBuilder.CreateIndex(
                name: "ix_simulators_code",
                table: "simulators",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alerts");

            migrationBuilder.DropTable(
                name: "backup_events");

            migrationBuilder.DropTable(
                name: "manual_backup_requests");

            migrationBuilder.DropTable(
                name: "backup_jobs");

            migrationBuilder.DropTable(
                name: "protected_systems");

            migrationBuilder.DropTable(
                name: "simulators");
        }
    }
}
