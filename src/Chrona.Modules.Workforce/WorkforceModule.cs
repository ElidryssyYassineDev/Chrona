using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Workforce.Application.Departments.CreateDepartment;
using Workforce.Application.Departments.DeleteDepartment;
using Workforce.Application.Departments.ListDepartments;
using Workforce.Application.Departments.RenameDepartment;
using Workforce.Application.Employees.CreateEmployee;
using Workforce.Application.Employees.DeactivateEmployee;
using Workforce.Application.Employees.GetEmployee;
using Workforce.Application.Employees.GetEmployeeForPrincipal;
using Workforce.Application.Employees.GetMyProfile;
using Workforce.Application.Employees.ListEmployees;
using Workforce.Application.Employees.UpdateEmployee;
using Workforce.Infrastructure;

namespace Workforce;

public static class WorkforceModule
{
    public static IServiceCollection AddWorkforceModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<WorkforceDbContext>(options =>
            options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());

        // Capacité inter-modules (utilisée par M5/M6)
        services.AddScoped<GetEmployeeForPrincipalHandler>();

        services.AddScoped<CreateDepartmentHandler>();
        services.AddScoped<RenameDepartmentHandler>();
        services.AddScoped<DeleteDepartmentHandler>();
        services.AddScoped<ListDepartmentsHandler>();

        services.AddScoped<CreateEmployeeHandler>();
        services.AddScoped<UpdateEmployeeHandler>();
        services.AddScoped<DeactivateEmployeeHandler>();
        services.AddScoped<GetEmployeeHandler>();
        services.AddScoped<ListEmployeesHandler>();
        services.AddScoped<GetMyProfileHandler>();

        return services;
    }
}