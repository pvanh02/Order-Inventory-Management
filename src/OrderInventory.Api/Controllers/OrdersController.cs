using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderInventory.Api.Security;
using OrderInventory.Application.Orders;
using OrderInventory.Application.Common;

namespace OrderInventory.Api.Controllers;

[ApiController]
[Route("api/v1/orders")]
[Authorize(Roles = "Admin,Staff")]
[ProducesResponseType<ValidationProblemDetails>(400)]
[ProducesResponseType<ProblemDetails>(401)]
[ProducesResponseType<ProblemDetails>(403)]
[ProducesResponseType<ProblemDetails>(404)]
[ProducesResponseType<ProblemDetails>(409)]
[ProducesResponseType<ProblemDetails>(500)]
[ProducesResponseType<ProblemDetails>(503)]
public sealed class OrdersController(OrderService orders) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<OrderSummaryDto>>(200)]
    public async Task<ActionResult<PagedResult<OrderSummaryDto>>> List([FromQuery] OrderListQuery query, CancellationToken cancellationToken) =>
        Ok(await orders.ListAsync(CurrentActor.Get(User), query, cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType<OrderDto>(200)]
    public async Task<ActionResult<OrderDto>> Get(int id, CancellationToken cancellationToken) =>
        Ok(await orders.GetAsync(CurrentActor.Get(User), id, cancellationToken));

    [HttpPost]
    [ProducesResponseType<OrderDto>(201)]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await orders.CreateAsync(CurrentActor.Get(User), request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = order.Id }, order);
    }

    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType<OrderDto>(200)]
    public async Task<ActionResult<OrderDto>> Cancel(int id, CancelOrderRequest request, CancellationToken cancellationToken) =>
        Ok(await orders.CancelAsync(CurrentActor.Get(User), id, request, cancellationToken));

    [HttpPost("{id:int}/complete")]
    [ProducesResponseType<OrderDto>(200)]
    public async Task<ActionResult<OrderDto>> Complete(int id, CancellationToken cancellationToken) =>
        Ok(await orders.CompleteAsync(CurrentActor.Get(User), id, cancellationToken));
}
