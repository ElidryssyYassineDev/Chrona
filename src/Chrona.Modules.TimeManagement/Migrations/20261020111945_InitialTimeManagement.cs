using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeManagement.Migrations
{
    /// <inheritdoc />
    public partial class InitialTimeManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "timesheets",
                columns: table => new
                {
                    timesheet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    period_end = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    last_submitted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_timesheets", x => x.timesheet_id);
                    table.CheckConstraint("ck_timesheets_period_valid", "period_start <= period_end");
                });

            migrationBuilder.CreateTable(
                name: "time_entries",
                columns: table => new
                {
                    time_entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    timesheet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allocation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entry_date = table.Column<DateOnly>(type: "date", nullable: false),
                    hours = table.Column<decimal>(type: "numeric(4,2)", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_time_entries", x => x.time_entry_id);
                    table.CheckConstraint("ck_time_entries_hours_range", "hours > 0 AND hours <= 24");
                    table.ForeignKey(
                        name: "fk_time_entries_timesheet_id",
                        column: x => x.timesheet_id,
                        principalTable: "timesheets",
                        principalColumn: "timesheet_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_time_entries_allocation_id",
                table: "time_entries",
                column: "allocation_id");

            migrationBuilder.CreateIndex(
                name: "ix_time_entries_timesheet_id",
                table: "time_entries",
                column: "timesheet_id");

            migrationBuilder.CreateIndex(
                name: "ix_timesheets_status",
                table: "timesheets",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "uk_timesheets_employee_id_period",
                table: "timesheets",
                columns: new[] { "employee_id", "period_start", "period_end" },
                unique: true);

            // FK inter-modules : absentes du modèle EF (08-database-design.md, §5), mais bien réelles en base.
            migrationBuilder.AddForeignKey(
                name: "fk_timesheets_employee_id",
                table: "timesheets",
                column: "employee_id",
                principalTable: "employees",
                principalColumn: "employee_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_time_entries_allocation_id",
                table: "time_entries",
                column: "allocation_id",
                principalTable: "allocations",
                principalColumn: "allocation_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "time_entries");

            migrationBuilder.DropTable(
                name: "timesheets");
        }
    }
}
