namespace OrderInventory.Application.Inventory;

public sealed record ReceiptRequest(int ProductId, int Quantity, string? Reason = null);
public sealed record AdjustmentRequest(int ProductId, int Delta, string Reason);
public sealed record InventoryDto(int ProductId, int CategoryId, string Sku, string Name, int OnHand);
public sealed record MovementDto(long Id, int ProductId, string Sku, string ProductName, bool ProductIsDeleted, int Delta,
    int BalanceAfter, string Type, int ActorId, string? Reason, DateTimeOffset OccurredAt);

public sealed record InventoryListQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public int? ProductId { get; init; }
    public int? CategoryId { get; init; }
    public string? Search { get; init; }
    public bool? InStock { get; init; }
    public string Sort { get; init; } = "id";
}

public sealed record MovementListQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public int? ProductId { get; init; }
    public int? ActorId { get; init; }
    public string? Type { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public string Sort { get; init; } = "id";
}
