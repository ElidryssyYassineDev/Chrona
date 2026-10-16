using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Workforce.Migrations
{
    /// <inheritdoc />
    public partial class EnforceEmployeeIntegrity : Migration
    {

        
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            // Réparation des données existantes avant d'ajouter les clés étrangères :
            // on garantit un département "General", on y rattache les employés orphelins,
            // et on annule les références manager invalides (aucune aujourd'hui).
            migrationBuilder.Sql(@"
            INSERT INTO departments (department_id, name, created_at_utc)
            SELECT gen_random_uuid(), 'General', now()
            WHERE NOT EXISTS (SELECT 1 FROM departments WHERE name = 'General');

            UPDATE employees e
            SET department_id = (SELECT department_id FROM departments WHERE name = 'General')
            WHERE NOT EXISTS (SELECT 1 FROM departments d WHERE d.department_id = e.department_id);

            UPDATE employees e
            SET manager_id = NULL
            WHERE manager_id IS NOT NULL
            AND NOT EXISTS (SELECT 1 FROM employees m WHERE m.employee_id = e.manager_id);
            ");
            migrationBuilder.RenameIndex(
                name: "ix_employees_keycloak_subject_id",
                table: "employees",
                newName: "uk_employees_keycloak_subject_id");

            migrationBuilder.CreateIndex(
                name: "ix_employees_department_id",
                table: "employees",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "ix_employees_manager_id",
                table: "employees",
                column: "manager_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_employees_deactivated_at_consistency",
                table: "employees",
                sql: "is_active = true OR deactivated_at_utc IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_employees_department_id",
                table: "employees",
                column: "department_id",
                principalTable: "departments",
                principalColumn: "department_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_employees_manager_id",
                table: "employees",
                column: "manager_id",
                principalTable: "employees",
                principalColumn: "employee_id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_employees_department_id",
                table: "employees");

            migrationBuilder.DropForeignKey(
                name: "fk_employees_manager_id",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "ix_employees_department_id",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "ix_employees_manager_id",
                table: "employees");

            migrationBuilder.DropCheckConstraint(
                name: "ck_employees_deactivated_at_consistency",
                table: "employees");

            migrationBuilder.RenameIndex(
                name: "uk_employees_keycloak_subject_id",
                table: "employees",
                newName: "ix_employees_keycloak_subject_id");
        }
    }
}
