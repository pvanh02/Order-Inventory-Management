using OrderInventory.Application.Abstractions;
using OrderInventory.Application.Common;
using OrderInventory.Application.Identity;
using OrderInventory.Domain.Inventory;
using OrderInventory.Domain.Orders;

namespace OrderInventory.Application.Orders;

public sealed class OrderService(IOrderData data, ICustomerData customers, ICatalogData catalog, IInventoryWriter writer, TimeProvider clock)
{
    public async Task<OrderDto> GetAsync(Actor actor, int id, CancellationToken cancellationToken)
    {
        Authorize(actor);
        return await data.ReadAsync(id, cancellationToken) ?? throw new NotFoundException("The order was not found.");
    }

    public async Task<OrderDto> CreateAsync(Actor actor, CreateOrderRequest request, CancellationToken cancellationToken)
    {
        Authorize(actor);
        var normalized = OrderPricingPolicy.Normalize(request);
        await using var transaction = await data.BeginTransactionAsync(cancellationToken);
        var customer = await customers.FindAsync(request.CustomerId, true, cancellationToken);
        if (customer is null || customer.IsDeleted) throw new NotFoundException("The customer was not found.");
        List<OrderLine> lines = [];
        foreach (var line in normalized)
        {
            var product = await catalog.FindProductAsync(line.ProductId, true, cancellationToken);
            if (product is null || product.IsDeleted) throw new NotFoundException("The product was not found.");
            try { lines.Add(OrderLine.Create(product, line.Quantity)); }
            catch (ArgumentException) { throw OrderPricingPolicy.Invalid("lines", "The line amount exceeds the supported whole VND range."); }
        }
        Order order;
        try { order = Order.Create(customer.Id, actor.UserId, clock.GetUtcNow(), lines); }
        catch (ArgumentException) { throw OrderPricingPolicy.Invalid("lines", "The order total exceeds the supported whole VND range."); }
        data.Add(order);
        await data.SaveChangesAsync(cancellationToken);
        foreach (var line in order.Lines.OrderBy(line => line.ProductId))
            await writer.ApplyAsync(new(line.ProductId, -line.Quantity, MovementType.Sale, actor.UserId, null, order.CreatedAt, line.Id), cancellationToken);
        await data.SaveChangesAsync(cancellationToken);
        var result = await data.ReadAsync(order.Id, cancellationToken) ?? throw new InvalidOperationException("The saved order is missing.");
        try { await transaction.CommitAsync(cancellationToken); }
        catch (ConflictException) { throw; }
        catch (Exception) { throw new CommitOutcomeUnknownException(order.Id); }
        return result;
    }

    private static void Authorize(Actor actor)
    {
        if (actor.UserId <= 0 || !Enum.IsDefined(actor.Role)) throw new ForbiddenException();
    }
}
