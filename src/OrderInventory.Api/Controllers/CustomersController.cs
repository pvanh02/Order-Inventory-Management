using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderInventory.Api.Security;
using OrderInventory.Application.Common;
using OrderInventory.Application.Customers;

namespace OrderInventory.Api.Controllers;

[ApiController]
[Route("api/v1/customers")]
[Authorize(Roles = "Admin,Staff")]
[ProducesResponseType<ValidationProblemDetails>(400)]
[ProducesResponseType<ProblemDetails>(401)]
[ProducesResponseType<ProblemDetails>(403)]
[ProducesResponseType<ProblemDetails>(404)]
[ProducesResponseType<ProblemDetails>(409)]
[ProducesResponseType<ProblemDetails>(500)]
public sealed class CustomersController(CustomerService customers) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<CustomerDto>>(200)]
    public async Task<ActionResult<PagedResult<CustomerDto>>> List([FromQuery] CustomerListQuery query, CancellationToken cancellationToken) =>
        Ok(await customers.ListAsync(CurrentActor.Get(User), query, cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType<CustomerDto>(200)]
    public async Task<ActionResult<CustomerDto>> Get(int id, CancellationToken cancellationToken) =>
        Ok(await customers.GetAsync(CurrentActor.Get(User), id, cancellationToken));

    [HttpPost]
    [ProducesResponseType<CustomerDto>(201)]
    public async Task<ActionResult<CustomerDto>> Create(CustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = await customers.CreateAsync(CurrentActor.Get(User), request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = customer.Id }, customer);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<CustomerDto>(200)]
    public async Task<ActionResult<CustomerDto>> Update(int id, CustomerRequest request, CancellationToken cancellationToken) =>
        Ok(await customers.UpdateAsync(CurrentActor.Get(User), id, request, cancellationToken));

    [HttpDelete("{id:int}"), Authorize(Roles = "Admin")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await customers.DeleteAsync(CurrentActor.Get(User), id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:int}/history"), Authorize(Roles = "Admin")]
    [ProducesResponseType<PagedResult<CustomerAuditDto>>(200)]
    public async Task<ActionResult<PagedResult<CustomerAuditDto>>> History(int id, [FromQuery] CustomerHistoryQuery query, CancellationToken cancellationToken) =>
        Ok(await customers.HistoryAsync(CurrentActor.Get(User), id, query, cancellationToken));
}
