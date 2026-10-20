using Chrona.Shared.Application;
using Chrona.TimeManagement.Tests.Infrastructure;
using Npgsql;
using TimeManagement.Application.Timesheets;
using TimeManagement.Application.Timesheets.AddTimeEntry;
using TimeManagement.Application.Timesheets.CreateTimesheet;
using TimeManagement.Application.Timesheets.EditTimeEntry;
using TimeManagement.Application.Timesheets.GetTimesheet;
using TimeManagement.Application.Timesheets.ListTimesheets;
using TimeManagement.Application.Timesheets.SubmitTimesheet;
using Xunit;

namespace Chrona.TimeManagement.Tests.Application;

[Collection(TimeDbCollection.Name)]
public class TimesheetHandlerTests(PostgresFixture db)
{
    private const string WeekStart = "2026-08-03";
    private const string WeekEnd = "2026-08-09";

    private sealed record Scenario(Guid EmployeeId, Guid SubjectId, Guid AllocationId, Guid TimesheetId);

    private static void AssertError(Result result, ErrorKind kind, string code, string? field = null)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(kind, result.Error!.Kind);
        Assert.Equal(code, result.Error.Code);
        Assert.Equal(field, result.Error.Field);
    }

    private Task<Result<CreateTimesheetResult>> CreateAsync(Guid subjectId, string start = WeekStart, string end = WeekEnd) =>
        db.RunAsync((ts, wf, al) => new CreateTimesheetHandler(ts, wf, al).HandleAsync(
            new CreateTimesheetCommand(subjectId, DateOnly.Parse(start), DateOnly.Parse(end))));

    private Task<Result<TimeEntryResponse>> AddAsync(Guid subjectId, Guid timesheetId, Guid allocationId, string date, decimal hours) =>
        db.RunAsync((ts, wf, al) => new AddTimeEntryHandler(ts, wf, al).HandleAsync(
            new AddTimeEntryCommand(subjectId, timesheetId, allocationId, DateOnly.Parse(date), hours)));

    private Task<Result<TimeEntryResponse>> EditAsync(Guid subjectId, Guid timesheetId, Guid entryId, string date, decimal hours) =>
        db.RunAsync((ts, wf, al) => new EditTimeEntryHandler(ts, wf, al).HandleAsync(
            new EditTimeEntryCommand(subjectId, timesheetId, entryId, DateOnly.Parse(date), hours)));

    private Task<Result<TimesheetResponse>> SubmitAsync(Guid subjectId, Guid timesheetId) =>
        db.RunAsync((ts, wf, al) => new SubmitTimesheetHandler(ts, wf, al).HandleAsync(
            new SubmitTimesheetCommand(subjectId, timesheetId)));

    private Task<Result<TimesheetResponse>> GetAsync(Guid subjectId, Guid timesheetId) =>
        db.RunAsync((ts, wf, al) => new GetTimesheetHandler(ts, wf, al).HandleAsync(new GetTimesheetQuery(subjectId, timesheetId)));

    // Un employé, une allocation (par défaut tout le mois d'août) et une feuille du 3 au 9 août.
    private async Task<Scenario> NewScenarioAsync(string allocationStart = "2026-08-01", string allocationEnd = "2026-08-31", bool allocationActive = true)
    {
        var employee = await db.NewEmployeeAsync();
        var allocationId = await db.NewAllocationAsync(employee.EmployeeId, allocationStart, allocationEnd, allocationActive);
        var timesheet = await CreateAsync(employee.SubjectId);
        Assert.True(timesheet.IsSuccess, timesheet.Error?.Message);
        return new Scenario(employee.EmployeeId, employee.SubjectId, allocationId, timesheet.Value.Timesheet.TimesheetId);
    }

    // ---- Création ----

    [Fact]
    public async Task Create_StartsInDraft_AndSamePeriodIsReusedNotDuplicated()
    {
        var employee = await db.NewEmployeeAsync();

        var first = await CreateAsync(employee.SubjectId);
        var second = await CreateAsync(employee.SubjectId);

        Assert.True(first.Value.Created);
        Assert.Equal("Draft", first.Value.Timesheet.Status);
        Assert.False(second.Value.Created);
        Assert.Equal(first.Value.Timesheet.TimesheetId, second.Value.Timesheet.TimesheetId);
    }

    [Fact]
    public async Task Create_ForTwoEmployeesOnTheSamePeriod_GivesTwoTimesheets()
    {
        var alice = await db.NewEmployeeAsync();
        var bob = await db.NewEmployeeAsync();

        var a = await CreateAsync(alice.SubjectId);
        var b = await CreateAsync(bob.SubjectId);

        Assert.NotEqual(a.Value.Timesheet.TimesheetId, b.Value.Timesheet.TimesheetId);
    }

    [Fact]
    public async Task Create_ConcurrentRequests_ProduceASingleTimesheet()
    {
        var employee = await db.NewEmployeeAsync();

        var results = await Task.WhenAll(CreateAsync(employee.SubjectId), CreateAsync(employee.SubjectId));

        Assert.All(results, r => Assert.True(r.IsSuccess, r.Error?.Message));
        Assert.Equal(1, results.Count(r => r.Value.Created));
        Assert.Equal(results[0].Value.Timesheet.TimesheetId, results[1].Value.Timesheet.TimesheetId);
    }

    [Fact]
    public async Task Create_WithEndBeforeStart_ReturnsValidationError()
    {
        var employee = await db.NewEmployeeAsync();

        var result = await CreateAsync(employee.SubjectId, "2026-08-09", "2026-08-03");

        AssertError(result, ErrorKind.Validation, "validation_error", "periodEnd");
    }

    [Fact]
    public async Task Create_ForAnAccountWithoutEmployeeRecord_ReturnsNotFound()
    {
        var result = await CreateAsync(Guid.NewGuid());

        AssertError(result, ErrorKind.NotFound, "employee_not_found");
    }

    // ---- Ajout d'une entrée : les deux contrôles de période, testés indépendamment ----

    [Fact]
    public async Task AddEntry_ValidEntry_IsRecordedWithProjectNameAndTotals()
    {
        var s = await NewScenarioAsync();

        var entry = await AddAsync(s.SubjectId, s.TimesheetId, s.AllocationId, "2026-08-05", 7.5m);
        var sheet = await GetAsync(s.SubjectId, s.TimesheetId);

        Assert.True(entry.IsSuccess, entry.Error?.Message);
        Assert.StartsWith("Project-", entry.Value.ProjectName);
        Assert.Single(sheet.Value.Entries);
        Assert.Equal(7.5m, sheet.Value.TotalHours);
    }

    [Fact]
    public async Task AddEntry_OutsideTheAllocationPeriod_ButInsideTheTimesheet_IsRejected()
    {
        // L'allocation ne couvre que le 5 et le 6 août ; la feuille couvre le 3 au 9.
        var s = await NewScenarioAsync(allocationStart: "2026-08-05", allocationEnd: "2026-08-06");

        var result = await AddAsync(s.SubjectId, s.TimesheetId, s.AllocationId, "2026-08-08", 8m);

        AssertError(result, ErrorKind.Conflict, "entry_outside_allocation_period");
    }

    [Fact]
    public async Task AddEntry_OutsideTheTimesheetPeriod_ButInsideTheAllocation_IsRejected()
    {
        // L'allocation couvre tout août, donc le 20 ; la feuille ne couvre que le 3 au 9.
        var s = await NewScenarioAsync();

        var result = await AddAsync(s.SubjectId, s.TimesheetId, s.AllocationId, "2026-08-20", 8m);

        AssertError(result, ErrorKind.Conflict, "entry_outside_timesheet_period");
    }

    [Fact]
    public async Task AddEntry_OnTheBoundaryDays_IsAccepted()
    {
        var s = await NewScenarioAsync(allocationStart: "2026-08-03", allocationEnd: "2026-08-09");

        var first = await AddAsync(s.SubjectId, s.TimesheetId, s.AllocationId, WeekStart, 4m);
        var last = await AddAsync(s.SubjectId, s.TimesheetId, s.AllocationId, WeekEnd, 4m);

        Assert.True(first.IsSuccess, first.Error?.Message);
        Assert.True(last.IsSuccess, last.Error?.Message);
    }

    [Fact]
    public async Task AddEntry_AgainstACancelledAllocation_IsRejected()
    {
        var s = await NewScenarioAsync(allocationActive: false);

        var result = await AddAsync(s.SubjectId, s.TimesheetId, s.AllocationId, "2026-08-05", 8m);

        AssertError(result, ErrorKind.Conflict, "allocation_not_active");
    }

    [Fact]
    public async Task AddEntry_AgainstAnotherEmployeesOrUnknownAllocation_IsRejected()
    {
        var s = await NewScenarioAsync();
        var other = await db.NewEmployeeAsync();
        var othersAllocation = await db.NewAllocationAsync(other.EmployeeId, "2026-08-01", "2026-08-31");

        var foreign = await AddAsync(s.SubjectId, s.TimesheetId, othersAllocation, "2026-08-05", 8m);
        var unknown = await AddAsync(s.SubjectId, s.TimesheetId, Guid.NewGuid(), "2026-08-05", 8m);

        AssertError(foreign, ErrorKind.Validation, "validation_error", "allocationId");
        AssertError(unknown, ErrorKind.Validation, "validation_error", "allocationId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(24.01)]
    [InlineData(1.234)]
    public async Task AddEntry_WithInvalidHours_ReturnsValidationError(double hours)
    {
        var s = await NewScenarioAsync();

        var result = await AddAsync(s.SubjectId, s.TimesheetId, s.AllocationId, "2026-08-05", (decimal)hours);

        AssertError(result, ErrorKind.Validation, "validation_error", "hours");
    }

    [Fact]
    public async Task AddEntry_OnAnotherEmployeesTimesheet_ReturnsNotFound()
    {
        var s = await NewScenarioAsync();
        var intruder = await db.NewEmployeeAsync();

        var result = await AddAsync(intruder.SubjectId, s.TimesheetId, s.AllocationId, "2026-08-05", 8m);

        AssertError(result, ErrorKind.NotFound, "not_found");
    }

    // ---- Modification ----

    [Fact]
    public async Task EditEntry_ChangesHoursAndDate()
    {
        var s = await NewScenarioAsync();
        var entry = (await AddAsync(s.SubjectId, s.TimesheetId, s.AllocationId, "2026-08-05", 4m)).Value;

        var edited = await EditAsync(s.SubjectId, s.TimesheetId, entry.TimeEntryId, "2026-08-06", 6m);
        var sheet = await GetAsync(s.SubjectId, s.TimesheetId);

        Assert.True(edited.IsSuccess, edited.Error?.Message);
        Assert.Equal(6m, sheet.Value.Entries.Single().Hours);
        Assert.Equal(new DateOnly(2026, 8, 6), sheet.Value.Entries.Single().EntryDate);
    }

    [Fact]
    public async Task EditEntry_MovingItOutsideEitherPeriod_IsRejected()
    {
        var s = await NewScenarioAsync(allocationStart: "2026-08-03", allocationEnd: "2026-08-06");
        var entry = (await AddAsync(s.SubjectId, s.TimesheetId, s.AllocationId, "2026-08-04", 4m)).Value;

        var outsideAllocation = await EditAsync(s.SubjectId, s.TimesheetId, entry.TimeEntryId, "2026-08-08", 4m);
        var outsideTimesheet = await EditAsync(s.SubjectId, s.TimesheetId, entry.TimeEntryId, "2026-08-20", 4m);

        AssertError(outsideAllocation, ErrorKind.Conflict, "entry_outside_allocation_period");
        AssertError(outsideTimesheet, ErrorKind.Conflict, "entry_outside_timesheet_period");
    }

    [Fact]
    public async Task EditEntry_Unknown_ReturnsNotFound()
    {
        var s = await NewScenarioAsync();

        var result = await EditAsync(s.SubjectId, s.TimesheetId, Guid.NewGuid(), "2026-08-05", 4m);

        AssertError(result, ErrorKind.NotFound, "time_entry_not_found");
    }

    // ---- Soumission et immutabilité ----

    [Fact]
    public async Task Submit_WithoutEntries_IsRejected()
    {
        var s = await NewScenarioAsync();

        var result = await SubmitAsync(s.SubjectId, s.TimesheetId);

        AssertError(result, ErrorKind.Conflict, "timesheet_empty");
    }

    [Fact]
    public async Task Submit_SetsStatusAndTimestamp_ThenFreezesEntries()
    {
        var s = await NewScenarioAsync();
        var entry = (await AddAsync(s.SubjectId, s.TimesheetId, s.AllocationId, "2026-08-05", 8m)).Value;

        var submitted = await SubmitAsync(s.SubjectId, s.TimesheetId);
        var again = await SubmitAsync(s.SubjectId, s.TimesheetId);
        var add = await AddAsync(s.SubjectId, s.TimesheetId, s.AllocationId, "2026-08-06", 1m);
        var edit = await EditAsync(s.SubjectId, s.TimesheetId, entry.TimeEntryId, "2026-08-05", 2m);

        Assert.True(submitted.IsSuccess, submitted.Error?.Message);
        Assert.Equal("Submitted", submitted.Value.Status);
        Assert.NotNull(submitted.Value.LastSubmittedAtUtc);
        AssertError(again, ErrorKind.Conflict, "timesheet_not_draft");
        AssertError(add, ErrorKind.Conflict, "timesheet_not_draft");
        AssertError(edit, ErrorKind.Conflict, "timesheet_not_draft");
    }

    [Fact]
    public async Task Submit_AnotherEmployeesTimesheet_ReturnsNotFound()
    {
        var s = await NewScenarioAsync();
        await AddAsync(s.SubjectId, s.TimesheetId, s.AllocationId, "2026-08-05", 8m);
        var intruder = await db.NewEmployeeAsync();

        var result = await SubmitAsync(intruder.SubjectId, s.TimesheetId);

        AssertError(result, ErrorKind.NotFound, "not_found");
    }

    // ---- Liste ----

    [Fact]
    public async Task List_ReturnsOnlyOwnTimesheets_AndFiltersByStatus()
    {
        var s = await NewScenarioAsync();
        var other = await db.NewEmployeeAsync();
        await CreateAsync(other.SubjectId);
        await AddAsync(s.SubjectId, s.TimesheetId, s.AllocationId, "2026-08-05", 8m);
        await SubmitAsync(s.SubjectId, s.TimesheetId);

        var all = await db.RunAsync((ts, wf, _) => new ListTimesheetsHandler(ts, wf).HandleAsync(new ListTimesheetsQuery(s.SubjectId, null)));
        var drafts = await db.RunAsync((ts, wf, _) => new ListTimesheetsHandler(ts, wf).HandleAsync(new ListTimesheetsQuery(s.SubjectId, "Draft")));
        var submitted = await db.RunAsync((ts, wf, _) => new ListTimesheetsHandler(ts, wf).HandleAsync(new ListTimesheetsQuery(s.SubjectId, "submitted")));
        var invalid = await db.RunAsync((ts, wf, _) => new ListTimesheetsHandler(ts, wf).HandleAsync(new ListTimesheetsQuery(s.SubjectId, "Nope")));

        Assert.Single(all.Value);
        Assert.Equal(1, all.Value[0].EntryCount);
        Assert.Equal(8m, all.Value[0].TotalHours);
        Assert.Empty(drafts.Value);
        Assert.Single(submitted.Value);
        AssertError(invalid, ErrorKind.Validation, "validation_error", "status");
    }

    // ---- Contraintes réelles en base (les FK inter-modules ne sont pas dans le modèle EF) ----

    [Fact]
    public async Task Database_RefusesDeletingAnAllocationThatHasTimeEntries()
    {
        var s = await NewScenarioAsync();
        await AddAsync(s.SubjectId, s.TimesheetId, s.AllocationId, "2026-08-05", 8m);

        await using var connection = new NpgsqlConnection(db.ConnectionString);
        await connection.OpenAsync();
        await using var delete = new NpgsqlCommand("DELETE FROM allocations WHERE allocation_id = @id", connection);
        delete.Parameters.AddWithValue("id", s.AllocationId);

        var ex = await Assert.ThrowsAsync<PostgresException>(() => delete.ExecuteNonQueryAsync());

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, ex.SqlState);
        Assert.Equal("fk_time_entries_allocation_id", ex.ConstraintName);
    }

    [Fact]
    public async Task Database_RefusesDeletingAnEmployeeThatHasATimesheet()
    {
        var employee = await db.NewEmployeeAsync(); // sans allocation : seule la FK de timesheets peut refuser
        await CreateAsync(employee.SubjectId);

        await using var connection = new NpgsqlConnection(db.ConnectionString);
        await connection.OpenAsync();
        await using var delete = new NpgsqlCommand("DELETE FROM employees WHERE employee_id = @id", connection);
        delete.Parameters.AddWithValue("id", employee.EmployeeId);

        var ex = await Assert.ThrowsAsync<PostgresException>(() => delete.ExecuteNonQueryAsync());

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, ex.SqlState);
        Assert.Equal("fk_timesheets_employee_id", ex.ConstraintName);
    }
}