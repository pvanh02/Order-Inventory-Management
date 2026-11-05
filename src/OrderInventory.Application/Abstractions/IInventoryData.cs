using OrderInventory.Application.Common;
using OrderInventory.Application.Inventory;

namespace OrderInventory.Application.Abstractions;

public interface IInventoryData
{
    Task<PagedResult<InventoryDto>> ListAsync(InventoryListQuery query, CancellationToken cancellationToken);
    Task<PagedResult<MovementDto>> MovementsAsync(MovementListQuery query, CancellationToken cancellationToken);
    Task<MovementDto> ReadMovementAsync(long id, CancellationToken cancellationToken);
}
