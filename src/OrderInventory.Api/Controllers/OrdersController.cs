using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderInventory.Api.Security;
using OrderInventory.Application.Orders;

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
}
