using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkAllocation.Domain;

namespace WorkAllocation.Infrastructure;

public class AllocationConfiguration : IEntityTypeConfiguration<Allocation>
{
    public void Configure(EntityTypeBuilder<Allocation> builder)
    {
        builder.ToTable("allocations", t =>
        {
            t.HasCheckConstraint("ck_allocations_percentage_range", "percentage > 0 AND percentage <= 100");
            t.HasCheckConstraint("ck_allocations_period_valid", "period_start <= period_end");
        });

        // Identifiant généré côté client : sans ValueGeneratedNever, EF prendrait un enfant ajouté à un parent
        // déjà chargé pour une ligne existante (UPDATE au lieu d'INSERT).
        builder.Property(a => a.Id).HasColumnName("allocation_id").ValueGeneratedNever();
        builder.Property(a => a.Percentage).HasColumnType("numeric(5,2)");
        builder.Property(a => a.Status).HasConversion<string>().IsRequired();
        builder.Ignore(a => a.Period);

        // Index composite du Capacity Validator (08-database-design.md, §4) + index de la FK vers projects.
        builder.HasIndex(a => new { a.EmployeeId, a.Status }).HasDatabaseName("ix_allocations_employee_id_status");
        builder.HasIndex(a => a.ProjectId).HasDatabaseName("ix_allocations_project_id");

        builder.HasMany(a => a.History)
            .WithOne()
            .HasForeignKey(h => h.AllocationId)
            .HasConstraintName("fk_allocation_history_allocation_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(a => a.History).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Les FK vers employees et projects (autres modules) ne sont pas dans le modèle EF : elles sont
        // ajoutées à la main dans la migration.
    }
}