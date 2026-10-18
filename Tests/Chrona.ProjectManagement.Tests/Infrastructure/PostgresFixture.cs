using Microsoft.EntityFrameworkCore;
using Npgsql;
using ProjectManagement;
using ProjectManagement.Infrastructure;
using Workforce.Contracts;
using Workforce.Domain;
using Workforce.Infrastructure;
using Xunit;

namespace Chrona.ProjectManagement.Tests.Infrastructure;

// Base jetable sur le Postgres de docker compose : Workforce puis ProjectManagement migrés depuis zéro.
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly string _adminConnectionString =
        Environment.GetEnvironmentVariable("CHRONA_TEST_PG_ADMIN")
        ?? "Host=localhost;Port=5432;Database=postgres;Username=keycloak;Password=keycloakAdmin";

    private readonly string _databaseName = $"chrona_pm_test_{Guid.NewGuid():N}";

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await using (var admin = new NpgsqlConnection(_adminConnectionString))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{_databaseName}\"", admin);
            await create.ExecuteNonQueryAsync();
        }

        ConnectionString = new NpgsqlConnectionStringBuilder(_adminConnectionString) { Database = _databaseName }
            .ConnectionString;

        await using (var workforce = CreateWorkforceContext())
        {
            await workforce.Database.MigrateAsync(); // d'abord : project_members référence employees
        }

        await using var projects = CreateProjectContext();
        await projects.Database.MigrateAsync();
    }

    public WorkforceDbContext CreateWorkforceContext() =>
        new(new DbContextOptionsBuilder<WorkforceDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options);

    public ProjectManagementDbContext CreateProjectContext()
    {
        var builder = new DbContextOptionsBuilder<ProjectManagementDbContext>();
        ProjectManagementModule.ConfigureDbContext(builder, ConnectionString);
        return new ProjectManagementDbContext(builder.Options);
    }

    // Un contexte neuf par appel, comme une vraie requête HTTP.
    public async Task<T> RunAsync<T>(Func<ProjectManagementDbContext, IWorkforceQueries, Task<T>> action)
    {
        await using var projects = CreateProjectContext();
        await using var workforce = CreateWorkforceContext();
        return await action(projects, new WorkforceQueries(workforce));
    }

    public async Task<Guid> NewEmployeeAsync(bool active = true)
    {
        await using var db = CreateWorkforceContext();
        var department = new Department($"Dept-{Guid.NewGuid():N}");
        var employee = new Employee(Guid.NewGuid(), "Alice", "Nguyen", department.Id);
        if (!active) employee.Deactivate();

        db.Departments.Add(department);
        db.Employees.Add(employee);
        await db.SaveChangesAsync();
        return employee.Id;
    }

    public async Task DisposeAsync()
    {
        NpgsqlConnection.ClearAllPools();
        await using var admin = new NpgsqlConnection(_adminConnectionString);
        await admin.OpenAsync();
        await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)", admin);
        await drop.ExecuteNonQueryAsync();
    }
}

[CollectionDefinition(ProjectDbCollection.Name)]
public sealed class ProjectDbCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "project-management-db";
}