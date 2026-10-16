
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Workforce.Application.Departments.CreateDepartment;
using Workforce.Application.Departments.DeleteDepartment;
using Workforce.Application.Departments.ListDepartments;
using Workforce.Application.Departments.RenameDepartment;
using Workforce.Application.Employees.GetEmployeeForPrincipal;
using Workforce.Infrastructure;

namespace Workforce;

public static class WorkforceModule
{
    public static IServiceCollection AddWorkforceModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<WorkforceDbContext>(options =>
            options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());

        services.AddScoped<GetEmployeeForPrincipalHandler>();
        services.AddScoped<CreateDepartmentHandler>();
        services.AddScoped<RenameDepartmentHandler>();
        services.AddScoped<DeleteDepartmentHandler>();
        services.AddScoped<ListDepartmentsHandler>();

        return services;
    }
}