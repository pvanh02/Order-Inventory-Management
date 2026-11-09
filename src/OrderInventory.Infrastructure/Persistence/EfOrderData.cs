using System.Data;
using Microsoft.EntityFrameworkCore;
using OrderInventory.Application.Abstractions;
using OrderInventory.Application.Orders;
using OrderInventory.Domain.Orders;
using OrderInventory.Application.Common;

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
    public async Task<PagedResult<OrderSummaryDto>> ListAsync(OrderListQuery query, CancellationToken cancellationToken)
    {
        var rows = context.Orders.AsNoTracking().AsQueryable();
        if (query.CustomerId.HasValue) rows = rows.Where(item => item.CustomerId == query.CustomerId);
        if (query.CreatedBy.HasValue) rows = rows.Where(item => item.CreatedBy == query.CreatedBy);
        if (query.ProductId.HasValue) rows = rows.Where(item => item.Lines.Any(line => line.ProductId == query.ProductId));
        if (query.Status is not null) rows = rows.Where(item => item.Status == OrderStatus.Pending);
        if (query.Search is not null) rows = rows.Where(item => item.Lines.Any(line => line.SkuSnapshot.Contains(query.Search) || line.NameSnapshot.Contains(query.Search)));
        if (query.From.HasValue) rows = rows.Where(item => item.CreatedAt >= query.From);
        if (query.To.HasValue) rows = rows.Where(item => item.CreatedAt < query.To);
        if (query.MinTotal.HasValue) rows = rows.Where(item => item.Total >= query.MinTotal);
        if (query.MaxTotal.HasValue) rows = rows.Where(item => item.Total <= query.MaxTotal);
        var count = await rows.CountAsync(cancellationToken);
        var ordered = query.Sort switch
        {
            "id" => rows.OrderBy(item => item.Id),
            "-id" => rows.OrderByDescending(item => item.Id),
            "createdAt" => rows.OrderBy(item => item.CreatedAt).ThenBy(item => item.Id),
            "total" => rows.OrderBy(item => item.Total).ThenBy(item => item.Id),
            "-total" => rows.OrderByDescending(item => item.Total).ThenBy(item => item.Id),
            _ => rows.OrderByDescending(item => item.CreatedAt).ThenBy(item => item.Id)
        };
        var offset = ((long)query.Page - 1) * query.PageSize;
        var items = offset > int.MaxValue ? [] : await ordered.Skip((int)offset).Take(query.PageSize)
            .Select(item => new OrderSummaryDto(item.Id, item.CustomerId, item.CreatedBy, item.CreatedAt, item.Status.ToString(), item.Total, item.Lines.Count))
            .ToListAsync(cancellationToken);
        return new(items, query.Page, query.PageSize, count);
    }
    private void RequireTransaction()
    {
        if (context.Database.CurrentTransaction is null) throw new InvalidOperationException("Order writes require the owner's transaction.");
    }
}
