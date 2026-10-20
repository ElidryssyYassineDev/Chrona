using Chrona.Api.Infrastructure;
using Chrona.Shared.Application;
using WorkAllocation.Application.Allocations;
using WorkAllocation.Application.Allocations.CancelAllocation;
using WorkAllocation.Application.Allocations.CreateAllocation;
using WorkAllocation.Application.Allocations.GetAllocation;
using WorkAllocation.Application.Allocations.GetCapacity;
using WorkAllocation.Application.Allocations.ListAllocations;
using WorkAllocation.Application.Allocations.ModifyAllocation;

namespace Chrona.Api.Endpoints;

public sealed record CreateAllocationRequest(
    Guid EmployeeId, Guid ProjectId, DateOnly PeriodStart, DateOnly PeriodEnd, decimal Percentage);

public sealed record ModifyAllocationRequest(DateOnly PeriodStart, DateOnly PeriodEnd, decimal Percentage);

public static class AllocationEndpoints
{
    private static Caller CallerOf(ICurrentUserContext user) =>
        new(user.SubjectId, user.Roles.Contains(AuthorizationPolicies.Manager));

    public static IEndpointRouteBuilder MapAllocationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/allocations");

        // Déclaré avant /{id:guid} : "capacity" n'est pas un GUID, il n'y a donc aucune ambiguïté de route.
        group.MapGet("/capacity", async (
                Guid employeeId, DateOnly periodStart, DateOnly periodEnd, Guid? excludeAllocationId, GetCapacityHandler handler) =>
            {
                var result = await handler.HandleAsync(new GetCapacityQuery(employeeId, periodStart, periodEnd, excludeAllocationId));
                return result.IsSuccess ? Results.Ok(result.Value) : result.Error!.ToErrorResult();
            })
            .RequireAuthorization(AuthorizationPolicies.Manager);

        group.MapGet("/", async (Guid? employeeId, Guid? projectId, ICurrentUserContext user, ListAllocationsHandler handler) =>
                Results.Ok(await handler.HandleAsync(new ListAllocationsQuery(CallerOf(user), employeeId, projectId))))
            .RequireAuthorization(AuthorizationPolicies.Employee);

        group.MapGet("/{id:guid}", async (Guid id, ICurrentUserContext user, GetAllocationHandler handler) =>
            {
                var result = await handler.HandleAsync(new GetAllocationQuery(id, CallerOf(user)));
                return result.IsSuccess ? Results.Ok(result.Value) : result.Error!.ToErrorResult();
            })
            .RequireAuthorization(AuthorizationPolicies.Employee);

        group.MapPost("/", async (CreateAllocationRequest r, CreateAllocationHandler handler) =>
            {
                var result = await handler.HandleAsync(
                    new CreateAllocationCommand(r.EmployeeId, r.ProjectId, r.PeriodStart, r.PeriodEnd, r.Percentage));
                return result.IsSuccess
                    ? Results.Created($"/api/v1/allocations/{result.Value.AllocationId}", result.Value)
                    : result.Error!.ToErrorResult();
            })
            .RequireAuthorization(AuthorizationPolicies.Manager);

        group.MapPut("/{id:guid}", async (Guid id, ModifyAllocationRequest r, ModifyAllocationHandler handler) =>
            {
                var result = await handler.HandleAsync(new ModifyAllocationCommand(id, r.PeriodStart, r.PeriodEnd, r.Percentage));
                return result.IsSuccess ? Results.Ok(result.Value) : result.Error!.ToErrorResult();
            })
            .RequireAuthorization(AuthorizationPolicies.Manager);

        group.MapPost("/{id:guid}/cancel", async (Guid id, CancelAllocationHandler handler) =>
            {
                var result = await handler.HandleAsync(new CancelAllocationCommand(id));
                return result.IsSuccess ? Results.Ok(result.Value) : result.Error!.ToErrorResult();
            })
            .RequireAuthorization(AuthorizationPolicies.Manager);

        return app;
    }
}