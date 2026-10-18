using System.Linq.Expressions;
using Chrona.Shared.Application;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Domain;
using ProjectManagement.Infrastructure;
using Workforce.Contracts;

namespace ProjectManagement.Application.Projects;

public sealed record ProjectResponse(
    Guid ProjectId, string Name, string Status, DateTimeOffset CreatedAtUtc, DateTimeOffset? ArchivedAtUtc, int MemberCount);

public sealed record ProjectMemberResponse(
    Guid EmployeeId, string EmployeeName, bool EmployeeIsActive, DateTimeOffset AddedAtUtc);

internal static class ProjectErrors
{
    public static readonly Error NameRequired =
        new("validation_error", "Project name must not be empty.", ErrorKind.Validation, "name");

    public static readonly Error NotFound =
        new("not_found", "No project was found with the given id.", ErrorKind.NotFound);

    public static readonly Error Archived =
        new("project_archived", "This project is archived and can no longer be changed.", ErrorKind.Conflict);

    public static readonly Error AlreadyMember =
        new("already_a_member", "This employee is already a member of the project.", ErrorKind.Conflict);

    public static readonly Error NotAMember =
        new("member_not_found", "This employee is not a member of the project.", ErrorKind.NotFound);

    public static readonly Error EmployeeInvalid =
        new("validation_error", "employeeId must reference an existing, active employee.", ErrorKind.Validation, "employeeId");

    public static Error NameTaken(string name) =>
        new("project_name_taken", $"A project named '{name}' already exists.", ErrorKind.Conflict);
}

internal static class ProjectProjection
{
    public static async Task<List<ProjectResponse>> ListAsync(
        ProjectManagementDbContext db, Expression<Func<Project, bool>>? filter = null)
    {
        var query = db.Projects.AsNoTracking();
        if (filter is not null)
        {
            query = query.Where(filter);
        }

        // Projection anonyme côté SQL, puis mapping en mémoire (le statut devient une chaîne).
        var rows = await query
            .OrderBy(p => p.Name)
            .Select(p => new { p.Id, p.Name, p.Status, p.CreatedAtUtc, p.ArchivedAtUtc, MemberCount = p.Members.Count() })
            .ToListAsync();

        return rows
            .Select(r => new ProjectResponse(r.Id, r.Name, r.Status.ToString(), r.CreatedAtUtc, r.ArchivedAtUtc, r.MemberCount))
            .ToList();
    }

    public static async Task<ProjectResponse?> GetAsync(ProjectManagementDbContext db, Guid projectId) =>
        (await ListAsync(db, p => p.Id == projectId)).FirstOrDefault();

    public static ProjectMemberResponse ToMemberResponse(
        ProjectMember member, IReadOnlyDictionary<Guid, EmployeeSummary> employees)
    {
        employees.TryGetValue(member.EmployeeId, out var employee);
        return new ProjectMemberResponse(
            member.EmployeeId, employee?.FullName ?? "Unknown employee", employee?.IsActive ?? false, member.AddedAtUtc);
    }
}