using OrderInventory.Application.Orders;
using OrderInventory.Application.Common;
using OrderInventory.Domain.Orders;

namespace OrderInventory.Application.Abstractions;

public interface IOrderData
{
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
    void Add(Order order);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<OrderDto?> ReadAsync(int id, CancellationToken cancellationToken);
    Task<PagedResult<OrderSummaryDto>> ListAsync(OrderListQuery query, CancellationToken cancellationToken);
    Task<OrderLifecycleState?> LockLifecycleAsync(int id, CancellationToken cancellationToken);
    Task<bool> TryCompleteAsync(int id, int actorId, DateTimeOffset completedAt, CancellationToken cancellationToken);
    Task<bool> TryCancelAsync(int id, int actorId, DateTimeOffset cancelledAt, string? reason, CancellationToken cancellationToken);
}
