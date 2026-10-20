using Microsoft.EntityFrameworkCore;

namespace TimeManagement.Infrastructure;

internal static class TimesheetLock
{
    // Verrou consultatif valable jusqu'à la fin de la transaction : sérialise ajout, modification et soumission
    // d'une même feuille, pour qu'une entrée ne s'ajoute jamais à une feuille déjà soumise.
    public static Task AcquireAsync(TimeManagementDbContext db, Guid timesheetId) =>
        db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({timesheetId.ToString()}, 0))");
}