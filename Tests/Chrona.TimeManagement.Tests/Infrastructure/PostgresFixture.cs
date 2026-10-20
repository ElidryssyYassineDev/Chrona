using Microsoft.EntityFrameworkCore;
using Npgsql;
using ProjectManagement;
using ProjectManagement.Domain;
using ProjectManagement.Infrastructure;
using TimeManagement;
using TimeManagement.Infrastructure;
using WorkAllocation;
using WorkAllocation.Contracts;
using WorkAllocation.Domain;
using WorkAllocation.Infrastructure;
using Workforce.Contracts;
using Workforce.Domain;
using Workforce.Infrastructure;
using Xunit;

namespace Chrona.TimeManagement.Tests.Infrastructure;

// Base jetable sur le Postgres de docker compose : les quatre modules migrés depuis zéro, dans l'ordre des FK.
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly string _adminConnectionString =
        Environment.GetEnvironmentVariable("CHRONA_TEST_PG_ADMIN")
        ?? "Host=localhost;Port=5432;Database=postgres;Username=keycloak;Password=keycloakAdmin";

    private readonly string _databaseName = $"chrona_tm_test_{Guid.NewGuid():N}";

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
        await using (var timesheets = CreateTimeContext()) await timesheets.Database.MigrateAsync();
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

    public TimeManagementDbContext CreateTimeContext()
    {
        var builder = new DbContextOptionsBuilder<TimeManagementDbContext>();
        TimeManagementModule.ConfigureDbContext(builder, ConnectionString);
        return new TimeManagementDbContext(builder.Options);
    }

    // Un jeu de contextes neuf par appel, comme une vraie requête HTTP.
    public async Task<T> RunAsync<T>(Func<TimeManagementDbContext, IWorkforceQueries, IAllocationLookup, Task<T>> action)
    {
        await using var timesheets = CreateTimeContext();
        await using var workforce = CreateWorkforceContext();
        await using var allocations = CreateAllocationContext();
        await using var projects = CreateProjectContext();
        return await action(timesheets, new WorkforceQueries(workforce), new AllocationLookup(allocations, new ProjectLookup(projects)));
    }

    public async Task<(Guid EmployeeId, Guid SubjectId)> NewEmployeeAsync()
    {
        await using var db = CreateWorkforceContext();
        var department = new Department($"Dept-{Guid.NewGuid():N}");
        var subjectId = Guid.NewGuid();
        var employee = new Employee(subjectId, "Alice", "Nguyen", department.Id);

        db.Departments.Add(department);
        db.Employees.Add(employee);
        await db.SaveChangesAsync();
        return (employee.Id, subjectId);
    }

    // Crée un projet actif puis une allocation de l'employé sur ce projet.
    public async Task<Guid> NewAllocationAsync(Guid employeeId, string start, string end, bool active = true)
    {
        await using var projectsDb = CreateProjectContext();
        var project = new Project($"Project-{Guid.NewGuid():N}");
        projectsDb.Projects.Add(project);
        await projectsDb.SaveChangesAsync();

        await using var allocationsDb = CreateAllocationContext();
        var allocation = new Allocation(
            employeeId, project.Id, new AllocationPeriod(DateOnly.Parse(start), DateOnly.Parse(end)), new AllocationPercentage(50m));
        if (!active) allocation.Cancel();

        allocationsDb.Allocations.Add(allocation);
        await allocationsDb.SaveChangesAsync();
        return allocation.Id;
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

[CollectionDefinition(TimeDbCollection.Name)]
public sealed class TimeDbCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "time-management-db";
}