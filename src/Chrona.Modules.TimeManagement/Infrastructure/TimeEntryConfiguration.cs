using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeManagement.Domain;

namespace TimeManagement.Infrastructure;

public class TimeEntryConfiguration : IEntityTypeConfiguration<TimeEntry>
{
    public void Configure(EntityTypeBuilder<TimeEntry> builder)
    {
        builder.ToTable("time_entries", t =>
            t.HasCheckConstraint("ck_time_entries_hours_range", "hours > 0 AND hours <= 24"));

        builder.Property(e => e.Id).HasColumnName("time_entry_id").ValueGeneratedNever();
        builder.Property(e => e.Hours).HasColumnType("numeric(4,2)");

        builder.HasIndex(e => e.TimesheetId).HasDatabaseName("ix_time_entries_timesheet_id");
        builder.HasIndex(e => e.AllocationId).HasDatabaseName("ix_time_entries_allocation_id");

        // La FK vers allocations (autre module) n'est pas dans le modèle EF : elle est ajoutée à la main dans la migration.
    }
}