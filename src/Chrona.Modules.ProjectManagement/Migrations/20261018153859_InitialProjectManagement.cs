using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectManagement.Migrations
{
    /// <inheritdoc />
    public partial class InitialProjectManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    archived_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_projects", x => x.project_id);
                    table.CheckConstraint("ck_projects_archived_at_consistency", "status <> 'Archived' OR archived_at_utc IS NOT NULL");
                    table.CheckConstraint("ck_projects_name_not_empty", "length(trim(name)) > 0");
                });

            migrationBuilder.CreateTable(
                name: "project_members",
                columns: table => new
                {
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    added_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_members", x => new { x.project_id, x.employee_id });
                    table.ForeignKey(
                        name: "fk_project_members_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "project_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_project_members_employee_id",
                table: "project_members",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "uk_projects_name",
                table: "projects",
                column: "name",
                unique: true);

            // FK inter-modules : absente du modèle EF (08-database-design.md, §5), mais bien réelle en base.
            migrationBuilder.AddForeignKey(
                name: "fk_project_members_employee_id",
                table: "project_members",
                column: "employee_id",
                principalTable: "employees",
                principalColumn: "employee_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "project_members");

            migrationBuilder.DropTable(
                name: "projects");
        }
    }
}
