using Microsoft.EntityFrameworkCore;

namespace WorkAllocation.Infrastructure;

internal static class EmployeeCapacityLock
{
    // Verrou consultatif PostgreSQL valable jusqu'à la fin de la transaction : il sérialise les contrôles
    // de capacité d'un même employé (le contrôle porte sur plusieurs lignes, la base ne peut pas l'imposer).
    public static Task AcquireAsync(WorkAllocationDbContext db, Guid employeeId) =>
        db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({employeeId.ToString()}, 0))");
}