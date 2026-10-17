using Microsoft.EntityFrameworkCore;
using Npgsql;
using Workforce.Infrastructure;
using Xunit;

namespace Chrona.Workforce.Tests.Infrastructure;

// Base jetable créée sur le Postgres de docker compose, migrée depuis zéro, supprimée à la fin.
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly string _adminConnectionString =
        Environment.GetEnvironmentVariable("CHRONA_TEST_PG_ADMIN")
        ?? "Host=localhost;Port=5432;Database=postgres;Username=keycloak;Password=keycloakAdmin";

    private readonly string _databaseName = $"chrona_test_{Guid.NewGuid():N}";
    private string _connectionString = string.Empty;

    public async Task InitializeAsync()
    {
        await using (var admin = new NpgsqlConnection(_adminConnectionString))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{_databaseName}\"", admin);
            await create.ExecuteNonQueryAsync();
        }

        _connectionString = new NpgsqlConnectionStringBuilder(_adminConnectionString) { Database = _databaseName }
            .ConnectionString;

        await using var db = CreateContext();
        await db.Database.MigrateAsync(); // valide aussi toute la chaîne de migrations sur une base vide
    }

    public WorkforceDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<WorkforceDbContext>()
            .UseNpgsql(_connectionString)
            .UseSnakeCaseNamingConvention()
            .Options);

    // Un contexte neuf par appel, comme une vraie requête HTTP.
    public async Task<T> RunAsync<T>(Func<WorkforceDbContext, Task<T>> action)
    {
        await using var context = CreateContext();
        return await action(context);
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

[CollectionDefinition(WorkforceDbCollection.Name)]
public sealed class WorkforceDbCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "workforce-db";
}