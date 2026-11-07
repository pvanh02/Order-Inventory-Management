using System.Data;
using Microsoft.EntityFrameworkCore;
using OrderInventory.Application.Abstractions;
using OrderInventory.Application.Orders;
using OrderInventory.Domain.Orders;

namespace OrderInventory.Infrastructure.Persistence;

public sealed class EfOrderData(OrderInventoryDbContext context) : IOrderData
{
    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken) =>
        new EfUnitOfWorkTransaction(await PersistenceErrors.ExecuteAsync(() => context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken)));
    public void Add(Order order)
    {
        RequireTransaction();
        context.Orders.Add(order);
    }
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        RequireTransaction();
        await PersistenceErrors.ExecuteAsync(() => context.SaveChangesAsync(cancellationToken));
    }
    public async Task<OrderDto?> ReadAsync(int id, CancellationToken cancellationToken)
    {
        var order = await context.Orders.AsNoTracking().Include(item => item.Lines).SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return order is null ? null : new(order.Id, order.CustomerId, order.CreatedBy, order.CreatedAt, order.Status.ToString(), order.Total,
            order.CompletedAt, order.CancelledAt, order.Lines.OrderBy(line => line.ProductId).Select(line => new OrderLineDto(line.Id, line.ProductId,
                line.Quantity, line.SkuSnapshot, line.NameSnapshot, line.UnitPriceSnapshot, line.LineTotal)).ToArray());
    }
    private void RequireTransaction()
    {
        if (context.Database.CurrentTransaction is null) throw new InvalidOperationException("Order writes require the owner's transaction.");
    }
}
