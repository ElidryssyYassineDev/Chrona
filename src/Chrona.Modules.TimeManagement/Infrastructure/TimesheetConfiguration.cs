using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeManagement.Domain;

namespace TimeManagement.Infrastructure;

public class TimesheetConfiguration : IEntityTypeConfiguration<Timesheet>
{
    public void Configure(EntityTypeBuilder<Timesheet> builder)
    {
        builder.ToTable("timesheets", t =>
            t.HasCheckConstraint("ck_timesheets_period_valid", "period_start <= period_end"));

        // Identifiant généré côté client : sans ValueGeneratedNever, EF prendrait une entrée ajoutée à une feuille
        // déjà chargée pour une ligne existante (UPDATE au lieu d'INSERT).
        builder.Property(t => t.Id).HasColumnName("timesheet_id").ValueGeneratedNever();
        builder.Property(t => t.Status).HasConversion<string>().IsRequired();

        // Une feuille par employé et par période exacte (04-use-cases.md, Create Timesheet) ;
        // cet index composite remplace ix_timesheets_employee_id (colonne de tête = employee_id).
        builder.HasIndex(t => new { t.EmployeeId, t.PeriodStart, t.PeriodEnd })
            .IsUnique()
            .HasDatabaseName("uk_timesheets_employee_id_period");

        // Sert View Pending Approvals (08-database-design.md, §4).
        builder.HasIndex(t => t.Status).HasDatabaseName("ix_timesheets_status");

        builder.HasMany(t => t.Entries)
            .WithOne()
            .HasForeignKey(e => e.TimesheetId)
            .HasConstraintName("fk_time_entries_timesheet_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(t => t.Entries).UsePropertyAccessMode(PropertyAccessMode.Field);

        // La FK vers employees (autre module) n'est pas dans le modèle EF : elle est ajoutée à la main dans la migration.
    }
}