namespace OrderInventory.Domain.Inventory;

public sealed class InventoryBalance
{
    private InventoryBalance() { }
    public int ProductId { get; private set; }
    public int OnHand { get; private set; }

    public static InventoryBalance Create(int productId)
    {
        if (productId <= 0) throw new ArgumentOutOfRangeException(nameof(productId));
        return new InventoryBalance { ProductId = productId };
    }
}
