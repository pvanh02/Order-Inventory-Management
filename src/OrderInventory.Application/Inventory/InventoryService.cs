using OrderInventory.Application.Abstractions;
using OrderInventory.Application.Catalog;
using OrderInventory.Application.Common;
using OrderInventory.Application.Identity;
using OrderInventory.Domain.Inventory;

namespace OrderInventory.Application.Inventory;

public sealed class InventoryService(ICatalogData data, IInventoryWriter writer, IInventoryData reads, TimeProvider clock)
{
    public Task<PagedResult<InventoryDto>> ListAsync(Actor actor, InventoryListQuery query, CancellationToken cancellationToken)
    {
        CatalogValidation.Reader(actor);
        return reads.ListAsync(CatalogValidation.Query(query), cancellationToken);
    }

    public Task<PagedResult<MovementDto>> MovementsAsync(Actor actor, MovementListQuery query, CancellationToken cancellationToken)
    {
        CatalogValidation.Reader(actor);
        return reads.MovementsAsync(CatalogValidation.Query(query), cancellationToken);
    }

    public Task<MovementDto> ReceiveAsync(Actor actor, ReceiptRequest request, CancellationToken cancellationToken)
    {
        CatalogValidation.Reader(actor);
        CatalogValidation.Id(request.ProductId, "productId");
        if (request.Quantity <= 0) throw CatalogValidation.Error("quantity", "Receipt quantity must be a positive integer.");
        return ApplyAsync(actor, request.ProductId, request.Quantity, MovementType.Receipt,
            CatalogValidation.Reason(request.Reason, false), cancellationToken);
    }

    public Task<MovementDto> AdjustAsync(Actor actor, AdjustmentRequest request, CancellationToken cancellationToken)
    {
        CatalogValidation.Admin(actor);
        CatalogValidation.Id(request.ProductId, "productId");
        if (request.Delta is 0 or int.MinValue) throw CatalogValidation.Error("delta", "Use a nonzero signed quantity within +/-2147483647.");
        return ApplyAsync(actor, request.ProductId, request.Delta, MovementType.Adjustment,
            CatalogValidation.Reason(request.Reason, true), cancellationToken);
    }

    private async Task<MovementDto> ApplyAsync(Actor actor, int productId, int delta, MovementType type, string? reason, CancellationToken cancellationToken)
    {
        await using var transaction = await data.BeginTransactionAsync(cancellationToken);
        var movement = await writer.ApplyAsync(new(productId, delta, type, actor.UserId, reason, clock.GetUtcNow()), cancellationToken);
        await data.SaveChangesAsync(cancellationToken);
        var result = await reads.ReadMovementAsync(movement.Id, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }
}
