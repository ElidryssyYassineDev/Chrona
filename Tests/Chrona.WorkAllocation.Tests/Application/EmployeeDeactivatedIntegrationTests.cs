using Chrona.Shared.Application;
using Chrona.WorkAllocation.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagement;
using WorkAllocation;
using WorkAllocation.Application.Allocations;
using WorkAllocation.Application.Allocations.CreateAllocation;
using WorkAllocation.Application.Allocations.GetAllocation;
using WorkAllocation.Application.Allocations.ListAllocations;
using Workforce;
using Workforce.Application.Employees.DeactivateEmployee;
using Xunit;

namespace Chrona.WorkAllocation.Tests.Application;

[Collection(AllocationDbCollection.Name)]
public class EmployeeDeactivatedIntegrationTests(PostgresFixture db)
{
    private ServiceProvider BuildProvider()
    {
        // Composition réelle : mêmes modules et même publisher qu'en production, sans HTTP.
        var services = new ServiceCollection();
        services.AddWorkforceModule(db.ConnectionString);
        services.AddProjectManagementModule(db.ConnectionString);
        services.AddWorkAllocationModule(db.ConnectionString);
        services.AddScoped<IDomainEventPublisher, InProcessDomainEventPublisher>();
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task DeactivatingAnEmployee_CancelsEveryActiveAllocation_AcrossModules()
    {
        var employee = await db.NewEmployeeAsync();
        var other = await db.NewEmployeeAsync();
        var project = await db.NewProjectAsync();

        await using var provider = BuildProvider();
        Guid current, elapsed, otherAllocation;
        using (var scope = provider.CreateScope())
        {
            var create = scope.ServiceProvider.GetRequiredService<CreateAllocationHandler>();
            // Une allocation en cours, une dont la période est déjà écoulée, et une d'un autre employé.
            current = (await create.HandleAsync(new CreateAllocationCommand(employee.EmployeeId, project, new DateOnly(2026, 8, 3), new DateOnly(2026, 8, 28), 50m))).Value.AllocationId;
            elapsed = (await create.HandleAsync(new CreateAllocationCommand(employee.EmployeeId, project, new DateOnly(2020, 1, 1), new DateOnly(2020, 1, 31), 50m))).Value.AllocationId;
            otherAllocation = (await create.HandleAsync(new CreateAllocationCommand(other.EmployeeId, project, new DateOnly(2026, 8, 3), new DateOnly(2026, 8, 28), 50m))).Value.AllocationId;
        }

        using (var scope = provider.CreateScope())
        {
            var deactivate = scope.ServiceProvider.GetRequiredService<DeactivateEmployeeHandler>();
            var first = await deactivate.HandleAsync(new DeactivateEmployeeCommand(employee.EmployeeId));
            var retry = await deactivate.HandleAsync(new DeactivateEmployeeCommand(employee.EmployeeId)); // idempotent

            Assert.True(first.IsSuccess);
            Assert.True(retry.IsSuccess);
        }

        var manager = new Caller(Guid.NewGuid(), true);
        foreach (var id in new[] { current, elapsed })
        {
            var detail = await db.RunAsync((wa, wf, pm) => new GetAllocationHandler(wa, wf, pm).HandleAsync(new GetAllocationQuery(id, manager)));
            Assert.Equal("Cancelled", detail.Value.Allocation.Status);
            Assert.Single(detail.Value.History, h => h.ChangeType == "StatusChanged"); // une seule annulation malgré le second appel
        }

        var untouched = await db.RunAsync((wa, wf, pm) => new GetAllocationHandler(wa, wf, pm).HandleAsync(new GetAllocationQuery(otherAllocation, manager)));
        Assert.Equal("Active", untouched.Value.Allocation.Status);
    }
}