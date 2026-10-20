using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkAllocation.Migrations
{
    /// <inheritdoc />
    public partial class InitialWorkAllocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "allocations",
                columns: table => new
                {
                    allocation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    period_end = table.Column<DateOnly>(type: "date", nullable: false),
                    percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_allocations", x => x.allocation_id);
                    table.CheckConstraint("ck_allocations_percentage_range", "percentage > 0 AND percentage <= 100");
                    table.CheckConstraint("ck_allocations_period_valid", "period_start <= period_end");
                });

            migrationBuilder.CreateTable(
                name: "allocation_history",
                columns: table => new
                {
                    allocation_history_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allocation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    change_type = table.Column<string>(type: "text", nullable: false),
                    old_value = table.Column<string>(type: "text", nullable: true),
                    new_value = table.Column<string>(type: "text", nullable: true),
                    changed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_allocation_history", x => x.allocation_history_id);
                    table.CheckConstraint("ck_allocation_history_change_type_valid", "change_type IN ('Created', 'StatusChanged', 'PeriodChanged', 'PercentageChanged')");
                    table.ForeignKey(
                        name: "fk_allocation_history_allocation_id",
                        column: x => x.allocation_id,
                        principalTable: "allocations",
                        principalColumn: "allocation_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_allocation_history_allocation_id",
                table: "allocation_history",
                column: "allocation_id");

            migrationBuilder.CreateIndex(
                name: "ix_allocations_employee_id_status",
                table: "allocations",
                columns: new[] { "employee_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_allocations_project_id",
                table: "allocations",
                column: "project_id");

            // FK inter-modules : absentes du modèle EF (08-database-design.md, §5), mais bien réelles en base.
            migrationBuilder.AddForeignKey(
                name: "fk_allocations_employee_id",
                table: "allocations",
                column: "employee_id",
                principalTable: "employees",
                principalColumn: "employee_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_allocations_project_id",
                table: "allocations",
                column: "project_id",
                principalTable: "projects",
                principalColumn: "project_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "allocation_history");

            migrationBuilder.DropTable(
                name: "allocations");
        }
    }
}
