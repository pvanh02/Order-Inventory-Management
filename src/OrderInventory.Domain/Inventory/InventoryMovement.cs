namespace OrderInventory.Domain.Inventory;

public enum MovementType { Receipt = 1, Adjustment = 2, Sale = 3, CancellationReturn = 4 }

public sealed class InventoryMovement
{
    private InventoryMovement() { }
    public long Id { get; set; }
    public int ProductId { get; private set; }
    public int Delta { get; private set; }
    public int BalanceAfter { get; private set; }
    public MovementType Type { get; private set; }
    public int ActorId { get; private set; }
    public string? Reason { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public int? OrderLineId { get; private set; }

    public static InventoryMovement Create(int productId, int delta, int balanceAfter, MovementType type, int actorId, string? reason, DateTimeOffset occurredAt, int? orderLineId = null)
    {
        if (productId <= 0) throw new ArgumentOutOfRangeException(nameof(productId));
        if (actorId <= 0) throw new ArgumentOutOfRangeException(nameof(actorId));
        if (delta is 0 or int.MinValue || !Enum.IsDefined(type) || type == MovementType.Receipt && delta < 0)
            throw new ArgumentOutOfRangeException(nameof(delta));
        if (balanceAfter < 0) throw new ArgumentOutOfRangeException(nameof(balanceAfter));
        if (type is MovementType.Sale or MovementType.CancellationReturn
            ? orderLineId is null or <= 0 || (type == MovementType.Sale ? delta >= 0 : delta <= 0)
            : orderLineId.HasValue)
            throw new ArgumentException("Sale/return movements require a source line and the corresponding delta sign.");
        var cleanedReason = reason?.Trim();
        if (cleanedReason?.Length > 512 || type == MovementType.Adjustment && string.IsNullOrWhiteSpace(cleanedReason))
            throw new ArgumentException("An adjustment requires a reason of at most 512 characters.", nameof(reason));
        return new InventoryMovement
        {
            ProductId = productId, Delta = delta, BalanceAfter = balanceAfter, Type = type,
            ActorId = actorId, Reason = string.IsNullOrEmpty(cleanedReason) ? null : cleanedReason, OccurredAt = occurredAt, OrderLineId = orderLineId
        };
    }
}
