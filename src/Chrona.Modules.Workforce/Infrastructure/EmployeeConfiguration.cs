using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Workforce.Domain;

namespace Workforce.Infrastructure;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees", t =>
            t.HasCheckConstraint(
                "ck_employees_deactivated_at_consistency",
                "is_active = true OR deactivated_at_utc IS NOT NULL"));

        builder.Property(e => e.Id).HasColumnName("employee_id");

        builder.HasIndex(e => e.KeycloakSubjectId)
            .IsUnique()
            .HasDatabaseName("uk_employees_keycloak_subject_id");

        // Même module : FK réelle, sans propriété de navigation (08-database-design.md, §5).
        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(e => e.DepartmentId)
            .HasConstraintName("fk_employees_department_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(e => e.ManagerId)
            .HasConstraintName("fk_employees_manager_id")
            .OnDelete(DeleteBehavior.SetNull);
    }
}