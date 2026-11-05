using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderInventory.Api.Security;
using OrderInventory.Application.Catalog;
using OrderInventory.Application.Common;

namespace OrderInventory.Api.Controllers;

[ApiController]
[Route("api/v1/products")]
[Authorize(Roles = "Admin,Staff")]
[ProducesResponseType<ValidationProblemDetails>(400)]
[ProducesResponseType<ProblemDetails>(401)]
[ProducesResponseType<ProblemDetails>(403)]
[ProducesResponseType<ProblemDetails>(404)]
[ProducesResponseType<ProblemDetails>(409)]
[ProducesResponseType<ProblemDetails>(500)]
public sealed class ProductsController(CatalogService catalog) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductDto>>> List([FromQuery] ProductListQuery query, CancellationToken cancellationToken) =>
        Ok(await catalog.ListProductsAsync(CurrentActor.Get(User), query, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> Get(int id, CancellationToken cancellationToken) =>
        Ok(await catalog.GetProductAsync(CurrentActor.Get(User), id, cancellationToken));

    [HttpPost, Authorize(Roles = "Admin")]
    [ProducesResponseType<ProductDto>(201)]
    public async Task<ActionResult<ProductDto>> Create(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await catalog.CreateProductAsync(CurrentActor.Get(User), request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = product.Id }, product);
    }

    [HttpPut("{id:int}"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductDto>> Update(int id, UpdateProductRequest request, CancellationToken cancellationToken) =>
        Ok(await catalog.UpdateProductAsync(CurrentActor.Get(User), id, request, cancellationToken));

    [HttpDelete("{id:int}"), Authorize(Roles = "Admin")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await catalog.DeleteProductAsync(CurrentActor.Get(User), id, cancellationToken);
        return NoContent();
    }
}
