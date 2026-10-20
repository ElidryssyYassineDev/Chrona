using Microsoft.EntityFrameworkCore;
using TimeManagement.Domain;

namespace TimeManagement.Infrastructure;

public class TimeManagementDbContext(DbContextOptions<TimeManagementDbContext> options) : DbContext(options)
{
    // Seule la racine d'agrégat a un DbSet ; TimeEntry est atteint via Timesheet.
    public DbSet<Timesheet> Timesheets => Set<Timesheet>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TimesheetConfiguration).Assembly);
}