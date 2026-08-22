using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Workforce.Domain;

namespace Workforce.Infrastructure;

public class WorkforceDbContext : DbContext
{
    public WorkforceDbContext(DbContextOptions<WorkforceDbContext> options) : base(options)
    {
        
    }

    public DbSet<Employee> Employees => Set<Employee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EmployeeConfiguration).Assembly);
    }
}