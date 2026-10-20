using Microsoft.EntityFrameworkCore;
using Npgsql;
using ProjectManagement;
using ProjectManagement.Contracts;
using ProjectManagement.Domain;
using ProjectManagement.Infrastructure;
using WorkAllocation;
using WorkAllocation.Infrastructure;
using Workforce.Contracts;
using Workforce.Domain;
using Workforce.Infrastructure;
using Xunit;

namespace Chrona.WorkAllocation.Tests.Infrastructure;

// Base jetable sur le Postgres de docker compose : les trois modules migrés depuis zéro, dans l'ordre des FK.
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly string _adminConnectionString =
        Environment.GetEnvironmentVariable("CHRONA_TEST_PG_ADMIN")
        ?? "Host=localhost;Port=5432;Database=postgres;Username=keycloak;Password=keycloakAdmin";

    private readonly string _databaseName = $"chrona_wa_test_{Guid.NewGuid():N}";

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

        await using (var workforce = CreateWorkforceContext()) await workforce.Database.MigrateAsync();
        await using (var projects = CreateProjectContext()) await projects.Database.MigrateAsync();
        await using (var allocations = CreateAllocationContext()) await allocations.Database.MigrateAsync();
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

    public WorkAllocationDbContext CreateAllocationContext()
    {
        var builder = new DbContextOptionsBuilder<WorkAllocationDbContext>();
        WorkAllocationModule.ConfigureDbContext(builder, ConnectionString);
        return new WorkAllocationDbContext(builder.Options);
    }

    // Un jeu de contextes neuf par appel, comme une vraie requête HTTP.
    public async Task<T> RunAsync<T>(Func<WorkAllocationDbContext, IWorkforceQueries, IProjectLookup, Task<T>> action)
    {
        await using var allocations = CreateAllocationContext();
        await using var workforce = CreateWorkforceContext();
        await using var projects = CreateProjectContext();
        return await action(allocations, new WorkforceQueries(workforce), new ProjectLookup(projects));
    }

    public async Task<(Guid EmployeeId, Guid SubjectId)> NewEmployeeAsync(bool active = true)
    {
        await using var db = CreateWorkforceContext();
        var department = new Department($"Dept-{Guid.NewGuid():N}");
        var subjectId = Guid.NewGuid();
        var employee = new Employee(subjectId, "Alice", "Nguyen", department.Id);
        if (!active) employee.Deactivate();

        db.Departments.Add(department);
        db.Employees.Add(employee);
        await db.SaveChangesAsync();
        return (employee.Id, subjectId);
    }

    public async Task<Guid> NewProjectAsync(bool active = true)
    {
        await using var db = CreateProjectContext();
        var project = new Project($"Project-{Guid.NewGuid():N}");
        if (!active) project.Archive();

        db.Projects.Add(project);
        await db.SaveChangesAsync();
        return project.Id;
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

[CollectionDefinition(AllocationDbCollection.Name)]
public sealed class AllocationDbCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "work-allocation-db";
}