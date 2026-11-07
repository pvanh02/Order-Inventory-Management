using OrderInventory.Domain.Inventory;

namespace OrderInventory.Application.Abstractions;

public sealed record StockChange(int ProductId, int Delta, MovementType Type, int ActorId, string? Reason, DateTimeOffset OccurredAt, int? OrderLineId = null);

public interface IInventoryWriter
{
    // Requires the owner's active transaction. Updates stock and stages its movement; never commits or retries.
    Task<InventoryMovement> ApplyAsync(StockChange change, CancellationToken cancellationToken);
}
