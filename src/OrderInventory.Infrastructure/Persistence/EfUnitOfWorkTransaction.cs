using Microsoft.EntityFrameworkCore.Storage;
using OrderInventory.Application.Abstractions;

namespace OrderInventory.Infrastructure.Persistence;

internal sealed class EfUnitOfWorkTransaction(IDbContextTransaction transaction) : IUnitOfWorkTransaction
{
    public async Task CommitAsync(CancellationToken cancellationToken) =>
        await PersistenceErrors.ExecuteAsync(async () =>
        {
            await transaction.CommitAsync(cancellationToken);
            return true;
        });
    public ValueTask DisposeAsync() => transaction.DisposeAsync();
}
