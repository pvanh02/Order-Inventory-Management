using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OrderInventory.Application.Abstractions;
using OrderInventory.Application.Common;
using OrderInventory.Domain.Inventory;

namespace OrderInventory.Infrastructure.Persistence;

public sealed class EfInventoryWriter(OrderInventoryDbContext context, ICatalogData catalog) : IInventoryWriter
{
    public async Task<InventoryMovement> ApplyAsync(StockChange change, CancellationToken cancellationToken)
    {
        var transaction = context.Database.CurrentTransaction ?? throw new InvalidOperationException("Stock writes require the owner's transaction.");
        // Validate before executing SQL, including when called from a seed/workflow core.
        _ = InventoryMovement.Create(change.ProductId, change.Delta, 0, change.Type, change.ActorId, change.Reason, change.OccurredAt);
        var product = await catalog.FindProductAsync(change.ProductId, true, cancellationToken);
        if (product is null || product.IsDeleted) throw new NotFoundException("The product was not found.");

        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = """
            UPDATE [InventoryBalances]
            SET [OnHand] = CONVERT(int, CONVERT(bigint, [OnHand]) + @delta)
            OUTPUT INSERTED.[OnHand]
            WHERE [ProductId] = @productId
              AND CONVERT(bigint, [OnHand]) + @delta BETWEEN 0 AND 2147483647;
            """;
        command.Parameters.Add(new SqlParameter("@productId", SqlDbType.Int) { Value = change.ProductId });
        command.Parameters.Add(new SqlParameter("@delta", SqlDbType.Int) { Value = change.Delta });
        var updated = await PersistenceErrors.ExecuteAsync(() => command.ExecuteScalarAsync(cancellationToken));
        if (updated is null || updated is DBNull)
        {
            _ = await catalog.ReadOnHandAsync(change.ProductId, cancellationToken);
            throw change.Delta < 0
                ? new ConflictException("insufficient_stock", "The stock is insufficient for this change.")
                : new ConflictException("stock_overflow", "The stock would exceed the supported quantity limit.");
        }
        var movement = InventoryMovement.Create(change.ProductId, change.Delta, (int)updated, change.Type, change.ActorId, change.Reason, change.OccurredAt);
        context.InventoryMovements.Add(movement);
        return movement;
    }
}
