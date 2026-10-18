using Microsoft.EntityFrameworkCore;
using ProjectManagement.Domain;

namespace ProjectManagement.Infrastructure;

public class ProjectManagementDbContext(DbContextOptions<ProjectManagementDbContext> options) : DbContext(options)
{
    // Seule la racine d'agrégat a un DbSet ; ProjectMember est atteint via Project.
    public DbSet<Project> Projects => Set<Project>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProjectConfiguration).Assembly);
}