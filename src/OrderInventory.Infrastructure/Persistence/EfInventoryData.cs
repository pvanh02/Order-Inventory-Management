using Microsoft.EntityFrameworkCore;
using OrderInventory.Application.Abstractions;
using OrderInventory.Application.Common;
using OrderInventory.Application.Inventory;
using OrderInventory.Domain.Inventory;

namespace OrderInventory.Infrastructure.Persistence;

public sealed class EfInventoryData(OrderInventoryDbContext context) : IInventoryData
{
    public async Task<PagedResult<InventoryDto>> ListAsync(InventoryListQuery query, CancellationToken cancellationToken)
    {
        var rows = from product in context.Products.AsNoTracking()
                   join category in context.Categories on product.CategoryId equals category.Id
                   join balance in context.InventoryBalances on product.Id equals balance.ProductId
                   where !product.IsDeleted && !category.IsDeleted
                   select new { ProductId = product.Id, product.CategoryId, product.Sku, product.Name, balance.OnHand };
        if (query.ProductId.HasValue) rows = rows.Where(item => item.ProductId == query.ProductId);
        if (query.CategoryId.HasValue) rows = rows.Where(item => item.CategoryId == query.CategoryId);
        if (query.Search is not null) rows = rows.Where(item => item.Name.Contains(query.Search) || item.Sku.Contains(query.Search));
        if (query.InStock.HasValue) rows = query.InStock.Value ? rows.Where(item => item.OnHand > 0) : rows.Where(item => item.OnHand == 0);
        var total = await rows.CountAsync(cancellationToken);
        var ordered = query.Sort switch
        {
            "sku" => rows.OrderBy(item => item.Sku).ThenBy(item => item.ProductId),
            "-sku" => rows.OrderByDescending(item => item.Sku).ThenBy(item => item.ProductId),
            "name" => rows.OrderBy(item => item.Name).ThenBy(item => item.ProductId),
            "-name" => rows.OrderByDescending(item => item.Name).ThenBy(item => item.ProductId),
            "onHand" => rows.OrderBy(item => item.OnHand).ThenBy(item => item.ProductId),
            "-onHand" => rows.OrderByDescending(item => item.OnHand).ThenBy(item => item.ProductId),
            "-id" => rows.OrderByDescending(item => item.ProductId),
            _ => rows.OrderBy(item => item.ProductId)
        };
        var offset = ((long)query.Page - 1) * query.PageSize;
        var items = offset > int.MaxValue ? [] : await ordered.Skip((int)offset).Take(query.PageSize)
            .Select(item => new InventoryDto(item.ProductId, item.CategoryId, item.Sku, item.Name, item.OnHand)).ToListAsync(cancellationToken);
        return new(items, query.Page, query.PageSize, total);
    }

    public async Task<PagedResult<MovementDto>> MovementsAsync(MovementListQuery query, CancellationToken cancellationToken)
    {
        var rows = context.InventoryMovements.AsNoTracking().AsQueryable();
        if (query.ProductId.HasValue) rows = rows.Where(item => item.ProductId == query.ProductId);
        if (query.ActorId.HasValue) rows = rows.Where(item => item.ActorId == query.ActorId);
        if (query.Type is not null)
        {
            var type = Enum.Parse<MovementType>(query.Type);
            rows = rows.Where(item => item.Type == type);
        }
        if (query.From.HasValue) rows = rows.Where(item => item.OccurredAt >= query.From);
        if (query.To.HasValue) rows = rows.Where(item => item.OccurredAt < query.To);
        var total = await rows.CountAsync(cancellationToken);
        var ordered = query.Sort switch
        {
            "occurredAt" => rows.OrderBy(item => item.OccurredAt).ThenBy(item => item.Id),
            "-occurredAt" => rows.OrderByDescending(item => item.OccurredAt).ThenBy(item => item.Id),
            "-id" => rows.OrderByDescending(item => item.Id),
            _ => rows.OrderBy(item => item.Id)
        };
        var offset = ((long)query.Page - 1) * query.PageSize;
        var items = offset > int.MaxValue ? [] : await Project(ordered.Skip((int)offset).Take(query.PageSize)).ToListAsync(cancellationToken);
        return new(items, query.Page, query.PageSize, total);
    }

    public Task<MovementDto> ReadMovementAsync(long id, CancellationToken cancellationToken) =>
        Project(context.InventoryMovements.AsNoTracking().Where(item => item.Id == id)).SingleAsync(cancellationToken);

    // No global soft-delete filter: movements must survive product/category deletion.
    private IQueryable<MovementDto> Project(IQueryable<InventoryMovement> rows) =>
        from movement in rows
        join product in context.Products on movement.ProductId equals product.Id
        select new MovementDto(movement.Id, product.Id, product.Sku, product.Name, product.IsDeleted,
            movement.Delta, movement.BalanceAfter, movement.Type == MovementType.Receipt ? "Receipt" : movement.Type == MovementType.Adjustment ? "Adjustment" : movement.Type == MovementType.Sale ? "Sale" : "CancellationReturn",
            movement.ActorId, movement.Reason, movement.OccurredAt);
}
