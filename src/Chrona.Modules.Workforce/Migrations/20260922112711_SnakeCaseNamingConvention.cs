using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Workforce.Migrations
{
    /// <inheritdoc />
    public partial class SnakeCaseNamingConvention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_employees",
                table: "employees");

            migrationBuilder.RenameColumn(
                name: "ManagerId",
                table: "employees",
                newName: "manager_id");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "employees",
                newName: "last_name");

            migrationBuilder.RenameColumn(
                name: "KeycloakSubjectId",
                table: "employees",
                newName: "keycloak_subject_id");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "employees",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "FirstName",
                table: "employees",
                newName: "first_name");

            migrationBuilder.RenameColumn(
                name: "DepartmentId",
                table: "employees",
                newName: "department_id");

            migrationBuilder.RenameColumn(
                name: "DeactivatedAtUtc",
                table: "employees",
                newName: "deactivated_at_utc");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "employees",
                newName: "created_at_utc");

            migrationBuilder.RenameColumn(
                name: "EmployeeId",
                table: "employees",
                newName: "employee_id");

            migrationBuilder.RenameIndex(
                name: "IX_employees_KeycloakSubjectId",
                table: "employees",
                newName: "ix_employees_keycloak_subject_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_employees",
                table: "employees",
                column: "employee_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_employees",
                table: "employees");

            migrationBuilder.RenameColumn(
                name: "manager_id",
                table: "employees",
                newName: "ManagerId");

            migrationBuilder.RenameColumn(
                name: "last_name",
                table: "employees",
                newName: "LastName");

            migrationBuilder.RenameColumn(
                name: "keycloak_subject_id",
                table: "employees",
                newName: "KeycloakSubjectId");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "employees",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "first_name",
                table: "employees",
                newName: "FirstName");

            migrationBuilder.RenameColumn(
                name: "department_id",
                table: "employees",
                newName: "DepartmentId");

            migrationBuilder.RenameColumn(
                name: "deactivated_at_utc",
                table: "employees",
                newName: "DeactivatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "created_at_utc",
                table: "employees",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "employee_id",
                table: "employees",
                newName: "EmployeeId");

            migrationBuilder.RenameIndex(
                name: "ix_employees_keycloak_subject_id",
                table: "employees",
                newName: "IX_employees_KeycloakSubjectId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_employees",
                table: "employees",
                column: "EmployeeId");
        }
    }
}
