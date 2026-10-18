using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagement.Domain;

namespace ProjectManagement.Infrastructure;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects", t =>
        {
            t.HasCheckConstraint("ck_projects_name_not_empty", "length(trim(name)) > 0");
            t.HasCheckConstraint("ck_projects_archived_at_consistency", "status <> 'Archived' OR archived_at_utc IS NOT NULL");
        });

        builder.Property(p => p.Id).HasColumnName("project_id");
        builder.Property(p => p.Name).IsRequired();
        builder.Property(p => p.Status).HasConversion<string>().IsRequired(); // texte, comme le reste du schéma

        builder.HasIndex(p => p.Name).IsUnique().HasDatabaseName("uk_projects_name");

        builder.HasMany(p => p.Members)
            .WithOne()
            .HasForeignKey(m => m.ProjectId)
            .HasConstraintName("fk_project_members_project_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Members).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}