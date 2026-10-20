using Chrona.Shared.Application;
using Microsoft.EntityFrameworkCore;
using TimeManagement.Domain;
using TimeManagement.Infrastructure;
using WorkAllocation.Contracts;
using Workforce.Contracts;

namespace TimeManagement.Application.Timesheets.EditTimeEntry;

public sealed record EditTimeEntryCommand(Guid SubjectId, Guid TimesheetId, Guid TimeEntryId, DateOnly EntryDate, decimal Hours);

public sealed class EditTimeEntryHandler(TimeManagementDbContext db, IWorkforceQueries workforce, IAllocationLookup allocations)
    : ICommandHandler<EditTimeEntryCommand, Result<TimeEntryResponse>>
{
    public async Task<Result<TimeEntryResponse>> HandleAsync(EditTimeEntryCommand command)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        await TimesheetLock.AcquireAsync(db, command.TimesheetId);

        var loaded = await TimesheetAccess.LoadOwnedAsync(db, workforce, command.SubjectId, command.TimesheetId);
        if (loaded.IsFailure) return Result<TimeEntryResponse>.Failure(loaded.Error!);
        var timesheet = loaded.Value;

        if (timesheet.Status != TimesheetStatus.Draft) return Result<TimeEntryResponse>.Failure(TimesheetErrors.NotDraft);

        var existing = timesheet.Entries.FirstOrDefault(e => e.Id == command.TimeEntryId);
        if (existing is null) return Result<TimeEntryResponse>.Failure(TimesheetErrors.EntryNotFound);

        // Mêmes contrôles qu'à l'ajout, sur l'allocation de l'entrée et la date résultante.
        var validation = await EntryRules.ValidateAsync(timesheet, existing.AllocationId, command.EntryDate, command.Hours, allocations);
        if (validation.IsFailure) return Result<TimeEntryResponse>.Failure(validation.Error!);

        var entry = timesheet.EditEntry(existing.Id, command.EntryDate, command.Hours);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        var allocation = validation.Value;
        var summaries = new Dictionary<Guid, AllocationSummary> { [allocation.AllocationId] = allocation };
        return Result<TimeEntryResponse>.Success(TimesheetMapper.ToEntryResponse(entry, summaries));
    }
}