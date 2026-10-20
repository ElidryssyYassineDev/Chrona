using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TimeManagement.Application.Timesheets.AddTimeEntry;
using TimeManagement.Application.Timesheets.CreateTimesheet;
using TimeManagement.Application.Timesheets.EditTimeEntry;
using TimeManagement.Application.Timesheets.GetTimesheet;
using TimeManagement.Application.Timesheets.ListTimesheets;
using TimeManagement.Application.Timesheets.SubmitTimesheet;
using TimeManagement.Infrastructure;

namespace TimeManagement;

public static class TimeManagementModule
{
    public const string MigrationsHistoryTable = "ef_migrations_history_time_management";

    public static void ConfigureDbContext(DbContextOptionsBuilder options, string connectionString) =>
        options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable(MigrationsHistoryTable))
               .UseSnakeCaseNamingConvention();

    public static IServiceCollection AddTimeManagementModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<TimeManagementDbContext>(o => ConfigureDbContext(o, connectionString));

        services.AddScoped<CreateTimesheetHandler>();
        services.AddScoped<GetTimesheetHandler>();
        services.AddScoped<ListTimesheetsHandler>();
        services.AddScoped<AddTimeEntryHandler>();
        services.AddScoped<EditTimeEntryHandler>();
        services.AddScoped<SubmitTimesheetHandler>();

        return services;
    }
}