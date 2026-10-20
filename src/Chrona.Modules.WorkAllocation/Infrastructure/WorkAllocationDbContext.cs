using Microsoft.EntityFrameworkCore;
using WorkAllocation.Domain;

namespace WorkAllocation.Infrastructure;

public class WorkAllocationDbContext(DbContextOptions<WorkAllocationDbContext> options) : DbContext(options)
{
    public DbSet<Allocation> Allocations => Set<Allocation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AllocationConfiguration).Assembly);
}