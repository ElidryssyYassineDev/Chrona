using Chrona.Shared.Application;
using Chrona.WorkAllocation.Tests.Infrastructure;
using Npgsql;
using WorkAllocation.Application.Allocations;
using WorkAllocation.Application.Allocations.CancelAllocation;
using WorkAllocation.Application.Allocations.CreateAllocation;
using WorkAllocation.Application.Allocations.GetAllocation;
using WorkAllocation.Application.Allocations.GetCapacity;
using WorkAllocation.Application.Allocations.ListAllocations;
using WorkAllocation.Application.Allocations.ModifyAllocation;
using Xunit;

namespace Chrona.WorkAllocation.Tests.Application;

[Collection(AllocationDbCollection.Name)]
public class AllocationHandlerTests(PostgresFixture db)
{
    private static void AssertError(Result result, ErrorKind kind, string code, string? field = null)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(kind, result.Error!.Kind);
        Assert.Equal(code, result.Error.Code);
        Assert.Equal(field, result.Error.Field);
    }

    private Task<Result<AllocationResponse>> CreateAsync(Guid employeeId, Guid projectId, string start, string end, decimal pct) =>
        db.RunAsync((wa, wf, pm) => new CreateAllocationHandler(wa, wf, pm).HandleAsync(
            new CreateAllocationCommand(employeeId, projectId, DateOnly.Parse(start), DateOnly.Parse(end), pct)));

    private Task<Result<AllocationResponse>> ModifyAsync(Guid id, string start, string end, decimal pct) =>
        db.RunAsync((wa, wf, pm) => new ModifyAllocationHandler(wa, wf, pm).HandleAsync(
            new ModifyAllocationCommand(id, DateOnly.Parse(start), DateOnly.Parse(end), pct)));

    private Task<Result<AllocationResponse>> CancelAsync(Guid id) =>
        db.RunAsync((wa, wf, pm) => new CancelAllocationHandler(wa, wf, pm).HandleAsync(new CancelAllocationCommand(id)));

    private Task<Result<AllocationDetailResponse>> GetAsync(Guid id, Caller caller) =>
        db.RunAsync((wa, wf, pm) => new GetAllocationHandler(wa, wf, pm).HandleAsync(new GetAllocationQuery(id, caller)));

    private static readonly Caller Manager = new(Guid.NewGuid(), true);

    // ---- Création et capacité ----

    [Fact]
    public async Task Create_Succeeds_WithNamesAndAFirstHistoryEntry()
    {
        var employee = await db.NewEmployeeAsync();
        var project = await db.NewProjectAsync();

        var created = await CreateAsync(employee.EmployeeId, project, "2026-08-03", "2026-08-28", 50m);
        var detail = await GetAsync(created.Value.AllocationId, Manager);

        Assert.True(created.IsSuccess, created.Error?.Message);
        Assert.Equal("Alice Nguyen", created.Value.EmployeeName);
        Assert.Equal("Active", created.Value.Status);
        var entry = Assert.Single(detail.Value.History);
        Assert.Equal("Created", entry.ChangeType);
    }

    [Fact]
    public async Task Create_ExactlyFullCapacity_Succeeds_OneUnitOver_IsRejected()
    {
        var employee = await db.NewEmployeeAsync();
        var project = await db.NewProjectAsync();
        await CreateAsync(employee.EmployeeId, project, "2026-08-03", "2026-08-28", 60m);

        var tooMuch = await CreateAsync(employee.EmployeeId, project, "2026-08-03", "2026-08-28", 40.01m);
        var exact = await CreateAsync(employee.EmployeeId, project, "2026-08-03", "2026-08-28", 40m);

        AssertError(tooMuch, ErrorKind.Conflict, "capacity_exceeded");
        Assert.True(exact.IsSuccess, exact.Error?.Message);
    }

    [Fact]
    public async Task Create_AdjacentPeriods_BothSucceedAt100()
    {
        var employee = await db.NewEmployeeAsync();
        var project = await db.NewProjectAsync();

        var first = await CreateAsync(employee.EmployeeId, project, "2026-08-01", "2026-08-15", 100m);
        var second = await CreateAsync(employee.EmployeeId, project, "2026-08-16", "2026-08-31", 100m);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess, second.Error?.Message);
    }

    [Fact]
    public async Task Create_OverlappingPeriod_IsRejected()
    {
        var employee = await db.NewEmployeeAsync();
        var project = await db.NewProjectAsync();
        await CreateAsync(employee.EmployeeId, project, "2026-08-01", "2026-08-15", 100m);

        var overlapping = await CreateAsync(employee.EmployeeId, project, "2026-08-15", "2026-08-20", 10m);

        AssertError(overlapping, ErrorKind.Conflict, "capacity_exceeded");
    }

    [Fact]
    public async Task Create_ConcurrentRequests_NeverExceedCapacity()
    {
        var employee = await db.NewEmployeeAsync();
        var project = await db.NewProjectAsync();

        var results = await Task.WhenAll(
            CreateAsync(employee.EmployeeId, project, "2026-08-03", "2026-08-28", 60m),
            CreateAsync(employee.EmployeeId, project, "2026-08-03", "2026-08-28", 60m));

        Assert.Equal(1, results.Count(r => r.IsSuccess));
        Assert.Equal(1, results.Count(r => r.IsFailure && r.Error!.Code == "capacity_exceeded"));
    }

    [Fact]
    public async Task Create_UnknownOrInactiveEmployee_IsRejected()
    {
        var project = await db.NewProjectAsync();
        var inactive = await db.NewEmployeeAsync(active: false);

        var unknown = await CreateAsync(Guid.NewGuid(), project, "2026-08-03", "2026-08-28", 50m);
        var deactivated = await CreateAsync(inactive.EmployeeId, project, "2026-08-03", "2026-08-28", 50m);

        AssertError(unknown, ErrorKind.Validation, "validation_error", "employeeId");
        AssertError(deactivated, ErrorKind.Validation, "validation_error", "employeeId");
    }

    [Fact]
    public async Task Create_UnknownOrArchivedProject_IsRejected()
    {
        var employee = await db.NewEmployeeAsync();
        var archived = await db.NewProjectAsync(active: false);

        var unknown = await CreateAsync(employee.EmployeeId, Guid.NewGuid(), "2026-08-03", "2026-08-28", 50m);
        var archivedResult = await CreateAsync(employee.EmployeeId, archived, "2026-08-03", "2026-08-28", 50m);

        AssertError(unknown, ErrorKind.Validation, "validation_error", "projectId");
        AssertError(archivedResult, ErrorKind.Validation, "validation_error", "projectId");
    }

    [Theory]
    [InlineData("2026-08-28", "2026-08-03", 50, "periodEnd")]
    [InlineData("2026-08-03", "2026-08-28", 0, "percentage")]
    [InlineData("2026-08-03", "2026-08-28", 101, "percentage")]
    public async Task Create_InvalidInput_ReturnsValidationError(string start, string end, int pct, string field)
    {
        var employee = await db.NewEmployeeAsync();
        var project = await db.NewProjectAsync();

        var result = await CreateAsync(employee.EmployeeId, project, start, end, pct);

        AssertError(result, ErrorKind.Validation, "validation_error", field);
    }

    // ---- Modification, annulation, historique ----

    [Fact]
    public async Task Modify_RechecksCapacity_AndRecordsHistory()
    {
        var employee = await db.NewEmployeeAsync();
        var project = await db.NewProjectAsync();
        await CreateAsync(employee.EmployeeId, project, "2026-08-03", "2026-08-28", 60m);
        var second = (await CreateAsync(employee.EmployeeId, project, "2026-08-03", "2026-08-28", 40m)).Value;

        var tooMuch = await ModifyAsync(second.AllocationId, "2026-08-03", "2026-08-28", 41m);
        var ok = await ModifyAsync(second.AllocationId, "2026-08-10", "2026-08-28", 30m);
        var detail = await GetAsync(second.AllocationId, Manager);

        AssertError(tooMuch, ErrorKind.Conflict, "capacity_exceeded");
        Assert.True(ok.IsSuccess, ok.Error?.Message);
        Assert.Equal("Created", detail.Value.History[0].ChangeType);
        Assert.Contains(detail.Value.History, h => h.ChangeType == "PeriodChanged");
        var percentageChange = Assert.Single(detail.Value.History, h => h.ChangeType == "PercentageChanged");
        Assert.Equal("40", percentageChange.OldValue);
        Assert.Equal("30", percentageChange.NewValue);
    }

    [Fact]
    public async Task Modify_DoesNotCountTheAllocationItself()
    {
        var employee = await db.NewEmployeeAsync();
        var project = await db.NewProjectAsync();
        var allocation = (await CreateAsync(employee.EmployeeId, project, "2026-08-03", "2026-08-28", 100m)).Value;

        var result = await ModifyAsync(allocation.AllocationId, "2026-08-05", "2026-08-30", 100m);

        Assert.True(result.IsSuccess, result.Error?.Message);
    }

    [Fact]
    public async Task Cancel_FreesCapacity_AndIsTerminal()
    {
        var employee = await db.NewEmployeeAsync();
        var project = await db.NewProjectAsync();
        var full = (await CreateAsync(employee.EmployeeId, project, "2026-08-03", "2026-08-28", 100m)).Value;

        var blocked = await CreateAsync(employee.EmployeeId, project, "2026-08-03", "2026-08-28", 100m);
        var cancelled = await CancelAsync(full.AllocationId);
        var freed = await CreateAsync(employee.EmployeeId, project, "2026-08-03", "2026-08-28", 100m);
        var again = await CancelAsync(full.AllocationId);
        var modifyAfter = await ModifyAsync(full.AllocationId, "2026-08-03", "2026-08-28", 10m);

        AssertError(blocked, ErrorKind.Conflict, "capacity_exceeded");
        Assert.Equal("Cancelled", cancelled.Value.Status);
        Assert.True(freed.IsSuccess, freed.Error?.Message);
        AssertError(again, ErrorKind.Conflict, "allocation_cancelled");
        AssertError(modifyAfter, ErrorKind.Conflict, "allocation_cancelled");
    }

    [Fact]
    public async Task Cancel_AppendsAStatusChangedHistoryEntry()
    {
        var employee = await db.NewEmployeeAsync();
        var project = await db.NewProjectAsync();
        var allocation = (await CreateAsync(employee.EmployeeId, project, "2026-08-03", "2026-08-28", 50m)).Value;

        await CancelAsync(allocation.AllocationId);
        var detail = await GetAsync(allocation.AllocationId, Manager);

        Assert.Equal(2, detail.Value.History.Count);
        var change = detail.Value.History.Single(h => h.ChangeType == "StatusChanged");
        Assert.Equal("Active", change.OldValue);
        Assert.Equal("Cancelled", change.NewValue);
    }

    [Fact]
    public async Task Capacity_ReportsPeakAndAvailable_AndExcludesTheEditedAllocation()
    {
        var employee = await db.NewEmployeeAsync();
        var project = await db.NewProjectAsync();
        await CreateAsync(employee.EmployeeId, project, "2026-08-01", "2026-08-15", 60m);
        var second = (await CreateAsync(employee.EmployeeId, project, "2026-08-10", "2026-08-31", 30m)).Value;

        var capacity = await db.RunAsync((wa, _, _) => new GetCapacityHandler(wa).HandleAsync(
            new GetCapacityQuery(employee.EmployeeId, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), null)));
        var excluding = await db.RunAsync((wa, _, _) => new GetCapacityHandler(wa).HandleAsync(
            new GetCapacityQuery(employee.EmployeeId, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), second.AllocationId)));

        Assert.Equal(90m, capacity.Value.PeakAllocatedPercentage);
        Assert.Equal(10m, capacity.Value.AvailablePercentage);
        Assert.Equal(60m, excluding.Value.PeakAllocatedPercentage);
    }

    // ---- Portée par rôle ----

    [Fact]
    public async Task Employee_SeesOnlyOwnAllocations_ManagerSeesAll()
    {
        var alice = await db.NewEmployeeAsync();
        var bob = await db.NewEmployeeAsync();
        var project = await db.NewProjectAsync();
        var aliceAllocation = (await CreateAsync(alice.EmployeeId, project, "2026-08-03", "2026-08-28", 50m)).Value;
        var bobAllocation = (await CreateAsync(bob.EmployeeId, project, "2026-08-03", "2026-08-28", 50m)).Value;
        var asAlice = new Caller(alice.SubjectId, false);

        var aliceList = await db.RunAsync((wa, wf, pm) => new ListAllocationsHandler(wa, wf, pm)
            .HandleAsync(new ListAllocationsQuery(asAlice, null, null)));
        var managerList = await db.RunAsync((wa, wf, pm) => new ListAllocationsHandler(wa, wf, pm)
            .HandleAsync(new ListAllocationsQuery(Manager, null, null)));
        var ownDetail = await GetAsync(aliceAllocation.AllocationId, asAlice);
        var otherDetail = await GetAsync(bobAllocation.AllocationId, asAlice);

        Assert.Contains(aliceList, a => a.AllocationId == aliceAllocation.AllocationId);
        Assert.DoesNotContain(aliceList, a => a.AllocationId == bobAllocation.AllocationId);
        Assert.Contains(managerList, a => a.AllocationId == bobAllocation.AllocationId);
        Assert.True(ownDetail.IsSuccess);
        AssertError(otherDetail, ErrorKind.NotFound, "not_found");
    }

    // ---- Contraintes réelles en base (les FK inter-modules ne sont pas dans le modèle EF) ----

    [Fact]
    public async Task Database_RefusesDeletingAnEmployeeOrProjectThatHasAllocations()
    {
        var employee = await db.NewEmployeeAsync();
        var project = await db.NewProjectAsync();
        await CreateAsync(employee.EmployeeId, project, "2026-08-03", "2026-08-28", 50m);

        await using var connection = new NpgsqlConnection(db.ConnectionString);
        await connection.OpenAsync();

        await using var deleteEmployee = new NpgsqlCommand("DELETE FROM employees WHERE employee_id = @id", connection);
        deleteEmployee.Parameters.AddWithValue("id", employee.EmployeeId);
        var employeeError = await Assert.ThrowsAsync<PostgresException>(() => deleteEmployee.ExecuteNonQueryAsync());

        await using var deleteProject = new NpgsqlCommand("DELETE FROM projects WHERE project_id = @id", connection);
        deleteProject.Parameters.AddWithValue("id", project);
        var projectError = await Assert.ThrowsAsync<PostgresException>(() => deleteProject.ExecuteNonQueryAsync());

        Assert.Equal("fk_allocations_employee_id", employeeError.ConstraintName);
        Assert.Equal("fk_allocations_project_id", projectError.ConstraintName);
    }
}