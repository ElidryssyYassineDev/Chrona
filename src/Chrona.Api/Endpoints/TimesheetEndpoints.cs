using Chrona.Api.Infrastructure;
using Chrona.Shared.Application;
using TimeManagement.Application.Timesheets.AddTimeEntry;
using TimeManagement.Application.Timesheets.CreateTimesheet;
using TimeManagement.Application.Timesheets.EditTimeEntry;
using TimeManagement.Application.Timesheets.GetTimesheet;
using TimeManagement.Application.Timesheets.ListTimesheets;
using TimeManagement.Application.Timesheets.SubmitTimesheet;

namespace Chrona.Api.Endpoints;

public sealed record CreateTimesheetRequest(DateOnly PeriodStart, DateOnly PeriodEnd);

public sealed record AddTimeEntryRequest(Guid AllocationId, DateOnly EntryDate, decimal Hours);

public sealed record EditTimeEntryRequest(DateOnly EntryDate, decimal Hours);

public static class TimesheetEndpoints
{
    public static IEndpointRouteBuilder MapTimesheetEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/timesheets");

        group.MapGet("/", async (string? status, ICurrentUserContext user, ListTimesheetsHandler handler) =>
            {
                var result = await handler.HandleAsync(new ListTimesheetsQuery(user.SubjectId, status));
                return result.IsSuccess ? Results.Ok(result.Value) : result.Error!.ToErrorResult();
            })
            .RequireAuthorization(AuthorizationPolicies.Employee);

        group.MapPost("/", async (CreateTimesheetRequest r, ICurrentUserContext user, CreateTimesheetHandler handler) =>
            {
                var result = await handler.HandleAsync(new CreateTimesheetCommand(user.SubjectId, r.PeriodStart, r.PeriodEnd));
                if (result.IsFailure) return result.Error!.ToErrorResult();

                var created = result.Value;
                return created.Created
                    ? Results.Created($"/api/v1/timesheets/{created.Timesheet.TimesheetId}", created.Timesheet)
                    : Results.Ok(created.Timesheet); // feuille existante réutilisée
            })
            .RequireAuthorization(AuthorizationPolicies.Employee);

        group.MapGet("/{id:guid}", async (Guid id, ICurrentUserContext user, GetTimesheetHandler handler) =>
            {
                var result = await handler.HandleAsync(new GetTimesheetQuery(user.SubjectId, id));
                return result.IsSuccess ? Results.Ok(result.Value) : result.Error!.ToErrorResult();
            })
            .RequireAuthorization(AuthorizationPolicies.Employee);

        group.MapPost("/{id:guid}/time-entries", async (Guid id, AddTimeEntryRequest r, ICurrentUserContext user, AddTimeEntryHandler handler) =>
            {
                var result = await handler.HandleAsync(new AddTimeEntryCommand(user.SubjectId, id, r.AllocationId, r.EntryDate, r.Hours));
                return result.IsSuccess
                    ? Results.Created($"/api/v1/timesheets/{id}", result.Value)
                    : result.Error!.ToErrorResult();
            })
            .RequireAuthorization(AuthorizationPolicies.Employee);

        group.MapPut("/{id:guid}/time-entries/{entryId:guid}", async (
                Guid id, Guid entryId, EditTimeEntryRequest r, ICurrentUserContext user, EditTimeEntryHandler handler) =>
            {
                var result = await handler.HandleAsync(new EditTimeEntryCommand(user.SubjectId, id, entryId, r.EntryDate, r.Hours));
                return result.IsSuccess ? Results.Ok(result.Value) : result.Error!.ToErrorResult();
            })
            .RequireAuthorization(AuthorizationPolicies.Employee);

        group.MapPost("/{id:guid}/submit", async (Guid id, ICurrentUserContext user, SubmitTimesheetHandler handler) =>
            {
                var result = await handler.HandleAsync(new SubmitTimesheetCommand(user.SubjectId, id));
                return result.IsSuccess ? Results.Ok(result.Value) : result.Error!.ToErrorResult();
            })
            .RequireAuthorization(AuthorizationPolicies.Employee);

        return app;
    }
}