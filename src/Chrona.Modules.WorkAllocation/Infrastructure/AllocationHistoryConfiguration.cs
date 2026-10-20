using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkAllocation.Domain;

namespace WorkAllocation.Infrastructure;

public class AllocationHistoryConfiguration : IEntityTypeConfiguration<AllocationHistory>
{
    public void Configure(EntityTypeBuilder<AllocationHistory> builder)
    {
        builder.ToTable("allocation_history", t =>
            t.HasCheckConstraint(
                "ck_allocation_history_change_type_valid",
                "change_type IN ('Created', 'StatusChanged', 'PeriodChanged', 'PercentageChanged')"));

        builder.Property(h => h.Id).HasColumnName("allocation_history_id").ValueGeneratedNever();
        builder.Property(h => h.ChangeType).HasConversion<string>().IsRequired();
        builder.HasIndex(h => h.AllocationId).HasDatabaseName("ix_allocation_history_allocation_id");
    }
}