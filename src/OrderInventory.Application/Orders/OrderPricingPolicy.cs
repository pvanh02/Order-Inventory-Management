using OrderInventory.Application.Common;

namespace OrderInventory.Application.Orders;

public static class OrderPricingPolicy
{
    public static IReadOnlyList<OrderLineRequest> Normalize(CreateOrderRequest request)
    {
        if (request.CustomerId <= 0) throw Invalid("customerId", "An active customer is required.");
        if (request.Lines is null || request.Lines.Count is < 1 or > 100) throw Invalid("lines", "Supply 1 to 100 input lines.");
        var quantities = new SortedDictionary<int, int>();
        foreach (var line in request.Lines)
        {
            if (line is null || line.ProductId <= 0 || line.Quantity <= 0) throw Invalid("lines", "Each line requires positive productId and integer quantity.");
            var quantity = (long)quantities.GetValueOrDefault(line.ProductId) + line.Quantity;
            if (quantity > int.MaxValue) throw Invalid("lines", "The merged quantity exceeds the supported range.");
            quantities[line.ProductId] = (int)quantity;
        }
        return quantities.Select(pair => new OrderLineRequest(pair.Key, pair.Value)).ToArray();
    }

    internal static ValidationException Invalid(string field, string message) => new(new Dictionary<string, string[]> { [field] = [message] });
}
