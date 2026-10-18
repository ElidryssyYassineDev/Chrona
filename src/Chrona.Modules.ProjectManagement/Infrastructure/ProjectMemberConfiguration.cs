using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagement.Domain;

namespace ProjectManagement.Infrastructure;

public class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> builder)
    {
        builder.ToTable("project_members");
        builder.HasKey(m => new { m.ProjectId, m.EmployeeId });

        // Le PK composite indexe déjà project_id ; employee_id (colonne de queue) a besoin du sien.
        builder.HasIndex(m => m.EmployeeId).HasDatabaseName("ix_project_members_employee_id");

        // La FK vers employees (autre module) n'est PAS dans le modèle EF : elle est ajoutée à la main dans la migration.
    }
}