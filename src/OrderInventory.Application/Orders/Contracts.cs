namespace OrderInventory.Application.Orders;

public sealed record OrderLineRequest(int ProductId, int Quantity);
public sealed record CreateOrderRequest(int CustomerId, IReadOnlyList<OrderLineRequest>? Lines);
public sealed record OrderLineDto(int Id, int ProductId, int Quantity, string SkuSnapshot, string NameSnapshot, decimal UnitPriceSnapshot, decimal LineTotal);
public sealed record OrderDto(int Id, int CustomerId, int CreatedBy, DateTimeOffset CreatedAt, string Status, decimal Total,
    DateTimeOffset? CompletedAt, DateTimeOffset? CancelledAt, IReadOnlyList<OrderLineDto> Lines);
