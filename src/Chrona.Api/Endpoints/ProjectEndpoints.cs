using Chrona.Api.Infrastructure;
using ProjectManagement.Application.Members.AddProjectMember;
using ProjectManagement.Application.Members.ListProjectMembers;
using ProjectManagement.Application.Members.RemoveProjectMember;
using ProjectManagement.Application.Projects.ArchiveProject;
using ProjectManagement.Application.Projects.CreateProject;
using ProjectManagement.Application.Projects.GetProject;
using ProjectManagement.Application.Projects.ListProjects;
using ProjectManagement.Application.Projects.UpdateProject;

namespace Chrona.Api.Endpoints;

public sealed record ProjectNameRequest(string Name);

public sealed record AddProjectMemberRequest(Guid EmployeeId);

public static class ProjectEndpoints
{
    public static IEndpointRouteBuilder MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/projects");

        group.MapGet("/", async (ListProjectsHandler handler) =>
                Results.Ok(await handler.HandleAsync(new ListProjectsQuery())))
            .RequireAuthorization(AuthorizationPolicies.Employee);

        group.MapGet("/{id:guid}", async (Guid id, GetProjectHandler handler) =>
            {
                var result = await handler.HandleAsync(new GetProjectQuery(id));
                return result.IsSuccess ? Results.Ok(result.Value) : result.Error!.ToErrorResult();
            })
            .RequireAuthorization(AuthorizationPolicies.Employee);

        group.MapPost("/", async (ProjectNameRequest request, CreateProjectHandler handler) =>
            {
                var result = await handler.HandleAsync(new CreateProjectCommand(request.Name));
                return result.IsSuccess
                    ? Results.Created($"/api/v1/projects/{result.Value.ProjectId}", result.Value)
                    : result.Error!.ToErrorResult();
            })
            .RequireAuthorization(AuthorizationPolicies.Manager);

        group.MapPut("/{id:guid}", async (Guid id, ProjectNameRequest request, UpdateProjectHandler handler) =>
            {
                var result = await handler.HandleAsync(new UpdateProjectCommand(id, request.Name));
                return result.IsSuccess ? Results.Ok(result.Value) : result.Error!.ToErrorResult();
            })
            .RequireAuthorization(AuthorizationPolicies.Manager);

        group.MapPost("/{id:guid}/archive", async (Guid id, ArchiveProjectHandler handler) =>
            {
                var result = await handler.HandleAsync(new ArchiveProjectCommand(id));
                return result.IsSuccess ? Results.Ok(result.Value) : result.Error!.ToErrorResult();
            })
            .RequireAuthorization(AuthorizationPolicies.Manager);

        group.MapGet("/{id:guid}/members", async (Guid id, ListProjectMembersHandler handler) =>
            {
                var result = await handler.HandleAsync(new ListProjectMembersQuery(id));
                return result.IsSuccess ? Results.Ok(result.Value) : result.Error!.ToErrorResult();
            })
            .RequireAuthorization(AuthorizationPolicies.Employee);

        group.MapPost("/{id:guid}/members", async (Guid id, AddProjectMemberRequest request, AddProjectMemberHandler handler) =>
            {
                var result = await handler.HandleAsync(new AddProjectMemberCommand(id, request.EmployeeId));
                return result.IsSuccess
                    ? Results.Created($"/api/v1/projects/{id}/members", result.Value)
                    : result.Error!.ToErrorResult();
            })
            .RequireAuthorization(AuthorizationPolicies.Manager);

        group.MapDelete("/{id:guid}/members/{employeeId:guid}", async (Guid id, Guid employeeId, RemoveProjectMemberHandler handler) =>
            {
                var result = await handler.HandleAsync(new RemoveProjectMemberCommand(id, employeeId));
                return result.IsSuccess ? Results.NoContent() : result.Error!.ToErrorResult();
            })
            .RequireAuthorization(AuthorizationPolicies.Manager);

        return app;
    }
}