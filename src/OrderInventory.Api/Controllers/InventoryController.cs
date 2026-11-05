using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderInventory.Api.Security;
using OrderInventory.Application.Common;
using OrderInventory.Application.Inventory;

namespace OrderInventory.Api.Controllers;

[ApiController]
[Route("api/v1/inventory")]
[Authorize(Roles = "Admin,Staff")]
[ProducesResponseType<ValidationProblemDetails>(400)]
[ProducesResponseType<ProblemDetails>(401)]
[ProducesResponseType<ProblemDetails>(403)]
[ProducesResponseType<ProblemDetails>(404)]
[ProducesResponseType<ProblemDetails>(409)]
[ProducesResponseType<ProblemDetails>(500)]
public sealed class InventoryController(InventoryService inventory) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<InventoryDto>>> List([FromQuery] InventoryListQuery query, CancellationToken cancellationToken) =>
        Ok(await inventory.ListAsync(CurrentActor.Get(User), query, cancellationToken));

    [HttpGet("movements")]
    public async Task<ActionResult<PagedResult<MovementDto>>> Movements([FromQuery] MovementListQuery query, CancellationToken cancellationToken) =>
        Ok(await inventory.MovementsAsync(CurrentActor.Get(User), query, cancellationToken));

    [HttpPost("receipts")]
    public async Task<ActionResult<MovementDto>> Receive(ReceiptRequest request, CancellationToken cancellationToken) =>
        Ok(await inventory.ReceiveAsync(CurrentActor.Get(User), request, cancellationToken));

    [HttpPost("adjustments"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<MovementDto>> Adjust(AdjustmentRequest request, CancellationToken cancellationToken) =>
        Ok(await inventory.AdjustAsync(CurrentActor.Get(User), request, cancellationToken));
}
