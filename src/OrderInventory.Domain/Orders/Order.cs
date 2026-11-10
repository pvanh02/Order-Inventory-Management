using OrderInventory.Domain.Catalog;

namespace OrderInventory.Domain.Orders;

public enum OrderStatus { Pending = 1, Completed = 2, Cancelled = 3 }

public sealed class Order
{
    private Order() { }
    public int Id { get; private set; }
    public int CustomerId { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public int? CompletedBy { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public int? CancelledBy { get; private set; }
    public string? CancellationReason { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal Total { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    private readonly List<OrderLine> lines = [];
    public IReadOnlyCollection<OrderLine> Lines => lines.AsReadOnly();

    public static Order Create(int customerId, int actorId, DateTimeOffset now, IReadOnlyList<OrderLine> lines)
    {
        if (customerId <= 0 || actorId <= 0) throw new ArgumentException("A customer and actor are required.");
        if (lines.Count is < 1 or > 100 || lines.Select(line => line.ProductId).Distinct().Count() != lines.Count)
            throw new ArgumentException("Supply 1 to 100 distinct product lines.");
        var total = lines.Sum(line => line.LineTotal);
        if (total > CatalogRules.MaximumPrice) throw new ArgumentException("The order total exceeds 18 whole VND digits.");
        var order = new Order { CustomerId = customerId, CreatedBy = actorId, CreatedAt = now, Status = OrderStatus.Pending, Total = total };
        order.lines.AddRange(lines);
        return order;
    }
}

public sealed class OrderLine
{
    private OrderLine() { }
    public int Id { get; private set; }
    public int OrderId { get; private set; }
    public int ProductId { get; private set; }
    public int Quantity { get; private set; }
    public string SkuSnapshot { get; private set; } = string.Empty;
    public string NameSnapshot { get; private set; } = string.Empty;
    public decimal UnitPriceSnapshot { get; private set; }
    public decimal LineTotal { get; private set; }

    public static OrderLine Create(Product product, int quantity)
    {
        if (product.Id <= 0 || product.IsDeleted || quantity <= 0) throw new ArgumentException("The product and quantity must be valid.");
        var total = product.UnitPrice * quantity;
        if (total > CatalogRules.MaximumPrice) throw new ArgumentException("The line total exceeds 18 whole VND digits.");
        return new() { ProductId = product.Id, Quantity = quantity, SkuSnapshot = product.Sku, NameSnapshot = product.Name,
            UnitPriceSnapshot = product.UnitPrice, LineTotal = total };
    }
}
