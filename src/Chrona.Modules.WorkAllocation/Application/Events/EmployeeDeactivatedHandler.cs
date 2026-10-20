using Chrona.Shared.Application;
using Microsoft.EntityFrameworkCore;
using Workforce.Contracts;
using WorkAllocation.Domain;
using WorkAllocation.Infrastructure;

namespace WorkAllocation.Application.Events;

// Annule toutes les allocations ACTIVES de l'employé désactivé, même celles dont la période est écoulée
// (06-domain-model.md, §7). Idempotent : sans allocation active, il n'y a rien à faire.
public sealed class EmployeeDeactivatedHandler(WorkAllocationDbContext db) : IDomainEventHandler<EmployeeDeactivated>
{
    public async Task HandleAsync(EmployeeDeactivated domainEvent)
    {
        var active = await db.Allocations
            .Where(a => a.EmployeeId == domainEvent.EmployeeId && a.Status == AllocationStatus.Active)
            .ToListAsync();

        if (active.Count == 0) return;

        foreach (var allocation in active)
        {
            allocation.Cancel(); // écrit l'entrée StatusChanged dans l'historique
        }

        await db.SaveChangesAsync();
    }
}