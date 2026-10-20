using Chrona.Shared.Application;
using Microsoft.EntityFrameworkCore;
using TimeManagement.Domain;
using TimeManagement.Infrastructure;
using WorkAllocation.Contracts;
using Workforce.Contracts;

namespace TimeManagement.Application.Timesheets.CreateTimesheet;

public sealed record CreateTimesheetCommand(Guid SubjectId, DateOnly PeriodStart, DateOnly PeriodEnd);

public sealed record CreateTimesheetResult(TimesheetResponse Timesheet, bool Created);

public sealed class CreateTimesheetHandler(TimeManagementDbContext db, IWorkforceQueries workforce, IAllocationLookup allocations)
    : ICommandHandler<CreateTimesheetCommand, Result<CreateTimesheetResult>>
{
    public async Task<Result<CreateTimesheetResult>> HandleAsync(CreateTimesheetCommand command)
    {
        if (command.PeriodStart == default) return Fail(TimesheetErrors.PeriodRequired);
        if (command.PeriodEnd < command.PeriodStart) return Fail(TimesheetErrors.PeriodInvalid);

        var employeeId = await workforce.GetEmployeeIdForPrincipalAsync(command.SubjectId);
        if (employeeId is null) return Fail(TimesheetErrors.EmployeeNotFound);
        var owner = employeeId.Value;

        // Même employé et même période : on réutilise la feuille existante (04-use-cases.md, Create Timesheet).
        var existing = await FindAsync(owner, command);
        if (existing is not null) return await ReuseAsync(existing);

        var timesheet = new Timesheet(owner, command.PeriodStart, command.PeriodEnd);
        db.Timesheets.Add(timesheet);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            // Course : une requête concurrente a créé la même feuille entre le contrôle et l'insertion.
            db.ChangeTracker.Clear();
            var winner = await FindAsync(owner, command);
            if (winner is not null) return await ReuseAsync(winner);
            throw;
        }

        var response = await TimesheetMapper.ToResponseAsync(timesheet, allocations);
        return Result<CreateTimesheetResult>.Success(new CreateTimesheetResult(response, Created: true));
    }

    private Task<Timesheet?> FindAsync(Guid employeeId, CreateTimesheetCommand command) =>
        db.Timesheets.AsNoTracking().Include(t => t.Entries).FirstOrDefaultAsync(t =>
            t.EmployeeId == employeeId && t.PeriodStart == command.PeriodStart && t.PeriodEnd == command.PeriodEnd);

    private async Task<Result<CreateTimesheetResult>> ReuseAsync(Timesheet timesheet) =>
        Result<CreateTimesheetResult>.Success(
            new CreateTimesheetResult(await TimesheetMapper.ToResponseAsync(timesheet, allocations), Created: false));

    private static Result<CreateTimesheetResult> Fail(Error error) => Result<CreateTimesheetResult>.Failure(error);
}