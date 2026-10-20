using Chrona.Shared.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Workforce.Contracts;
using WorkAllocation.Application.Allocations.CancelAllocation;
using WorkAllocation.Application.Allocations.CreateAllocation;
using WorkAllocation.Application.Allocations.GetAllocation;
using WorkAllocation.Application.Allocations.GetCapacity;
using WorkAllocation.Application.Allocations.ListAllocations;
using WorkAllocation.Application.Allocations.ModifyAllocation;
using WorkAllocation.Application.Events;
using WorkAllocation.Infrastructure;
using WorkAllocation.Contracts;

namespace WorkAllocation;

public static class WorkAllocationModule
{
    public const string MigrationsHistoryTable = "ef_migrations_history_work_allocation";

    public static void ConfigureDbContext(DbContextOptionsBuilder options, string connectionString) =>
        options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable(MigrationsHistoryTable))
               .UseSnakeCaseNamingConvention();

    public static IServiceCollection AddWorkAllocationModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<WorkAllocationDbContext>(o => ConfigureDbContext(o, connectionString));

        services.AddScoped<CreateAllocationHandler>();
        services.AddScoped<ModifyAllocationHandler>();
        services.AddScoped<CancelAllocationHandler>();
        services.AddScoped<GetAllocationHandler>();
        services.AddScoped<ListAllocationsHandler>();
        services.AddScoped<GetCapacityHandler>();
        services.AddScoped<IAllocationLookup, AllocationLookup>();

        // Abonnement à l'événement publié par Workforce (06-domain-model.md, §7).
        services.AddScoped<IDomainEventHandler<EmployeeDeactivated>, EmployeeDeactivatedHandler>();

        return services;
    }
}