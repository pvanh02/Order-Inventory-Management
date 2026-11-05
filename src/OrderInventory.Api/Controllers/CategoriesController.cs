using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderInventory.Api.Security;
using OrderInventory.Application.Catalog;
using OrderInventory.Application.Common;

namespace OrderInventory.Api.Controllers;

[ApiController]
[Route("api/v1/categories")]
[Authorize(Roles = "Admin,Staff")]
[ProducesResponseType<ValidationProblemDetails>(400)]
[ProducesResponseType<ProblemDetails>(401)]
[ProducesResponseType<ProblemDetails>(403)]
[ProducesResponseType<ProblemDetails>(404)]
[ProducesResponseType<ProblemDetails>(409)]
[ProducesResponseType<ProblemDetails>(500)]
public sealed class CategoriesController(CatalogService catalog) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<CategoryDto>>> List([FromQuery] CategoryListQuery query, CancellationToken cancellationToken) =>
        Ok(await catalog.ListCategoriesAsync(CurrentActor.Get(User), query, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryDto>> Get(int id, CancellationToken cancellationToken) =>
        Ok(await catalog.GetCategoryAsync(CurrentActor.Get(User), id, cancellationToken));

    [HttpPost, Authorize(Roles = "Admin")]
    [ProducesResponseType<CategoryDto>(201)]
    public async Task<ActionResult<CategoryDto>> Create(CategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await catalog.CreateCategoryAsync(CurrentActor.Get(User), request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = category.Id }, category);
    }

    [HttpPut("{id:int}"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<CategoryDto>> Update(int id, CategoryRequest request, CancellationToken cancellationToken) =>
        Ok(await catalog.UpdateCategoryAsync(CurrentActor.Get(User), id, request, cancellationToken));

    [HttpDelete("{id:int}"), Authorize(Roles = "Admin")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await catalog.DeleteCategoryAsync(CurrentActor.Get(User), id, cancellationToken);
        return NoContent();
    }
}
