using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace TimeManagement.Infrastructure;

internal static class DbUpdateExceptionExtensions
{
    public static bool IsUniqueViolation(this DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}