using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagement.Application.Members.AddProjectMember;
using ProjectManagement.Application.Members.ListProjectMembers;
using ProjectManagement.Application.Members.RemoveProjectMember;
using ProjectManagement.Application.Projects.ArchiveProject;
using ProjectManagement.Application.Projects.CreateProject;
using ProjectManagement.Application.Projects.GetProject;
using ProjectManagement.Application.Projects.ListProjects;
using ProjectManagement.Application.Projects.UpdateProject;
using ProjectManagement.Infrastructure;

namespace ProjectManagement;

public static class ProjectManagementModule
{
    // Historique de migrations propre au module (08-database-design.md, §2).
    public const string MigrationsHistoryTable = "ef_migrations_history_project_management";

    // Partagé avec les tests d'intégration pour garantir exactement la même configuration.
    public static void ConfigureDbContext(DbContextOptionsBuilder options, string connectionString) =>
        options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable(MigrationsHistoryTable))
               .UseSnakeCaseNamingConvention();

    public static IServiceCollection AddProjectManagementModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ProjectManagementDbContext>(o => ConfigureDbContext(o, connectionString));

        services.AddScoped<CreateProjectHandler>();
        services.AddScoped<UpdateProjectHandler>();
        services.AddScoped<ArchiveProjectHandler>();
        services.AddScoped<GetProjectHandler>();
        services.AddScoped<ListProjectsHandler>();
        services.AddScoped<AddProjectMemberHandler>();
        services.AddScoped<RemoveProjectMemberHandler>();
        services.AddScoped<ListProjectMembersHandler>();

        return services;
    }
}