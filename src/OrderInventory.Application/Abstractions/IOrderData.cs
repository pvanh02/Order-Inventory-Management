using OrderInventory.Application.Orders;
using OrderInventory.Domain.Orders;

namespace OrderInventory.Application.Abstractions;

public interface IOrderData
{
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
    void Add(Order order);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<OrderDto?> ReadAsync(int id, CancellationToken cancellationToken);
}
