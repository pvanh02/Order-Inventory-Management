using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OrderInventory.Application.Common;

namespace OrderInventory.Infrastructure.Persistence;

internal static class PersistenceErrors
{
    public static async Task<T> ExecuteAsync<T>(Func<Task<T>> operation,
        string uniqueIndex = "IX_UserAccounts_NormalizedUserName", string duplicateCode = "duplicate_username",
        string duplicateMessage = "The user name is already in use.")
    {
        try { return await operation(); }
        catch (DbUpdateConcurrencyException) { throw WriteConflict(); }
        catch (DbUpdateException error) when (error.InnerException is SqlException { Number: 2601 or 2627 } sql &&
            sql.Message.Contains(uniqueIndex, StringComparison.Ordinal))
        { throw new ConflictException(duplicateCode, duplicateMessage); }
        catch (Exception error) when (ContainsDeadlock(error)) { throw WriteConflict(); }
    }
    private static ConflictException WriteConflict() =>
        new("write_conflict", "The data changed concurrently. Reload and retry the operation.");

    private static bool ContainsDeadlock(Exception error)
    {
        // The SQL Server execution strategy can wrap a deadlock even when retries are disabled.
        for (Exception? current = error; current is not null; current = current.InnerException)
            if (current is SqlException { Number: 1205 }) return true;
        return false;
    }
}
