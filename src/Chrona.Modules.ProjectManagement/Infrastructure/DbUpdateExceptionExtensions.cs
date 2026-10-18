using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ProjectManagement.Infrastructure;

internal static class DbUpdateExceptionExtensions
{
    public static bool IsUniqueViolation(this DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    public static bool IsForeignKeyViolation(this DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation };
}