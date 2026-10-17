using Chrona.Api.Infrastructure;
using Chrona.Shared.Application;
using Workforce.Application.Employees;
using Workforce.Application.Employees.CreateEmployee;
using Workforce.Application.Employees.DeactivateEmployee;
using Workforce.Application.Employees.GetEmployee;
using Workforce.Application.Employees.GetMyProfile;
using Workforce.Application.Employees.ListEmployees;
using Workforce.Application.Employees.UpdateEmployee;

namespace Chrona.Api.Endpoints;

public sealed record CreateEmployeeRequest(
    Guid KeycloakSubjectId, string FirstName, string LastName, Guid DepartmentId, Guid? ManagerId);

public sealed record UpdateEmployeeRequest(string FirstName, string LastName, Guid DepartmentId, Guid? ManagerId);

public sealed record MyProfileResponse(EmployeeResponse Employee, IReadOnlyList<string> Roles);

public static class EmployeeEndpoints
{
    private static readonly string[] AppRoles = [AuthorizationPolicies.Employee, AuthorizationPolicies.Manager];

    public static IEndpointRouteBuilder MapEmployeeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/employees");

        group.MapGet("/me", async (ICurrentUserContext currentUser, GetMyProfileHandler handler) =>
            {
                var employee = await handler.HandleAsync(new GetMyProfileQuery(currentUser.SubjectId));
                if (employee is null)
                    return Results.NotFound(new { message = "No employee record exists for this account" });

                var roles = currentUser.Roles.Where(r => AppRoles.Contains(r)).ToList();
                return Results.Ok(new MyProfileResponse(employee, roles));
            })
            .RequireAuthorization();

        group.MapGet("/", async (ListEmployeesHandler handler) =>
                Results.Ok(await handler.HandleAsync(new ListEmployeesQuery())))
            .RequireAuthorization(AuthorizationPolicies.Manager);

        group.MapGet("/{id:guid}", async (Guid id, GetEmployeeHandler handler) =>
            {
                var result = await handler.HandleAsync(new GetEmployeeQuery(id));
                return result.IsSuccess ? Results.Ok(result.Value) : result.Error!.ToErrorResult();
            })
            .RequireAuthorization(AuthorizationPolicies.Manager);

        group.MapPost("/", async (CreateEmployeeRequest r, CreateEmployeeHandler handler) =>
            {
                var result = await handler.HandleAsync(new CreateEmployeeCommand(
                    r.KeycloakSubjectId, r.FirstName, r.LastName, r.DepartmentId, r.ManagerId));
                return result.IsSuccess
                    ? Results.Created($"/api/v1/employees/{result.Value.EmployeeId}", result.Value)
                    : result.Error!.ToErrorResult();
            })
            .RequireAuthorization(AuthorizationPolicies.Manager);

        group.MapPut("/{id:guid}", async (Guid id, UpdateEmployeeRequest r, UpdateEmployeeHandler handler) =>
            {
                var result = await handler.HandleAsync(new UpdateEmployeeCommand(
                    id, r.FirstName, r.LastName, r.DepartmentId, r.ManagerId));
                return result.IsSuccess ? Results.Ok(result.Value) : result.Error!.ToErrorResult();
            })
            .RequireAuthorization(AuthorizationPolicies.Manager);

        group.MapPost("/{id:guid}/deactivate", async (Guid id, DeactivateEmployeeHandler handler) =>
            {
                var result = await handler.HandleAsync(new DeactivateEmployeeCommand(id));
                return result.IsSuccess ? Results.NoContent() : result.Error!.ToErrorResult();
            })
            .RequireAuthorization(AuthorizationPolicies.Manager);

        return app;
    }
}