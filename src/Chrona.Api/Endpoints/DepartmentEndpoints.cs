using Chrona.Api.Infrastructure;
using Workforce.Application.Departments.CreateDepartment;
using Workforce.Application.Departments.DeleteDepartment;
using Workforce.Application.Departments.ListDepartments;
using Workforce.Application.Departments.RenameDepartment;

namespace Chrona.Api.Endpoints;

public sealed record DepartmentNameRequest(string Name);

public static class DepartmentEndpoints
{
    public static IEndpointRouteBuilder MapDepartmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/departments");

        group.MapGet("/", async (ListDepartmentsHandler handler) =>
                Results.Ok(await handler.HandleAsync(new ListDepartmentsQuery())))
            .RequireAuthorization(AuthorizationPolicies.Employee);

        group.MapPost("/", async (DepartmentNameRequest request, CreateDepartmentHandler handler) =>
            {
                var result = await handler.HandleAsync(new CreateDepartmentCommand(request.Name));
                return result.IsSuccess
                    ? Results.Created($"/api/v1/departments/{result.Value.DepartmentId}", result.Value)
                    : result.Error!.ToErrorResult();
            })
            .RequireAuthorization(AuthorizationPolicies.Manager);

        group.MapPut("/{id:guid}", async (Guid id, DepartmentNameRequest request, RenameDepartmentHandler handler) =>
            {
                var result = await handler.HandleAsync(new RenameDepartmentCommand(id, request.Name));
                return result.IsSuccess ? Results.Ok(result.Value) : result.Error!.ToErrorResult();
            })
            .RequireAuthorization(AuthorizationPolicies.Manager);

        group.MapDelete("/{id:guid}", async (Guid id, DeleteDepartmentHandler handler) =>
            {
                var result = await handler.HandleAsync(new DeleteDepartmentCommand(id));
                return result.IsSuccess ? Results.NoContent() : result.Error!.ToErrorResult();
            })
            .RequireAuthorization(AuthorizationPolicies.Manager);

        return app;
    }
}