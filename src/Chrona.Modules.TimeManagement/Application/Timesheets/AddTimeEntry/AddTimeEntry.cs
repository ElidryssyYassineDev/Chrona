using Chrona.Shared.Application;
using Microsoft.EntityFrameworkCore;
using TimeManagement.Domain;
using TimeManagement.Infrastructure;
using WorkAllocation.Contracts;
using Workforce.Contracts;

namespace TimeManagement.Application.Timesheets.AddTimeEntry;

public sealed record AddTimeEntryCommand(Guid SubjectId, Guid TimesheetId, Guid AllocationId, DateOnly EntryDate, decimal Hours);

public sealed class AddTimeEntryHandler(TimeManagementDbContext db, IWorkforceQueries workforce, IAllocationLookup allocations)
    : ICommandHandler<AddTimeEntryCommand, Result<TimeEntryResponse>>
{
    public async Task<Result<TimeEntryResponse>> HandleAsync(AddTimeEntryCommand command)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        await TimesheetLock.AcquireAsync(db, command.TimesheetId);

        var loaded = await TimesheetAccess.LoadOwnedAsync(db, workforce, command.SubjectId, command.TimesheetId);
        if (loaded.IsFailure) return Result<TimeEntryResponse>.Failure(loaded.Error!);
        var timesheet = loaded.Value;

        if (timesheet.Status != TimesheetStatus.Draft) return Result<TimeEntryResponse>.Failure(TimesheetErrors.NotDraft);

        var validation = await EntryRules.ValidateAsync(timesheet, command.AllocationId, command.EntryDate, command.Hours, allocations);
        if (validation.IsFailure) return Result<TimeEntryResponse>.Failure(validation.Error!);

        var entry = timesheet.AddEntry(command.AllocationId, command.EntryDate, command.Hours);
        db.Add(entry); // marque explicitement l'entrée comme ajoutée (identifiant renseigné côté client)
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        var allocation = validation.Value;
        var summaries = new Dictionary<Guid, AllocationSummary> { [allocation.AllocationId] = allocation };
        return Result<TimeEntryResponse>.Success(TimesheetMapper.ToEntryResponse(entry, summaries));
    }
}