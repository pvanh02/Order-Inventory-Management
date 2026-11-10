namespace OrderInventory.Application.Orders;

public sealed record OrderLineRequest(int ProductId, int Quantity);
public sealed record CreateOrderRequest(int CustomerId, IReadOnlyList<OrderLineRequest>? Lines);
public sealed record CancelOrderRequest(string? Reason = null);
public sealed record OrderLineDto(int Id, int ProductId, int Quantity, string SkuSnapshot, string NameSnapshot, decimal UnitPriceSnapshot, decimal LineTotal);
public sealed record OrderDto(int Id, int CustomerId, int CreatedBy, DateTimeOffset CreatedAt, string Status, decimal Total,
    DateTimeOffset? CompletedAt, DateTimeOffset? CancelledAt, IReadOnlyList<OrderLineDto> Lines)
{
    public int? CompletedBy { get; init; }
    public int? CancelledBy { get; init; }
    public string? CancellationReason { get; init; }
}

public sealed record OrderLifecycleState(int Id, int CreatedBy, OrderInventory.Domain.Orders.OrderStatus Status);

public sealed record OrderSummaryDto(int Id, int CustomerId, int CreatedBy, DateTimeOffset CreatedAt, string Status, decimal Total, int LineCount);

public sealed record OrderListQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string Sort { get; init; } = "-createdAt";
    public int? CustomerId { get; init; }
    public int? CreatedBy { get; init; }
    public int? ProductId { get; init; }
    public string? Status { get; init; }
    public string? Search { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public decimal? MinTotal { get; init; }
    public decimal? MaxTotal { get; init; }
}
